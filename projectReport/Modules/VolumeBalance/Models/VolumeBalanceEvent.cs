using System;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolumeBalanceEvent
    {
        public int Id { get; set; }

        public string EventTime { get; set; }

        public string Description { get; set; }

        public double CurrentDepth { get; set; }

        public string Activity { get; set; }

        public int IdW { get; set; }
    }
}