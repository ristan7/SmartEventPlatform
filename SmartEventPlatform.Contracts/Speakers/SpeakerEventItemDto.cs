namespace SmartEventPlatform.Contracts.Speakers
{
    public class SpeakerEventItemDto
    {
        public long EventSpeakerId { get; set; }
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
