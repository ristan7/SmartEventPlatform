namespace SmartEventPlatform.Contracts.Integration
{
    public class EventDeletedEvent
    {
        public long EventId { get; set; }
        public long LocationId { get; set; }
    }
}