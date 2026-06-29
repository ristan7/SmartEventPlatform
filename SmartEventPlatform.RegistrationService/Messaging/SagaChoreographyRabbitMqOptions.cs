namespace SmartEventPlatform.RegistrationService.Messaging
{
    /// <summary>
    /// Konfiguracija RabbitMQ topologije za Saga Koreografiju.
    ///
    /// Sva tri servisa koriste isti exchange i iste queue nazive.
    /// Svaki servis cita ove opcije da bi znao gdje da objavljuje
    /// i odakle da cita poruke.
    ///
    /// BITNO: Svi publisheri i consumeri moraju deklarisati queues s
    /// IDENTICNIM argumentima — razlika uzrokuje PRECONDITION_FAILED u RabbitMQ.
    /// </summary>
    public class SagaChoreographyRabbitMqOptions
    {
        public const string SectionName = "SagaChoreography";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";

        // Exchange i DLX — isti za sve servise
        public string Exchange { get; set; } = "smart-event.saga-choreography";
        public string DeadLetterExchange { get; set; } = "smart-event.saga-choreography.dlx";

        // EventService queue — slusa: SagaChoreographyStarted, SagaAttendanceFailed, SagaRegistrationConfirmed
        public string EventServiceQueue { get; set; } = "saga-choreo.event-service.queue";
        public string EventServiceRoutingKey { get; set; } = "saga.event-service";
        public string EventServiceDlq { get; set; } = "saga-choreo.event-service.dlq";

        // DirectoryService queue — slusa: SagaSpotReserved
        public string DirectoryServiceQueue { get; set; } = "saga-choreo.directory-service.queue";
        public string DirectoryServiceRoutingKey { get; set; } = "saga.directory-service";
        public string DirectoryServiceDlq { get; set; } = "saga-choreo.directory-service.dlq";

        // RegistrationService queue — slusa: SagaSpotReservationFailed, SagaAttendanceRecorded, SagaSpotReleased
        public string RegistrationServiceQueue { get; set; } = "saga-choreo.registration-service.queue";
        public string RegistrationServiceRoutingKey { get; set; } = "saga.registration-service";
        public string RegistrationServiceDlq { get; set; } = "saga-choreo.registration-service.dlq";

        public ushort PrefetchCount { get; set; } = 10;
        public int MaxRetryCount { get; set; } = 10;
    }
}