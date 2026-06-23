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
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                // Transakcija počinje PRIJE idempotency provjere.
                // Na ovaj način provjera i upis ProcessedMessage su atomična operacija.
                // Ako dvije instance consumera istovremeno prime istu poruku,
                // unique constraint na MessageId garantuje da će samo jedna uspješno
                // commitovati — druga će dobiti DbUpdateException i reschedulovati poruku.
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
                        else
                        {
                            _logger.LogWarning(
                                "Poruka {MessageId} tipa {EventType} nije mogla biti deserijalizovana.",
                                messageId, eventType);
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
                            else
                            {
                                _logger.LogWarning(
                                    "RegistrationDeletedEvent stigao za EventId={EventId}, ali tracker ne postoji.",
                                    evt.EventId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Poruka {MessageId} tipa {EventType} nije mogla biti deserijalizovana.",
                                messageId, eventType);
                        }
                    }
                    else
                    {
                        // Nepoznat tip poruke — logujemo, ali ne bacamo grešku.
                        // Poruka se ackuje (ne vraća u red) da ne bi blokirala queue,
                        // ali se NE evidentira kao obrađena — u slučaju ponovne isporuke
                        // (npr. pri restartu servisa) može biti ponovo evaluirana.
                        _logger.LogWarning(
                            "Nepoznat tip poruke: {EventType}, MessageId={MessageId}. Poruka se ackuje bez obrade.",
                            eventType, messageId);

                        await tx.RollbackAsync(cancellationToken);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                        return;
                    }

                    // ProcessedMessage se upisuje SAMO kada je poruka stvarno obrađena.
                    // Ovo sprečava dvostruku obradu čak i u slučaju at-least-once isporuke.
                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        MessageId = messageId,
                        EventType = eventType,
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Obradjena poruka tipa {EventType}, MessageId={MessageId}",
                        eventType, messageId);
                }
                else
                {
                    // Poruka je već obrađena — rollback (nema šta da commitujemo) i ack.
                    await tx.RollbackAsync(cancellationToken);
                    _logger.LogWarning("Poruka {MessageId} vec obradjena — preskacam.", messageId);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
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