using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Participants
{
    public class ParticipantDto
    {
        public long ParticipantId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string FullName => FirstName + " " + LastName;
    }
}
