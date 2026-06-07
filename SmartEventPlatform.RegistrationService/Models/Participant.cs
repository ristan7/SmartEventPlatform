namespace SmartEventPlatformWeb.RegistrationService.Models
{
    public class Participant
    {
        public long ParticipantId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
