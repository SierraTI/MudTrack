using System;

namespace ProjectReport.Models
{
    public class VolSystem
    {
        public int VolSystemId { get; set; }

        public int EventFluidSystemId { get; set; }

        public double? PreviousVolume { get; set; }

        public double? CurrentVolume { get; set; }

        public double? Density { get; set; }

        public string? Remarks { get; set; }
    }
}