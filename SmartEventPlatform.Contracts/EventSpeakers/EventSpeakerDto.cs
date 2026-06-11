namespace SmartEventPlatform.Contracts.EventSpeakers
{
    public class EventSpeakerDto
    {
        public long EventSpeakerId { get; set; }

        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;

        public long SpeakerId { get; set; }
        public string SpeakerFullName { get; set; } = string.Empty;

        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
