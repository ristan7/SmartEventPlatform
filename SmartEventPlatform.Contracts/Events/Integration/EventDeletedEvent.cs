namespace SmartEventPlatform.Contracts.Events.Integration
{
    public class EventDeletedEvent
    {
        public long EventId { get; set; }
        public long LocationId { get; set; }
    }
}