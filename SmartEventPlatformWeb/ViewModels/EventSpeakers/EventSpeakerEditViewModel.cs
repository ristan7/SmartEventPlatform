using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartEventPlatformWeb.ViewModels.EventSpeakers
{
    public class EventSpeakerEditViewModel
    {
        public long EventSpeakerId { get; set; }
        public long EventId { get; set; }
        public long SpeakerId { get; set; }
        public long EventRoleId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }

        public IEnumerable<SelectListItem> Events { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Speakers { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> EventRoles { get; set; } = new List<SelectListItem>();
    }
}
