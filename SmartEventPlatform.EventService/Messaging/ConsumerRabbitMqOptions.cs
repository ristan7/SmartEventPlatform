namespace SmartEventPlatform.EventService.Messaging
{
    public class ConsumerRabbitMqOptions
    {
        public const string SectionName = "RabbitMqConsumer";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "smart-event.registration-integration";
        public string Queue { get; set; } = "event.registration-usage.queue";
        public string RoutingKey { get; set; } = "registration.event-usage.changed";
        public ushort PrefetchCount { get; set; } = 1;
    }
}