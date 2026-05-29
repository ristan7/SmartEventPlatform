using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.Registrations
{
    public class RegistrationCreateViewModel
    {
        [Range(1, long.MaxValue, ErrorMessage = "Event is required.")]
        public long EventId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Participant is required.")]
        public long ParticipantId { get; set; }

        [Required(ErrorMessage = "Registration date is required.")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public IEnumerable<SelectListItem> Events { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Participants { get; set; } = new List<SelectListItem>();
    }
}
