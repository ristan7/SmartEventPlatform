using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartEventPlatformWeb.ViewModels.Registrations
{
    public class RegistrationCreateViewModel
    {
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public IEnumerable<SelectListItem> Events { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Participants { get; set; } = new List<SelectListItem>();
    }
}
