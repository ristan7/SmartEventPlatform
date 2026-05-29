using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.EventTypes
{
    public class EventTypeCreateViewModel
    {
        [Required(ErrorMessage = "Event type name is required.")]
        [StringLength(20, ErrorMessage = "Event type name cannot be longer than 20 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
