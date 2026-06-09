using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Events
{
    public class EventRegistrationInfoDto
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int Capacity { get; set; }
        public bool Exists { get; set; }
    }
}
