using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.DirectoryService.Data;
using SmartEventPlatform.DirectoryService.Models;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.DirectoryService.Messaging
{
    /// <summary>
    /// Consumes messages from the speaker-usage queue.
    /// Handles EventSpeakerAddedEvent and EventSpeakerRemovedEvent to maintain
    /// SpeakerUsageTrackers, which allow DirectoryService to know whether a speaker
    /// has active engagements without synchronous HTTP calls to EventService.
    /// </summary>
    public sealed class SpeakerUsageConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<SpeakerUsageRabbitMqOptions> _options;
        private readonly ILogger<SpeakerUsageConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public SpeakerUsageConsumerService(
            IServiceScopeFactory scopeFactory,
            IOptions<SpeakerUsageRabbitMqOptions> options,
            ILogger<SpeakerUsageConsumerService> logger)
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

            _logger.LogInformation(
                "SpeakerUsageConsumerService started. Listening on queue '{Queue}' (routing key: '{RoutingKey}').",
                mq.Queue, mq.RoutingKey);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDbContext>();

                var messageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString();
                var eventType = ea.BasicProperties.Type ?? string.Empty;
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                // Transaction starts BEFORE the idempotency check.
                // This makes the check and ProcessedMessage insert atomic.
                // Under at-least-once delivery, if two consumer instances receive
                // the same message concurrently, the unique constraint on MessageId
                // ensures only one commit succeeds — the other gets DbUpdateException
                // and requeues the message.
                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.MessageId == messageId, cancellationToken);

                if (!alreadyProcessed)
                {
                    if (eventType == nameof(EventSpeakerAddedEvent))
                    {
                        var evt = JsonSerializer.Deserialize<EventSpeakerAddedEvent>(body);
                        if (evt is not null)
                        {
                            // A speaker was added to an event — record the engagement
                            var exists = await db.SpeakerUsageTrackers
                                .AnyAsync(t => t.EventSpeakerId == evt.EventSpeakerId, cancellationToken);

                            if (!exists)
                            {
                                db.SpeakerUsageTrackers.Add(new SpeakerUsageTracker
                                {
                                    EventSpeakerId = evt.EventSpeakerId,
                                    SpeakerId = evt.SpeakerId
                                });
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Could not deserialize message. MessageId={MessageId}, EventType={EventType}.",
                                messageId, eventType);
                        }
                    }
                    else if (eventType == nameof(EventSpeakerRemovedEvent))
                    {
                        var evt = JsonSerializer.Deserialize<EventSpeakerRemovedEvent>(body);
                        if (evt is not null)
                        {
                            // The speaker was removed from the event — clear the engagement
                            var tracker = await db.SpeakerUsageTrackers
                                .FirstOrDefaultAsync(t => t.EventSpeakerId == evt.EventSpeakerId, cancellationToken);

                            if (tracker is not null)
                                db.SpeakerUsageTrackers.Remove(tracker);
                            else
                                _logger.LogWarning(
                                    "EventSpeakerRemovedEvent received for EventSpeakerId={EventSpeakerId}, but SpeakerUsageTracker does not exist.",
                                    evt.EventSpeakerId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Could not deserialize message. MessageId={MessageId}, EventType={EventType}.",
                                messageId, eventType);
                        }
                    }
                    else
                    {
                        // Unknown event type on this queue — log and ack without recording.
                        // Not recording allows future re-evaluation if support is added later.
                        _logger.LogWarning(
                            "Unknown event type '{EventType}', MessageId={MessageId}. Acknowledging without processing.",
                            eventType, messageId);

                        await tx.RollbackAsync(cancellationToken);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                        return;
                    }

                    // ProcessedMessage is inserted ONLY for successfully handled messages.
                    // This prevents duplicate processing under at-least-once delivery.
                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        MessageId = messageId,
                        EventType = eventType,
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Message processed successfully. EventType={EventType}, MessageId={MessageId}.",
                        eventType, messageId);
                }
                else
                {
                    // Duplicate — rollback and ack.
                    await tx.RollbackAsync(cancellationToken);
                    _logger.LogWarning(
                        "Duplicate message detected, skipping. MessageId={MessageId}.",
                        messageId);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing speaker-usage message. DeliveryTag={DeliveryTag}.", ea.DeliveryTag);
                if (_channel is not null)
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: true,
                        cancellationToken: cancellationToken);
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