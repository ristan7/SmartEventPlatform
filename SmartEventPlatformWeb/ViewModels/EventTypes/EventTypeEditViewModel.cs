using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.EventTypes
{
    public class EventTypeEditViewModel
    {
        public long EventTypeId { get; set; }

        [Required(ErrorMessage = "Event type name is required.")]
        [StringLength(100, ErrorMessage = "Event type name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
