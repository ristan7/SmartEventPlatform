using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartEventPlatformWeb.ViewModels.Registrations
{
    public class RegistrationEditViewModel
    {
        public long RegistrationId { get; set; }
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public DateTime RegistrationDate { get; set; }

        public IEnumerable<SelectListItem> Events { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Participants { get; set; } = new List<SelectListItem>();
    }
}
