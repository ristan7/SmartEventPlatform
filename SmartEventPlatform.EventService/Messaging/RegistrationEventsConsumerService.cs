using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.EventService.Messaging
{
    public sealed class RegistrationEventsConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<ConsumerRabbitMqOptions> _options;
        private readonly ILogger<RegistrationEventsConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly ConcurrentDictionary<string, int> _retryCounts = new();

        public RegistrationEventsConsumerService(
            IServiceScopeFactory scopeFactory,
            IOptions<ConsumerRabbitMqOptions> options,
            ILogger<RegistrationEventsConsumerService> logger)
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


            await _channel.QueueDeclareAsync(
                queue: mq.Queue, durable: true, exclusive: false,
                autoDelete: false, arguments: new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", mq.DeadLetterExchange }
            }, cancellationToken: stoppingToken);

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
                "RegistrationEventsConsumerService started. Queue='{Queue}', DLQ='{DLQ}', MaxRetries={Max}.",
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
                var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.MessageId == messageId, cancellationToken);

                if (!alreadyProcessed)
                {
                    if (eventType == nameof(RegistrationCreatedEvent))
                    {
                        var evt = JsonSerializer.Deserialize<RegistrationCreatedEvent>(body);
                        if (evt is not null)
                        {
                            var tracker = await db.EventRegistrationTrackers
                                .FirstOrDefaultAsync(t => t.EventId == evt.EventId, cancellationToken);

                            if (tracker is null)
                                db.EventRegistrationTrackers.Add(new EventRegistrationTracker
                                { EventId = evt.EventId, RegistrationCount = 1 });
                            else
                                tracker.RegistrationCount++;
                        }
                        else
                            _logger.LogWarning("Could not deserialize. MessageId={Id}, EventType={T}.", messageId, eventType);
                    }
                    else if (eventType == nameof(RegistrationDeletedEvent))
                    {
                        var evt = JsonSerializer.Deserialize<RegistrationDeletedEvent>(body);
                        if (evt is not null)
                        {
                            var tracker = await db.EventRegistrationTrackers
                                .FirstOrDefaultAsync(t => t.EventId == evt.EventId, cancellationToken);

                            if (tracker is not null)
                            {
                                tracker.RegistrationCount--;
                                if (tracker.RegistrationCount <= 0)
                                    db.EventRegistrationTrackers.Remove(tracker);
                            }
                            else
                                _logger.LogWarning("Tracker not found for EventId={Id}.", evt.EventId);
                        }
                        else
                            _logger.LogWarning("Could not deserialize. MessageId={Id}, EventType={T}.", messageId, eventType);
                    }
                    else
                    {
                        _logger.LogWarning("Unknown event type '{T}', MessageId={Id}. Acking.", eventType, messageId);
                        await tx.RollbackAsync(cancellationToken);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                        return;
                    }

                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        MessageId = messageId,
                        EventType = eventType,
                        ProcessedAtUtc = DateTime.UtcNow
                    });

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

                _logger.LogError(ex,
                    "Error processing. MessageId={Id}, EventType={T}, Attempt={N}/{Max}.",
                    messageId, eventType, retryCount, mq.MaxRetryCount);

                if (retryCount >= mq.MaxRetryCount)
                {
                    _retryCounts.TryRemove(messageId, out _);
                    _logger.LogWarning("Max retries exceeded, dead-lettering. MessageId={Id}.", messageId);
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: false,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: true,
                        cancellationToken: cancellationToken);
                }
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