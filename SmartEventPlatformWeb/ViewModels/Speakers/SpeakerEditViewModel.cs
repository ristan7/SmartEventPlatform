using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.Speakers
{
    public class SpeakerEditViewModel
    {
        public long SpeakerId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot be longer than 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot be longer than 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Expertise areas cannot be longer than 500 characters.")]
        public string ExpertiseAreas { get; set; } = string.Empty;
    }
}
