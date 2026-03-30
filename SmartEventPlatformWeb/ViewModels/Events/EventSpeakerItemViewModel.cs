namespace SmartEventPlatformWeb.ViewModels.Events
{
    public class EventSpeakerItemViewModel
    {
        public long EventSpeakerId { get; set; }
        public string SpeakerFullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
