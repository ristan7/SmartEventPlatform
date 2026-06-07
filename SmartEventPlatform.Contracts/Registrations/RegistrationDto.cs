using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Registrations
{
    public class RegistrationDto
    {
        public long RegistrationId { get; set; }
        public DateTime RegistrationDate { get; set; }

        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;

        public long ParticipantId { get; set; }
        public string ParticipantFullName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
    }
}
