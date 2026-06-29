namespace SmartEventPlatform.Contracts.Integration
{
    /// <summary>
    /// KORAK 2 (uspjeh): EventService rezervisao mjesto za Sagu.
    /// DirectoryService slusa ovaj dogadjaj i biljezi prisustvo.
    /// </summary>
    public class SagaSpotReservedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public long LocationId { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    /// <summary>
    /// KORAK 2 (neuspjeh): EventService NIJE uspio rezervisati mjesto (kapacitet popunjen).
    /// RegistrationService slusa i PONISTAVA PENDING registraciju — Saga zavrsena bez uspjeha.
    /// </summary>
    public class SagaSpotReservationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    /// <summary>
    /// KOMPENZACIJA KORAKA 2: EventService je oslobodio privremenu rezervaciju.
    /// Objavljuje se kad DirectoryService javi AttendanceFailed.
    /// RegistrationService slusa i brise PENDING registraciju.
    /// </summary>
    public class SagaSpotReleasedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}