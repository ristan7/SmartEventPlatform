namespace SmartEventPlatform.Contracts.Integration
{
    public class EventCreatedEvent
    {
        public long EventId { get; set; }
        public long LocationId { get; set; }
    }
}