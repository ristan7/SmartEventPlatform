namespace SmartEventPlatformWeb.ViewModels.Events
{
    public class EventDetailsViewModel
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public string Agenda { get; set; } = string.Empty;
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }

        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;

    }
}
