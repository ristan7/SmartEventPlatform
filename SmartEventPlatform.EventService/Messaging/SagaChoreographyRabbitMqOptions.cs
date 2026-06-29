namespace SmartEventPlatform.EventService.Messaging
{
    public class SagaChoreographyRabbitMqOptions
    {
        public const string SectionName = "SagaChoreography";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";

        public string Exchange { get; set; } = "smart-event.saga-choreography";
        public string DeadLetterExchange { get; set; } = "smart-event.saga-choreography.dlx";

        public string EventServiceQueue { get; set; } = "saga-choreo.event-service.queue";
        public string EventServiceRoutingKey { get; set; } = "saga.event-service";
        public string EventServiceDlq { get; set; } = "saga-choreo.event-service.dlq";

        public string DirectoryServiceQueue { get; set; } = "saga-choreo.directory-service.queue";
        public string DirectoryServiceRoutingKey { get; set; } = "saga.directory-service";
        public string DirectoryServiceDlq { get; set; } = "saga-choreo.directory-service.dlq";

        public string RegistrationServiceQueue { get; set; } = "saga-choreo.registration-service.queue";
        public string RegistrationServiceRoutingKey { get; set; } = "saga.registration-service";
        public string RegistrationServiceDlq { get; set; } = "saga-choreo.registration-service.dlq";

        public ushort PrefetchCount { get; set; } = 10;
        public int MaxRetryCount { get; set; } = 10;
    }
}