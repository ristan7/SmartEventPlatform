using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Registrations
{
    public class RegistrationCreateUpdateDto
    {
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
