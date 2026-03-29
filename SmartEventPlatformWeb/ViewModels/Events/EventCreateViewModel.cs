using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartEventPlatformWeb.ViewModels.Events
{
    public class EventCreateViewModel
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public string Agenda { get; set; } = string.Empty;
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }

        public long LocationId { get; set; }
        
        public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
    }
}
