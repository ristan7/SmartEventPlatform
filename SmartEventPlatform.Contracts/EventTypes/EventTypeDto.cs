using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.EventTypes
{
    public class EventTypeDto
    {
        public long EventTypeId { get; set; }

        [Required(ErrorMessage = "Event type name is required.")]
        [StringLength(100, ErrorMessage = "Event type name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
