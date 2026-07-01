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
    /// <summary>
    /// DirectoryService konzumira dogadjaje iz saga-choreo.directory-service.queue.
    ///
    /// Poruke koje prima:
    ///   SagaSpotReserved → Zabiljezi prisustvo na lokaciji (Korak 3)
    ///                    → Uspjeh: objavi SagaAttendanceRecorded
    ///                    → Greska: objavi SagaAttendanceFailed
    /// </summary>
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

            await _channel.QueueDeclareAsync(mq.DirectoryServiceDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(mq.DirectoryServiceDlq, mq.DeadLetterExchange, mq.DirectoryServiceRoutingKey, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(
                mq.DirectoryServiceQueue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", mq.DeadLetterExchange } },
                cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(mq.DirectoryServiceQueue, mq.Exchange, mq.DirectoryServiceRoutingKey, cancellationToken: stoppingToken);

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
                    _logger.LogInformation("[SagaChoreo-DS] Primljena poruka Type={Type}.", messageType);

                    switch (messageType)
                    {
                        case nameof(SagaSpotReservedEvent):
                            await HandleSpotReservedAsync(
                                JsonSerializer.Deserialize<SagaSpotReservedEvent>(payload)!,
                                stoppingToken);
                            break;
                        default:
                            _logger.LogWarning("[SagaChoreo-DS] Nepoznat tip: {Type}. Ack-ujemo.", messageType);
                            break;
                    }

                    _retryCounts.TryRemove(messageId, out int _);
                    await _channel.BasicAckAsync(args.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SagaChoreo-DS] Greska pri obradi Type={Type}. Pokusaj={Retry}.",
                        messageType, retryCount + 1);

                    if (retryCount >= mq.MaxRetryCount)
                    {
                        _retryCounts.TryRemove(messageId, out int _);
                        await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
                    }
                    else
                    {
                        _retryCounts[messageId] = retryCount + 1;
                        await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
                    }
                }
            };

            await _channel.BasicConsumeAsync(mq.DirectoryServiceQueue, autoAck: false, consumer: consumer, stoppingToken);
            _logger.LogInformation("[SagaChoreo-DS] Consumer pokrenut na '{Queue}'.", mq.DirectoryServiceQueue);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleSpotReservedAsync(SagaSpotReservedEvent evt, CancellationToken ct)
        {
            _logger.LogInformation("[SagaChoreo-DS] SpotReserved. CorrelationId={CorrId}, LocationId={LocationId}.",
                evt.CorrelationId, evt.LocationId);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DirectoryDbContext>();

            var locationExists = await db.Locations.AnyAsync(l => l.LocationId == evt.LocationId, ct);
            if (!locationExists)
            {
                _logger.LogWarning("[SagaChoreo-DS] Lokacija {LocationId} ne postoji.", evt.LocationId);
                await PublishAttendanceFailedAsync(evt, $"Lokacija {evt.LocationId} ne postoji.", ct);
                return;
            }

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var tracker = await db.LocationRegistrationTrackers
                    .FirstOrDefaultAsync(t => t.LocationId == evt.LocationId, ct);

                if (tracker is null)
                {
                    db.LocationRegistrationTrackers.Add(new LocationRegistrationTracker
                    {
                        LocationId = evt.LocationId,
                        RegistrationCount = 1
                    });
                    _logger.LogInformation("[SagaChoreo-DS] LocationRegistrationTracker kreiran. LocationId={LocationId}, Count=1.", evt.LocationId);
                }
                else
                {
                    tracker.RegistrationCount++;
                    _logger.LogInformation("[SagaChoreo-DS] LocationRegistrationTracker incrementiran. LocationId={LocationId}, Count={Count}.",
                        evt.LocationId, tracker.RegistrationCount);
                }

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "[SagaChoreo-DS] Greska pri bilježenju prisustva za LocationId={LocationId}.", evt.LocationId);
                await PublishAttendanceFailedAsync(evt, $"Greska pri bilježenju prisustva: {ex.Message}", ct);
                return;
            }

            // Uspjeh: objavi SagaAttendanceRecorded
            var attendanceRecordedEvt = new SagaAttendanceRecordedEvent
            {
                CorrelationId = evt.CorrelationId,
                LocationId = evt.LocationId,
                OccurredAt = DateTime.UtcNow
            };
            await _publisher.PublishAsync(
                _options.Value.RegistrationServiceRoutingKey,
                JsonSerializer.Serialize(attendanceRecordedEvt),
                nameof(SagaAttendanceRecordedEvent), ct);

            _logger.LogInformation("[SagaChoreo-DS] SagaAttendanceRecorded objavljen. CorrelationId={CorrId}.", evt.CorrelationId);
        }

        private async Task PublishAttendanceFailedAsync(SagaSpotReservedEvent evt, string reason, CancellationToken ct)
        {
            var failedEvt = new SagaAttendanceFailedEvent
            {
                CorrelationId = evt.CorrelationId,
                EventId = evt.EventId,
                LocationId = evt.LocationId,
                Reason = reason,
                OccurredAt = DateTime.UtcNow
            };
            await _publisher.PublishAsync(
                _options.Value.EventServiceRoutingKey,
                JsonSerializer.Serialize(failedEvt),
                nameof(SagaAttendanceFailedEvent), ct);
            _logger.LogWarning("[SagaChoreo-DS] SagaAttendanceFailed objavljen. Razlog='{Reason}'.", reason);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}