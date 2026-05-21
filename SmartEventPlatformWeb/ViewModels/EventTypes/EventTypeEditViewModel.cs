using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.EventTypes
{
    public class EventTypeEditViewModel
    {
        public long EventTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
