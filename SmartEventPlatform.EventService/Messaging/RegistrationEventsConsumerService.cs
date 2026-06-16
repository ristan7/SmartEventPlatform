using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartEventPlatform.Contracts.Events.Integration;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Models;
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

            await _channel.QueueDeclareAsync(
                queue: mq.Queue, durable: true, exclusive: false,
                autoDelete: false, arguments: null, cancellationToken: stoppingToken);

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

            _logger.LogInformation("EventService Consumer sluša na queue-u {Queue}", mq.Queue);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

                var messageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString();
                var eventType = ea.BasicProperties.Type ?? string.Empty;

                // Idempotent provjera
                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.MessageId == messageId, cancellationToken);

                if (alreadyProcessed)
                {
                    _logger.LogWarning("Poruka {MessageId} vec obradjena — preskacam.", messageId);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                    return;
                }

                var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                if (eventType == nameof(RegistrationCreatedEvent))
                {
                    var evt = JsonSerializer.Deserialize<RegistrationCreatedEvent>(body);
                    if (evt is not null)
                    {
                        var tracker = await db.EventRegistrationTrackers
                            .FirstOrDefaultAsync(t => t.EventId == evt.EventId, cancellationToken);

                        if (tracker is null)
                        {
                            db.EventRegistrationTrackers.Add(new EventRegistrationTracker
                            {
                                EventId = evt.EventId,
                                RegistrationCount = 1
                            });
                        }
                        else
                        {
                            tracker.RegistrationCount++;
                        }
                    }
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
                    }
                }
                else
                {
                    _logger.LogWarning("Nepoznat tip poruke: {EventType}", eventType);
                }

                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    MessageId = messageId,
                    EventType = eventType,
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);

                _logger.LogInformation("Obradjena poruka tipa {EventType}, MessageId={MessageId}", eventType, messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska pri obradi poruke. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                if (_channel is not null)
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