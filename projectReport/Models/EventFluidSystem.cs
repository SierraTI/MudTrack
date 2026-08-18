using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReport.Models
{
    public class EventFluidSystem
    {

        public int EventFluidSystemId { get; set; }

        public int VolumeBalanceEventId { get; set; }

        public int PitNameId { get; set; }

        public int PitSystemId { get; set; }

        public int? FluidTypeId { get; set; }

        public string? FluidSubType { get; set; }
    }
}