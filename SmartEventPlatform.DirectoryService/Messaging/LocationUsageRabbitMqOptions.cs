namespace SmartEventPlatform.DirectoryService.Messaging
{
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

        public string DeadLetterExchange { get; set; } = "smart-event.dlx";
        public string DeadLetterQueue { get; set; } = "directory.location-usage.dlq";
        public int MaxRetryCount { get; set; } = 10;
    }
}