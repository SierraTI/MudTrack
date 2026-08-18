using System;

namespace ProjectReport.Models
{
    public class VolumeBalance
    {
        public int VolumeBalanceId { get; set; }

        public int WellId { get; set; }

        public string ReportDate { get; set; }

        public string Shift { get; set; }

        public string Status { get; set; }

        public string Engineer { get; set; }

        public string Remarks { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }

}
