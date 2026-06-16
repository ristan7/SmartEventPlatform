namespace SmartEventPlatform.EventService.Messaging
{
    public class ConsumerRabbitMqOptions
    {
        public const string SectionName = "RabbitMqConsumer";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "registration.events";
        public string Queue { get; set; } = "registration.events.queue";
        public string RoutingKey { get; set; } = "registration.event";
        public ushort PrefetchCount { get; set; } = 1;
    }
}