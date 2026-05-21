namespace SmartEventPlatformWeb.Domains
{
    public class Event
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }

        public long LocationId { get; set; }
        public Location? Location { get; set; }

        public long EventTypeId { get; set; }
        public EventType? EventType { get; set; }

        public ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
