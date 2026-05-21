namespace SmartEventPlatformWeb.ViewModels.Participants
{
    public class ParticipantEditViewModel
    {
        public long ParticipantId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
