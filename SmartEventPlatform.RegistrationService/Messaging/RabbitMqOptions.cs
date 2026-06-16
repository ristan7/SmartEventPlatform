namespace SmartEventPlatform.RegistrationService.Messaging
{
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "registration.events";
        public string RoutingKey { get; set; } = "registration.event";
        public string Queue { get; set; } = "registration.events.queue";
    }
}