namespace SmartEventPlatform.RegistrationService.Messaging
{
    public class EmailNotificationMessage
    {
        public long RegistrationId { get; set; }
        public string ParticipantFirstName { get; set; } = string.Empty;
        public string ParticipantLastName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}