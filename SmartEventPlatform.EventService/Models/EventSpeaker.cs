namespace SmartEventPlatformWeb.EventService.Models
{
    public class EventSpeaker
    {
        public long EventSpeakerId { get; set; }
        public DateTime Time { get; set; }
        public string Topic { get; set; } = string.Empty;

        public long EventId { get; set; }
        public Event? Event { get; set; }

        public long SpeakerId { get; set; }
        public Speaker? Speaker { get; set; }

    }
}
