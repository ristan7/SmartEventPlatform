namespace SmartEventPlatform.RegistrationService.Models
{
    public class Registration
    {
        public long RegistrationId { get; set; }
        public DateTime RegistrationDate { get; set; }

        public long EventId { get; set; }

        public long ParticipantId { get; set; }
        public Participant? Participant { get; set; }

    }
}
