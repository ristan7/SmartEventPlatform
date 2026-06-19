namespace SmartEventPlatform.EventService.Messaging
{
    public class PublisherRabbitMqOptions
    {
        public const string SectionName = "RabbitMqPublisher";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "smart-event.event-integration";
        public string RoutingKey { get; set; } = "event.directory-usage.changed";
        public string Queue { get; set; } = "directory.event-usage.queue";
    }
}