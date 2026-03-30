namespace SmartEventPlatformWeb.ViewModels.Speakers
{
    public class SpeakerEventItemViewModel
    {
        public long EventSpeakerId { get; set; }
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
