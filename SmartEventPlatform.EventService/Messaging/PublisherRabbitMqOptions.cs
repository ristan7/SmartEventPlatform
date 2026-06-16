namespace SmartEventPlatform.EventService.Messaging
{
    public class PublisherRabbitMqOptions
    {
        public const string SectionName = "RabbitMqPublisher";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "event.events";
        public string RoutingKey { get; set; } = "event.event";
        public string Queue { get; set; } = "event.events.queue";
    }
}