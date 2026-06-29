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

        public string LocationUsageRoutingKey { get; set; } = "event.location-usage.changed";
        public string LocationUsageQueue { get; set; } = "directory.location-usage.queue";

        public string SpeakerUsageRoutingKey { get; set; } = "event.speaker-usage.changed";
        public string SpeakerUsageQueue { get; set; } = "directory.speaker-usage.queue";

        // DLQ — mora biti konzistentan s DirectoryService consumerima
        public string DeadLetterExchange { get; set; } = "smart-event.dlx";
        public string LocationUsageDlq { get; set; } = "directory.location-usage.dlq";
        public string SpeakerUsageDlq { get; set; } = "directory.speaker-usage.dlq";
    }
}