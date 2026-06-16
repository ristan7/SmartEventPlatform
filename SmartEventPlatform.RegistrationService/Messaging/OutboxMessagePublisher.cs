using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.RegistrationService.Data;

namespace SmartEventPlatform.RegistrationService.Messaging
{
    public class OutboxMessagePublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxMessagePublisher> _logger;

        public OutboxMessagePublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxMessagePublisher> logger)
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
                    var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

                    var pending = await db.OutboxMessages
                        .OrderBy(x => x.CreatedAt)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var message in pending)
                    {
                        try
                        {
                            await publisher.PublishAsync(
                                message.Payload,
                                message.MessageId,
                                message.EventType,
                                stoppingToken);

                            db.OutboxMessages.Remove(message);
                            await db.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Neuspesno slanje outbox poruke ID={Id}", message.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Greska u OutboxMessagePublisher.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}