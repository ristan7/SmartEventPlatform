namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    /// <summary>
    /// Domenski događaj: Cijena kotizacije stručnog događaja je promijenjena.
    /// </summary>
    public class EventFeeChangedDomainEvent : EventDomainEvent
    {
        public decimal OldFee { get; set; }
        public decimal NewFee { get; set; }
    }
}