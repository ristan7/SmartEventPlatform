namespace SmartEventPlatformWeb.ViewModels.Events
{
    public class EventListViewModel
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public string LocationName { get; set; } = string.Empty;
    }
}
