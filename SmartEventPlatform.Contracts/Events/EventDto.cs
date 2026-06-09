using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Events
{
    public class EventDto
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }

        public long LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        public int Capacity { get; set; }

        public long EventTypeId { get; set; }
        public string EventTypeName { get; set; } = string.Empty;

        public List<string> Speakers { get; set; } = new();
    }
}
