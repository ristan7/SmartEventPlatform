using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Data;

namespace SmartEventPlatform.EventService.Messaging
{
    public class OutboxMessagePublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxMessagePublisher> _logger;

        public OutboxMessagePublisher(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxMessagePublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

                    var pending = await db.OutboxMessages
                        .OrderBy(x => x.CreatedAt)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var message in pending)
                    {
                        try
                        {
                            // The routing key is stored on the message itself.
                            // This publisher has no routing logic — it reads the key
                            // that was set when the outbox message was created.
                            await publisher.PublishAsync(
                                payload: message.Payload,
                                messageId: message.MessageId,
                                eventType: message.EventType,
                                routingKey: message.RoutingKey,
                                cancellationToken: stoppingToken);

                            db.OutboxMessages.Remove(message);
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Outbox message published. EventType={EventType}, MessageId={MessageId}, RoutingKey={RoutingKey}",
                                message.EventType, message.MessageId, message.RoutingKey);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "Failed to publish outbox message. Id={Id}, EventType={EventType}, RoutingKey={RoutingKey}",
                                message.Id, message.EventType, message.RoutingKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in OutboxMessagePublisher.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}