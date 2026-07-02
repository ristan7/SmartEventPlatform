namespace SmartEventPlatform.RegistrationService.Messaging
{
    public class EmailRabbitMqOptions
    {
        public const string SectionName = "RabbitMqEmail";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "smart-event.email-integration";
        public string Queue { get; set; } = "registration.email.queue";
        public string RoutingKey { get; set; } = "registration.email.notify";
        public ushort PrefetchCount { get; set; } = 1;

        public string DeadLetterExchange { get; set; } = "smart-event.dlx";
        public string DeadLetterQueue { get; set; } = "registration.email.dlq";
        public int MaxRetryCount { get; set; } = 5;

        public int MaxEmailsPerMinute { get; set; } = 10;
        public string OutboxFolder { get; set; } = "outbox";
    }
}