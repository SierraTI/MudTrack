using System;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolumeSummaryRow
    {
        public string Name { get; set; } = string.Empty;
        public string Previous { get; set; } = "0";
        public string Current { get; set; } = "0";
    }
}
