using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Speakers
{
    public class SpeakerEventItemDto
    {
        public long EventSpeakerId { get; set; }
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
