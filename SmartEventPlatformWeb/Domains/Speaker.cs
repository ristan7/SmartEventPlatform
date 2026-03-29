namespace SmartEventPlatformWeb.Domains
{
    public class Speaker
    {
        public long SpekaerId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExpertiseAreas { get; set; } = string.Empty;

        public ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
    }
}
