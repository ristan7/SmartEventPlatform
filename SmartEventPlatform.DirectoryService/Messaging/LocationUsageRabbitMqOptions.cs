namespace SmartEventPlatform.DirectoryService.Messaging
{
    /// <summary>
    /// Configuration for the consumer that processes location-usage events
    /// (EventCreatedEvent and EventDeletedEvent) published by EventService.
    /// </summary>
    public class LocationUsageRabbitMqOptions
    {
        public const string SectionName = "RabbitMqLocationUsage";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "smart-event.event-integration";
        public string Queue { get; set; } = "directory.location-usage.queue";
        public string RoutingKey { get; set; } = "event.location-usage.changed";
        public ushort PrefetchCount { get; set; } = 1;
    }
}