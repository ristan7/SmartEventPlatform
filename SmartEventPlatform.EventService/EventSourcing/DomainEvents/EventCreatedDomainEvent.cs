namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    public class EventCreatedDomainEvent : EventDomainEvent
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }
        public long LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public long EventTypeId { get; set; }
    }
}