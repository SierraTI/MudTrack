namespace ProjectReport.Models.Geometry.ThermalGradient
{
    /// <summary>
    /// Represents a gradient segment between two thermal points
    /// </summary>
    public class SegmentGradient
    {
        public double StartTVD { get; set; }
        public double EndTVD { get; set; }
        public double StartTemp { get; set; }
        public double EndTemp { get; set; }
        public double Gradient { get; set; } // °F per 100 ft
        public string ColorCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public SegmentGradient(double startTVD, double endTVD, double startTemp, double endTemp, double gradient)
        {
            StartTVD = startTVD;
            EndTVD = endTVD;
            StartTemp = startTemp;
            EndTemp = endTemp;
            Gradient = gradient;
            
            // Color coding based on gradient value
            if (gradient < 0.8)
            {
                ColorCode = "#007BFF"; // Blue - Low gradient
                Description = "Low";
            }
            else if (gradient >= 0.8 && gradient < 2.0)
            {
                ColorCode = "#28A745"; // Green - Normal gradient
                Description = "Normal";
            }
            else
            {
                ColorCode = "#DC3545"; // Red - High gradient (> 2.0 per spec)
                Description = "High / Anomaly";
            }
        }
    }
}
