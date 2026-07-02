namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    
    public class EventLocationChangedDomainEvent : EventDomainEvent
    {
        public long OldLocationId { get; set; }
        public string OldLocationName { get; set; } = string.Empty;
        public long NewLocationId { get; set; }
        public string NewLocationName { get; set; } = string.Empty;
    }
}