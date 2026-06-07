using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.EventSpeakers
{
    public class EventSpeakerDto
    {
        public long EventSpeakerId { get; set; }
        public DateTime Time { get; set; }
        public string Topic { get; set; } = string.Empty;

        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;

        public long SpeakerId { get; set; }
        public string SpeakerFullName { get; set; } = string.Empty;
    }
}
