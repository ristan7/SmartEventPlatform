namespace SmartEventPlatform.Contracts.Integration
{
    /// <summary>
    /// KORAK 3 (uspjeh): DirectoryService zabiljezio prisustvo na lokaciji.
    /// RegistrationService slusa i POTVRDUJE registraciju.
    /// </summary>
    public class SagaAttendanceRecordedEvent
    {
        public Guid CorrelationId { get; set; }
        public long LocationId { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    /// <summary>
    /// KORAK 3 (neuspjeh): DirectoryService NIJE uspio zabiljeZiti prisustvo.
    /// EventService slusa i OSLOBADJA privremenu rezervaciju → objavljuje SagaSpotReleased.
    /// </summary>
    public class SagaAttendanceFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public long LocationId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    /// <summary>
    /// KORAK 4: RegistrationService potvrdio registraciju (status → Confirmed).
    /// EventService slusa i finalizuje privremenu rezervaciju u EventRegistrationTracker.
    /// </summary>
    public class SagaRegistrationConfirmedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}