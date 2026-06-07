using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Speakers
{
    public class SpeakerDto
    {
        public long SpeakerId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExpertiseAreas { get; set; } = string.Empty;

        public string FullName => FirstName + " " + LastName;
    }
}
