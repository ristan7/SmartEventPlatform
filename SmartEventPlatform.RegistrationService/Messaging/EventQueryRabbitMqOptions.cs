namespace SmartEventPlatform.RegistrationService.Messaging
{
    public class EventQueryRabbitMqOptions
    {
        public const string SectionName = "RabbitMqEventQuery";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string RequestQueue { get; set; } = "event.query.request.queue";
        public string ReplyQueue { get; set; } = "event.query.reply.queue";
        public int TimeoutSeconds { get; set; } = 5;
    }
}