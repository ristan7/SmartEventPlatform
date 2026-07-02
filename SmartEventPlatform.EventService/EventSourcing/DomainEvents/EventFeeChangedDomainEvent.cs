namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    
    public class EventFeeChangedDomainEvent : EventDomainEvent
    {
        public decimal OldFee { get; set; }
        public decimal NewFee { get; set; }
    }
}