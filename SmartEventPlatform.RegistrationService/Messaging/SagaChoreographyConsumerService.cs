using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.RegistrationService.Data;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.RegistrationService.Messaging
{
    /// <summary>
    /// RegistrationService konzumira dogadjaje iz saga-choreo.registration-service.queue.
    ///
    /// Poruke koje prima:
    ///   SagaSpotReservationFailed  → EventService nije uspio rezervisati → kompenzuj K1
    ///   SagaAttendanceRecorded     → DirectoryService uspio → potvrdi registraciju (K4)
    ///   SagaSpotReleased           → EventService oslobodio spot → kompenzuj K1
    /// </summary>
    public sealed class SagaChoreographyConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<SagaChoreographyRabbitMqOptions> _options;
        private readonly ISagaChoreographyPublisher _publisher;
        private readonly IEmailQueuePublisher _emailPublisher;
        private readonly ILogger<SagaChoreographyConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly ConcurrentDictionary<string, int> _retryCounts = new();

        public SagaChoreographyConsumerService(
            IServiceScopeFactory scopeFactory,
            IOptions<SagaChoreographyRabbitMqOptions> options,
            ISagaChoreographyPublisher publisher,
            IEmailQueuePublisher emailPublisher,
            ILogger<SagaChoreographyConsumerService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _publisher = publisher;
            _emailPublisher = emailPublisher;
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

            await _channel.ExchangeDeclareAsync(mq.Exchange, ExchangeType.Direct, true, false, cancellationToken: stoppingToken);
            await _channel.ExchangeDeclareAsync(mq.DeadLetterExchange, ExchangeType.Direct, true, false, cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(mq.RegistrationServiceDlq, true, false, false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(mq.RegistrationServiceDlq, mq.DeadLetterExchange, mq.RegistrationServiceRoutingKey, cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                mq.RegistrationServiceQueue, true, false, false,
                arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", mq.DeadLetterExchange } },
                cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(mq.RegistrationServiceQueue, mq.Exchange, mq.RegistrationServiceRoutingKey, cancellationToken: stoppingToken);

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

                    _logger.LogInformation(
                        "[SagaChoreo-RS] Primljena poruka Type={Type}, MessageId={MsgId}.",
                        messageType, messageId);

                    switch (messageType)
                    {
                        case nameof(SagaSpotReservationFailedEvent):
                            await HandleSpotReservationFailedAsync(
                                JsonSerializer.Deserialize<SagaSpotReservationFailedEvent>(payload)!,
                                stoppingToken);
                            break;

                        case nameof(SagaAttendanceRecordedEvent):
                            await HandleAttendanceRecordedAsync(
                                JsonSerializer.Deserialize<SagaAttendanceRecordedEvent>(payload)!,
                                stoppingToken);
                            break;

                        case nameof(SagaSpotReleasedEvent):
                            await HandleSpotReleasedAsync(
                                JsonSerializer.Deserialize<SagaSpotReleasedEvent>(payload)!,
                                stoppingToken);
                            break;

                        default:
                            _logger.LogWarning("[SagaChoreo-RS] Nepoznat tip poruke: {Type}. Ack-ujemo.", messageType);
                            break;
                    }

                    _retryCounts.TryRemove(messageId, out int _);
                    await _channel.BasicAckAsync(args.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[SagaChoreo-RS] Greska pri obradi Type={Type}, MessageId={MsgId}. Pokusaj={Retry}.",
                        messageType, messageId, retryCount + 1);

                    if (retryCount >= mq.MaxRetryCount)
                    {
                        _logger.LogError("[SagaChoreo-RS] Max retry dostignut za MessageId={MsgId}. Saljemo u DLQ.", messageId);
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

            await _channel.BasicConsumeAsync(mq.RegistrationServiceQueue, autoAck: false, consumer, stoppingToken);
            _logger.LogInformation("[SagaChoreo-RS] Consumer pokrenut na '{Queue}'.", mq.RegistrationServiceQueue);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleSpotReservationFailedAsync(SagaSpotReservationFailedEvent evt, CancellationToken ct)
        {
            _logger.LogWarning("[SagaChoreo-RS] SpotReservationFailed. CorrelationId={CorrId}, Razlog='{Reason}'.",
                evt.CorrelationId, evt.Reason);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

            var saga = await db.SagaChoreographyStates
                .FirstOrDefaultAsync(s => s.CorrelationId == evt.CorrelationId, ct);

            if (saga is null)
            {
                _logger.LogWarning("[SagaChoreo-RS] Saga state nije pronadjen za CorrelationId={CorrId}.", evt.CorrelationId);
                return;
            }

            if (saga.RegistrationId.HasValue)
            {
                // NOVO
                var reg = await db.Registrations.FindAsync(saga.RegistrationId.Value);
                if (reg is not null)
                {
                    db.Registrations.Remove(reg);
                    _logger.LogInformation("[SagaChoreo-RS] Kompenzacija K1: obrisana PENDING registracija {RegId}.", saga.RegistrationId);
                }
            }

            saga.Status = "Compensated";
            saga.FailureReason = $"SpotReservationFailed: {evt.Reason}";
            saga.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("[SagaChoreo-RS] Saga {CorrId} → Compensated.", evt.CorrelationId);
        }

        private async Task HandleAttendanceRecordedAsync(SagaAttendanceRecordedEvent evt, CancellationToken ct)
        {
            _logger.LogInformation("[SagaChoreo-RS] AttendanceRecorded. CorrelationId={CorrId}.", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

            var saga = await db.SagaChoreographyStates
                .FirstOrDefaultAsync(s => s.CorrelationId == evt.CorrelationId, ct);

            if (saga is null)
            {
                _logger.LogWarning("[SagaChoreo-RS] Saga state nije pronadjen za CorrelationId={CorrId}.", evt.CorrelationId);
                return;
            }

            if (saga.RegistrationId.HasValue)
            {
                // NOVO
                var reg = await db.Registrations.FindAsync(saga.RegistrationId.Value);
                if (reg is not null)
                {
                    reg.Status = "Confirmed";
                    _logger.LogInformation("[SagaChoreo-RS] Korak 4: Registracija {RegId} → CONFIRMED.", saga.RegistrationId);
                }
            }

            saga.Status = "Completed";
            saga.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            // Email notifikacija (best-effort)
            try
            {
                await _emailPublisher.EnqueueAsync(new EmailNotificationMessage
                {
                    RegistrationId = saga.RegistrationId ?? 0,
                    ParticipantFirstName = saga.ParticipantFirstName,
                    ParticipantLastName = saga.ParticipantLastName,
                    ParticipantEmail = saga.ParticipantEmail,
                    EventId = saga.EventId,
                    EventName = saga.EventName,
                    EventDateTime = DateTime.MinValue,
                    RegistrationDate = saga.RegistrationDate
                }, ct);

                _logger.LogInformation("[SagaChoreo-RS] Email notifikacija stavljena u red. CorrelationId={CorrId}.", evt.CorrelationId);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "[SagaChoreo-RS] Email nije stavljen u red (best-effort). CorrelationId={CorrId}.", evt.CorrelationId);
            }

            // Obavijesti EventService da finalizuje rezervaciju
            var confirmedEvt = new SagaRegistrationConfirmedEvent
            {
                CorrelationId = evt.CorrelationId,
                EventId = saga.EventId,
                OccurredAt = DateTime.UtcNow
            };

            await _publisher.PublishAsync(
                routingKey: _options.Value.EventServiceRoutingKey,
                payload: JsonSerializer.Serialize(confirmedEvt),
                messageType: nameof(SagaRegistrationConfirmedEvent),
                cancellationToken: ct);

            _logger.LogInformation("[SagaChoreo-RS] SagaRegistrationConfirmed objavljen. CorrelationId={CorrId}.", evt.CorrelationId);
        }

        private async Task HandleSpotReleasedAsync(SagaSpotReleasedEvent evt, CancellationToken ct)
        {
            _logger.LogWarning("[SagaChoreo-RS] SpotReleased (kompenzacija). CorrelationId={CorrId}.", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

            var saga = await db.SagaChoreographyStates
                .FirstOrDefaultAsync(s => s.CorrelationId == evt.CorrelationId, ct);

            if (saga is null)
            {
                _logger.LogWarning("[SagaChoreo-RS] Saga state nije pronadjen za CorrelationId={CorrId}.", evt.CorrelationId);
                return;
            }

            if (saga.RegistrationId.HasValue)
            {
                // NOVO
                var reg = await db.Registrations.FindAsync(saga.RegistrationId.Value);
                if (reg is not null)
                {
                    db.Registrations.Remove(reg);
                    _logger.LogInformation("[SagaChoreo-RS] Kompenzacija K1: obrisana PENDING registracija {RegId} (nakon SpotReleased).", saga.RegistrationId);
                }
            }

            saga.Status = "Compensated";
            saga.FailureReason = $"SpotReleased (attendance failed): {evt.Reason}";
            saga.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("[SagaChoreo-RS] Saga {CorrId} → Compensated (spot released).", evt.CorrelationId);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}