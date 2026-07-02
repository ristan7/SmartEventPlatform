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
    public sealed class SagaChoreographyConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<SagaChoreographyRabbitMqOptions> _options;
        private readonly ISagaChoreographyPublisher _publisher;
        private readonly ILogger<SagaChoreographyConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly ConcurrentDictionary<string, int> _retryCounts = new();

        public SagaChoreographyConsumerService(
            IServiceScopeFactory scopeFactory,
            IOptions<SagaChoreographyRabbitMqOptions> options,
            ISagaChoreographyPublisher publisher,
            ILogger<SagaChoreographyConsumerService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _publisher = publisher;
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

            await _channel.ExchangeDeclareAsync(mq.Exchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.ExchangeDeclareAsync(mq.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(mq.EventServiceDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(mq.EventServiceDlq, mq.DeadLetterExchange, mq.EventServiceRoutingKey, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(
                mq.EventServiceQueue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", mq.DeadLetterExchange } },
                cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(mq.EventServiceQueue, mq.Exchange, mq.EventServiceRoutingKey, cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(0, mq.PrefetchCount, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, args) =>
            {
                var messageId = args.BasicProperties.MessageId ?? args.DeliveryTag.ToString();
                var messageType = args.BasicProperties.Type ?? "";
                _retryCounts.TryGetValue(messageId, out var retryCount);

                try
                {
                    var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                    _logger.LogInformation("[SagaChoreo-ES] Primljena poruka Type={Type}.", messageType);

                    switch (messageType)
                    {
                        case nameof(SagaChoreographyStartedEvent):
                            await HandleSagaStartedAsync(
                                JsonSerializer.Deserialize<SagaChoreographyStartedEvent>(payload)!,
                                stoppingToken);
                            break;
                        case nameof(SagaAttendanceFailedEvent):
                            await HandleAttendanceFailedAsync(
                                JsonSerializer.Deserialize<SagaAttendanceFailedEvent>(payload)!,
                                stoppingToken);
                            break;
                        case nameof(SagaRegistrationConfirmedEvent):
                            await HandleRegistrationConfirmedAsync(
                                JsonSerializer.Deserialize<SagaRegistrationConfirmedEvent>(payload)!,
                                stoppingToken);
                            break;
                        default:
                            _logger.LogWarning("[SagaChoreo-ES] Nepoznat tip: {Type}. Ack-ujemo.", messageType);
                            break;
                    }

                    _retryCounts.TryRemove(messageId, out int _);
                    await _channel.BasicAckAsync(args.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SagaChoreo-ES] Greska pri obradi Type={Type}. Pokusaj={Retry}.",
                        messageType, retryCount + 1);

                    if (retryCount >= mq.MaxRetryCount)
                    {
                        _retryCounts.TryRemove(messageId, out int _);
                        await _channel.BasicNackAsync(args.DeliveryTag, false, requeue: false);
                    }
                    else
                    {
                        _retryCounts[messageId] = retryCount + 1;
                        await _channel.BasicNackAsync(args.DeliveryTag, false, requeue: true);
                    }
                }
            };

            await _channel.BasicConsumeAsync(mq.EventServiceQueue, autoAck: false, consumer, stoppingToken);
            _logger.LogInformation("[SagaChoreo-ES] Consumer pokrenut na '{Queue}'.", mq.EventServiceQueue);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleSagaStartedAsync(SagaChoreographyStartedEvent evt, CancellationToken ct)
        {
            _logger.LogInformation("[SagaChoreo-ES] SagaStarted. CorrelationId={CorrId}, EventId={EventId}.",
                evt.CorrelationId, evt.EventId);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

            // CorrelationId (Guid) se pretvara u deterministican long za SagaSpotReservation.SagaId
            long sagaIdAsLong = Math.Abs(evt.CorrelationId.GetHashCode());

            // Idempotentnost: vec postoji rezervacija za ovu Sagu
            var existingReservation = await db.SagaSpotReservations
                .FirstOrDefaultAsync(r => r.SagaId == sagaIdAsLong, ct);

            if (existingReservation != null)
            {
                _logger.LogInformation("[SagaChoreo-ES] Idempotentno: rezervacija vec postoji. Objavljujem SpotReserved.");
                await PublishSpotReservedAsync(evt, ct);
                return;
            }

            // Provjeri dogadjaj
            var eventEntity = await db.Events.FindAsync(evt.EventId);
            if (eventEntity is null)
            {
                await PublishSpotReservationFailedAsync(evt, "Dogadjaj ne postoji.", ct);
                return;
            }

            // Provjeri kapacitet
            var confirmedCount = await db.EventRegistrationTrackers
                .Where(t => t.EventId == evt.EventId)
                .Select(t => t.RegistrationCount)
                .FirstOrDefaultAsync(ct);

            var pendingCount = await db.SagaSpotReservations
                .CountAsync(r => r.EventId == evt.EventId, ct);

            if (confirmedCount + pendingCount >= eventEntity.LocationCapacitySnapshot)
            {
                _logger.LogWarning("[SagaChoreo-ES] Nema slobodnih mjesta za EventId={EventId}.", evt.EventId);
                await PublishSpotReservationFailedAsync(evt, "Kapacitet dogadjaja je popunjen.", ct);
                return;
            }

            // Kreiraj privremenu rezervaciju
            db.SagaSpotReservations.Add(new SagaSpotReservation
            {
                SagaId = sagaIdAsLong,
                EventId = evt.EventId,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("[SagaChoreo-ES] Rezervacija kreirana SagaId={SagaId}. Objavljujem SpotReserved.", sagaIdAsLong);
            await PublishSpotReservedAsync(evt, ct);
        }

        private async Task PublishSpotReservedAsync(SagaChoreographyStartedEvent evt, CancellationToken ct)
        {
            var spotReservedEvt = new SagaSpotReservedEvent
            {
                CorrelationId = evt.CorrelationId,
                EventId = evt.EventId,
                LocationId = evt.LocationId,
                OccurredAt = DateTime.UtcNow
            };
            await _publisher.PublishAsync(
                _options.Value.DirectoryServiceRoutingKey,
                JsonSerializer.Serialize(spotReservedEvt),
                nameof(SagaSpotReservedEvent), ct);
            _logger.LogInformation("[SagaChoreo-ES] SagaSpotReserved objavljen. CorrelationId={CorrId}.", evt.CorrelationId);
        }

        private async Task PublishSpotReservationFailedAsync(
            SagaChoreographyStartedEvent evt, string reason, CancellationToken ct)
        {
            var failedEvt = new SagaSpotReservationFailedEvent
            {
                CorrelationId = evt.CorrelationId,
                EventId = evt.EventId,
                Reason = reason,
                OccurredAt = DateTime.UtcNow
            };
            await _publisher.PublishAsync(
                _options.Value.RegistrationServiceRoutingKey,
                JsonSerializer.Serialize(failedEvt),
                nameof(SagaSpotReservationFailedEvent), ct);
            _logger.LogWarning("[SagaChoreo-ES] SagaSpotReservationFailed objavljen. Razlog='{Reason}'.", reason);
        }

        private async Task HandleAttendanceFailedAsync(SagaAttendanceFailedEvent evt, CancellationToken ct)
        {
            _logger.LogWarning("[SagaChoreo-ES] AttendanceFailed. CorrelationId={CorrId}.", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

            long sagaIdAsLong = Math.Abs(evt.CorrelationId.GetHashCode());

            var reservation = await db.SagaSpotReservations
                .FirstOrDefaultAsync(r => r.SagaId == sagaIdAsLong && r.EventId == evt.EventId, ct);

            if (reservation is not null)
            {
                db.SagaSpotReservations.Remove(reservation);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("[SagaChoreo-ES] Kompenzacija K2: Rezervacija uklonjena za SagaId={SagaId}.", sagaIdAsLong);
            }
            else
            {
                _logger.LogWarning("[SagaChoreo-ES] Rezervacija nije pronadjena. Mozda vec kompenzovana.");
            }

            var spotReleasedEvt = new SagaSpotReleasedEvent
            {
                CorrelationId = evt.CorrelationId,
                EventId = evt.EventId,
                Reason = evt.Reason,
                OccurredAt = DateTime.UtcNow
            };
            await _publisher.PublishAsync(
                _options.Value.RegistrationServiceRoutingKey,
                JsonSerializer.Serialize(spotReleasedEvt),
                nameof(SagaSpotReleasedEvent), ct);
            _logger.LogWarning("[SagaChoreo-ES] SagaSpotReleased objavljen. CorrelationId={CorrId}.", evt.CorrelationId);
        }

        private async Task HandleRegistrationConfirmedAsync(SagaRegistrationConfirmedEvent evt, CancellationToken ct)
        {
            _logger.LogInformation("[SagaChoreo-ES] RegistrationConfirmed. CorrelationId={CorrId}.", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

            long sagaIdAsLong = Math.Abs(evt.CorrelationId.GetHashCode());

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var reservation = await db.SagaSpotReservations
                    .FirstOrDefaultAsync(r => r.SagaId == sagaIdAsLong && r.EventId == evt.EventId, ct);

                if (reservation is null)
                {
                    _logger.LogWarning("[SagaChoreo-ES] Rezervacija nije pronadjena. Idempotentno nastavljamo.");
                    await tx.CommitAsync(ct);
                    return;
                }

                db.SagaSpotReservations.Remove(reservation);

                var tracker = await db.EventRegistrationTrackers.FindAsync(evt.EventId);
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

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "[SagaChoreo-ES] Finalizacija: EventRegistrationTracker incrementiran za EventId={EventId}.", evt.EventId);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}