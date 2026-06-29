namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    /// <summary>
    /// Domenski događaj: Stručni događaj je otkazan.
    /// Otkazan događaj se ne može više mijenjati — poslovno pravilo.
    /// </summary>
    public class EventCancelledDomainEvent : EventDomainEvent
    {
        public string Reason { get; set; } = string.Empty;
    }
}