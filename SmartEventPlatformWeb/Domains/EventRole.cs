namespace SmartEventPlatformWeb.Domains
{
    public class EventRole
    {
        public long EventRoleId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
    }
}
