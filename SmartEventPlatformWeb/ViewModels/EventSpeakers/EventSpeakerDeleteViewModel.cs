namespace SmartEventPlatformWeb.ViewModels.EventSpeakers
{
    public class EventSpeakerDeleteViewModel
    {
        public long EventSpeakerId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string SpeakerFullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
