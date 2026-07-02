namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    
    public class EventRescheduledDomainEvent : EventDomainEvent
    {
        public DateTime OldDateTime { get; set; }
        public DateTime NewDateTime { get; set; }
        public int OldDurationInMinutes { get; set; }
        public int NewDurationInMinutes { get; set; }
    }
}