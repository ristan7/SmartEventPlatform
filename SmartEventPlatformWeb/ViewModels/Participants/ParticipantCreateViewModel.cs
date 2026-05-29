using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.Participants
{
    public class ParticipantCreateViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot be longer than 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot be longer than 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is not valid.")]
        [StringLength(200, ErrorMessage = "Email cannot be longer than 200 characters.")]
        public string Email { get; set; } = string.Empty;
    }
}
