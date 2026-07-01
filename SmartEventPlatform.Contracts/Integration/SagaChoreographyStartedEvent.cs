namespace SmartEventPlatform.Contracts.Integration
{
    
    public class SagaChoreographyStartedEvent
    {
        public Guid CorrelationId { get; set; }

        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public long LocationId { get; set; }
        public DateTime RegistrationDate { get; set; }

        
        public string ParticipantFirstName { get; set; } = string.Empty;
        public string ParticipantLastName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }
    }
}