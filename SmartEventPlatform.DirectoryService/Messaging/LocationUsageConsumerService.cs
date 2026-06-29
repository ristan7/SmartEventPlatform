using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.DirectoryService.Data;
using SmartEventPlatform.DirectoryService.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.DirectoryService.Messaging
{
    public sealed class LocationUsageConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<LocationUsageRabbitMqOptions> _options;
        private readonly ILogger<LocationUsageConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly ConcurrentDictionary<string, int> _retryCounts = new();

        public LocationUsageConsumerService(
            IServiceScopeFactory scopeFactory,
            IOptions<LocationUsageRabbitMqOptions> options,
            ILogger<LocationUsageConsumerService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mq = _options.Value;

            var factory = new ConnectionFactory
            {
                HostName = mq.HostName,
                Port = mq.Port,
                UserName = mq.UserName,
                Password = mq.Password
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: mq.Exchange, type: ExchangeType.Direct,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: mq.DeadLetterExchange, type: ExchangeType.Direct,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: mq.DeadLetterQueue,
                durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: mq.DeadLetterQueue, exchange: mq.DeadLetterExchange,
                routingKey: mq.RoutingKey, cancellationToken: stoppingToken);

            // Isti x-dead-letter-exchange argument kao u EventService/RabbitMqPublisher.cs
            await _channel.QueueDeclareAsync(
                queue: mq.Queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", mq.DeadLetterExchange } },
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: mq.Queue, exchange: mq.Exchange,
                routingKey: mq.RoutingKey, cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0, prefetchCount: mq.PrefetchCount,
                global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: mq.Queue, autoAck: false,
                consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation(
                "LocationUsageConsumerService started. Queue='{Queue}', DLQ='{DLQ}', MaxRetries={Max}.",
                mq.Queue, mq.DeadLetterQueue, mq.MaxRetryCount);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            var mq = _options.Value;
            var messageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString();
            var eventType = ea.BasicProperties.Type ?? string.Empty;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDbContext>();
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.MessageId == messageId, cancellationToken);

                if (!alreadyProcessed)
                {
                    if (eventType == nameof(EventCreatedEvent))
                    {
                        var evt = JsonSerializer.Deserialize<EventCreatedEvent>(body);
                        if (evt is not null)
                        {
                            var exists = await db.LocationUsageTrackers
                                .AnyAsync(t => t.EventId == evt.EventId, cancellationToken);
                            if (!exists)
                                db.LocationUsageTrackers.Add(new LocationUsageTracker
                                { EventId = evt.EventId, LocationId = evt.LocationId });
                        }
                        else
                            _logger.LogWarning("Could not deserialize. MessageId={Id}, EventType={T}.", messageId, eventType);
                    }
                    else if (eventType == nameof(EventDeletedEvent))
                    {
                        var evt = JsonSerializer.Deserialize<EventDeletedEvent>(body);
                        if (evt is not null)
                        {
                            var tracker = await db.LocationUsageTrackers
                                .FirstOrDefaultAsync(t => t.EventId == evt.EventId, cancellationToken);
                            if (tracker is not null)
                                db.LocationUsageTrackers.Remove(tracker);
                            else
                                _logger.LogWarning("LocationUsageTracker not found. EventId={Id}.", evt.EventId);
                        }
                        else
                            _logger.LogWarning("Could not deserialize. MessageId={Id}, EventType={T}.", messageId, eventType);
                    }
                    else
                    {
                        _logger.LogWarning("Unknown type '{T}', MessageId={Id}. Acking.", eventType, messageId);
                        await tx.RollbackAsync(cancellationToken);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                        return;
                    }

                    db.ProcessedMessages.Add(new ProcessedMessage
                    { MessageId = messageId, EventType = eventType, ProcessedAtUtc = DateTime.UtcNow });

                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                    _logger.LogInformation("Processed. EventType={T}, MessageId={Id}.", eventType, messageId);
                }
                else
                {
                    await tx.RollbackAsync(cancellationToken);
                    _logger.LogWarning("Duplicate, skipping. MessageId={Id}.", messageId);
                }

                _retryCounts.TryRemove(messageId, out _);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                var retryCount = _retryCounts.AddOrUpdate(messageId, 1, (_, old) => old + 1);
                _logger.LogError(ex, "Error. MessageId={Id}, Attempt={N}/{Max}.", messageId, retryCount, mq.MaxRetryCount);

                if (retryCount >= mq.MaxRetryCount)
                {
                    _retryCounts.TryRemove(messageId, out _);
                    _logger.LogWarning("Max retries exceeded, dead-lettering. MessageId={Id}.", messageId);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                }
                else
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}