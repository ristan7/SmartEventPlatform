namespace SmartEventPlatformWeb.ViewModels.Registrations
{
    public class RegistrationListViewModel
    {
        public long RegistrationId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string ParticipantFullName { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
    }
}
