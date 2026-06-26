namespace SmartEventPlatform.DirectoryService.Messaging
{
    /// <summary>
    /// Configuration for the consumer that processes speaker-usage events
    /// (EventSpeakerAddedEvent and EventSpeakerRemovedEvent) published by EventService.
    /// </summary>
    public class SpeakerUsageRabbitMqOptions
    {
        public const string SectionName = "RabbitMqSpeakerUsage";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "smart-event.event-integration";
        public string Queue { get; set; } = "directory.speaker-usage.queue";
        public string RoutingKey { get; set; } = "event.speaker-usage.changed";
        public ushort PrefetchCount { get; set; } = 1;
    }
}