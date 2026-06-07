using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.EventTypes
{
    public class EventTypeDto
    {
        public long EventTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
