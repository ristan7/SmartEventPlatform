namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    /// <summary>
    /// Domenski događaj: Naziv stručnog događaja je promijenjen.
    /// </summary>
    public class EventRenamedDomainEvent : EventDomainEvent
    {
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
    }
}