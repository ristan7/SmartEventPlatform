namespace SmartEventPlatformWeb.ViewModels.Registrations
{
    public class RegistrationDeleteViewModel
    {
        public long RegistrationId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string ParticipantFullName { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }
}
