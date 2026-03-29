namespace SmartEventPlatformWeb.Domains
{
    public class Event
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }

        public long LocationId { get; set; }
        public Location? Location { get; set; }

        public ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
    }
}
