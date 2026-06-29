namespace SmartEventPlatform.RegistrationService.Messaging
{
    /// <summary>
    /// Payload emaila koji se stavlja na email queue pri kreiranju registracije.
    /// Sadrzi sve podatke potrebne za generisanje emaila — worker ne treba
    /// dodatne pozive bazi ili servisima.
    /// </summary>
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