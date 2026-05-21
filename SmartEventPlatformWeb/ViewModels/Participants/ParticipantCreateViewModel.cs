using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.Participants
{
    public class ParticipantCreateViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
