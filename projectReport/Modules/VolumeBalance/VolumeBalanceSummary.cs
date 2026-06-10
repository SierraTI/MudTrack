namespace ProjectReport.Modules.VolumeBalance
{
    public class VolumeBalanceSummary
    {
        public double TheoreticalWell { get; set; }
        public double ActualWell { get; set; }
        public double Surface { get; set; }
        public double Variance => (ActualWell + Surface) - TheoreticalWell;
    }
}
