namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    /// <summary>
    /// Domenski događaj: Datum, vrijeme i trajanje stručnog događaja su promijenjeni.
    /// </summary>
    public class EventRescheduledDomainEvent : EventDomainEvent
    {
        public DateTime OldDateTime { get; set; }
        public DateTime NewDateTime { get; set; }
        public int OldDurationInMinutes { get; set; }
        public int NewDurationInMinutes { get; set; }
    }
}