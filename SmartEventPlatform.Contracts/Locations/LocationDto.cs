using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Locations
{
    public class LocationDto
    {
        public long LocationId { get; set; }

        [Required(ErrorMessage = "Location name is required.")]
        [StringLength(150, ErrorMessage = "Location name cannot be longer than 150 characters.")]
        public string LocationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(250, ErrorMessage = "Address cannot be longer than 250 characters.")]
        public string Address { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
        public int Capacity { get; set; }
    }
}
