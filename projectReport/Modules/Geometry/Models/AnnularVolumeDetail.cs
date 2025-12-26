using ProjectReport.Models;

namespace ProjectReport.Models.Geometry
{
    public class AnnularVolumeDetail : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public double Volume { get; set; }
        
        // SRS Required Fields for Annular Volume Details Table
        public double WellboreID { get; set; }  // Inner diameter of wellbore section
        public double DrillStringOD { get; set; }  // Outer diameter of drill string component
        public double TopMD { get; set; }  // Top measured depth
        public double BottomMD { get; set; }  // Bottom measured depth
        
        // Calculated property for depth range display
        public string DepthRange => $"{TopMD:F2} - {BottomMD:F2} ft";
        
        // Annular volume calculation: Volume between wellbore ID and drill string OD
        // Formula: ((Wellbore ID² - Drill String OD²) / 1029.4) × Length
        public double AnnularVolume
        {
            get
            {
                if (WellboreID <= 0 || DrillStringOD <= 0 || TopMD >= BottomMD)
                    return 0;
                
                double length = BottomMD - TopMD;
                return ((WellboreID * WellboreID) - (DrillStringOD * DrillStringOD)) / 1029.4 * length;
            }
        }
    }
}

