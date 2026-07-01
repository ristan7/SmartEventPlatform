namespace SmartEventPlatform.RegistrationService.Messaging
{
    public class EmailRabbitMqOptions
    {
        public const string SectionName = "RabbitMqEmail";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Queue { get; set; } = "registration.email.queue";

        public int MaxEmailsPerMinute { get; set; } = 10;
        public string OutboxFolder { get; set; } = "outbox";
    }
}