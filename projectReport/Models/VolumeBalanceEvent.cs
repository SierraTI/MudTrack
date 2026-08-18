using System;

namespace ProjectReport.Models
{
    public class VolumeBalanceEvent
    {
        public int VolumeBalanceEventId { get; set; }

        public int VolumeBalanceId { get; set; }

        public int EventNo { get; set; }

        public DateTime EventDateTime { get; set; }

        public string Activity { get; set; }

        public double? CurrentDepth { get; set; }

        public string Description { get; set; }

        public string Remarks { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}