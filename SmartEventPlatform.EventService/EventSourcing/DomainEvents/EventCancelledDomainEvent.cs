namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    
    public class EventCancelledDomainEvent : EventDomainEvent
    {
        public string Reason { get; set; } = string.Empty;
    }
}