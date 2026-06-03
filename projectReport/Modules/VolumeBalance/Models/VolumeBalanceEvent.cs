using System;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolumeBalanceEvent
    {
        public int Id { get; set; }

        public string Hora { get; set; }

        public string Description { get; set; }

        public string CurrentDepth { get; set; }

        public string Activity { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}