using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEventPlatform.Contracts.Locations
{
    public class LocationDto
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }
}
