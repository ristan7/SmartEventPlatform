namespace SmartEventPlatform.EventService.Models
{
    public class EventType
    {
        public long EventTypeId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
