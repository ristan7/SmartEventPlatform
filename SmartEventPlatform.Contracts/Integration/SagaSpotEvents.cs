namespace SmartEventPlatform.Contracts.Integration
{
    public class SagaSpotReservedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public long LocationId { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    
    public class SagaSpotReservationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    
    public class SagaSpotReleasedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}