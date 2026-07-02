namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    
    public class EventRenamedDomainEvent : EventDomainEvent
    {
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
    }
}