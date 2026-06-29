namespace SmartEventPlatform.Contracts.Integration
{
    /// <summary>
    /// KORAK 1: RegistrationService objavljuje ovaj dogadjaj kada kreira PENDING registraciju.
    /// EventService slusa ovaj dogadjaj i pokusava da rezervise mjesto.
    /// </summary>
    public class SagaChoreographyStartedEvent
    {
        /// <summary>Jedinstveni identifikator ove Saga instance — prati se kroz sve servise.</summary>
        public Guid CorrelationId { get; set; }

        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public long LocationId { get; set; }
        public DateTime RegistrationDate { get; set; }

        // Podaci potrebni za email notifikaciju (RegistrationService ih cuva u SagaChoreographyState)
        public string ParticipantFirstName { get; set; } = string.Empty;
        public string ParticipantLastName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }
    }
}