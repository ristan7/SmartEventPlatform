namespace SmartEventPlatformWeb.ViewModels.EventSpeakers
{
    public class EventSpeakerDetailsViewModel
    {
        public long EventSpeakerId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string SpeakerFullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public long EventId { get; set; }
        public long SpeakerId { get; set; }
        public long EventRoleId { get; set; }
    }
}
