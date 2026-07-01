namespace SmartEventPlatform.Contracts.Integration
{
    
    public class SagaAttendanceRecordedEvent
    {
        public Guid CorrelationId { get; set; }
        public long LocationId { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    
    public class SagaAttendanceFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public long LocationId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    
    public class SagaRegistrationConfirmedEvent
    {
        public Guid CorrelationId { get; set; }
        public long EventId { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}