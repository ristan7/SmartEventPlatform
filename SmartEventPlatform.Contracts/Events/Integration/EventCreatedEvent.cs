namespace SmartEventPlatform.Contracts.Events.Integration
{
    public class EventCreatedEvent
    {
        public long EventId { get; set; }
        public long LocationId { get; set; }
    }
}