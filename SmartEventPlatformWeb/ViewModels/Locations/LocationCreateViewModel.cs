using System.ComponentModel.DataAnnotations;

namespace SmartEventPlatformWeb.ViewModels.Locations
{
    public class LocationCreateViewModel
    {
        [Required(ErrorMessage = "Location name is required.")]
        [StringLength(100, ErrorMessage = "Location name cannot be longer than 100 characters.")]
        public string LocationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(250, ErrorMessage = "Address cannot be longer than 250 characters.")]
        public string Address { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
        public int Capacity { get; set; }
    }
}
