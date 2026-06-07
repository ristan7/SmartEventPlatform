using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Registrations
{
    public class RegistrationCreateUpdateDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "Event is required.")]
        public long EventId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Participant is required.")]
        public long ParticipantId { get; set; }

        [Required(ErrorMessage = "Registration date is required.")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
