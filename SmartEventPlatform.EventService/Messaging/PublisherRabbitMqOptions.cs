namespace SmartEventPlatform.EventService.Messaging
{
    public class PublisherRabbitMqOptions
    {
        public const string SectionName = "RabbitMqPublisher";

        // Connection
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";

        // Exchange — shared by both flows
        public string Exchange { get; set; } = "smart-event.event-integration";

        // Location usage flow (EventCreatedEvent, EventDeletedEvent)
        public string LocationUsageRoutingKey { get; set; } = "event.location-usage.changed";
        public string LocationUsageQueue { get; set; } = "directory.location-usage.queue";

        // Speaker usage flow (EventSpeakerAddedEvent, EventSpeakerRemovedEvent)
        public string SpeakerUsageRoutingKey { get; set; } = "event.speaker-usage.changed";
        public string SpeakerUsageQueue { get; set; } = "directory.speaker-usage.queue";
    }
}