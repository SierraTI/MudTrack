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
        public double BottomMD { get; set; }  // Bottom depth
        public string SectionType { get; set; } = string.Empty; // Casing, Liner, OpenHole
        public string Stage { get; set; } = string.Empty; // Surface, Intermediate, etc.
        
        // Element description (e.g., "Drill Pipe / Surface Casing")
        public string ElementDescription { get; set; } = string.Empty;
        
        // Calculated property for depth range display
        public string DepthRange => $"{TopMD:F0} - {BottomMD:F0} ft";
        
        // Annular volume calculation: Volume between wellbore ID and drill string OD
        // Formula: ((Wellbore ID² - Drill String OD²) / 1029.4) × Length
        public double AnnularVolume
        {
            get
            {
                // Use stored Volume if available, otherwise calculate
                if (Volume > 0)
                    return Volume;
                
                if (WellboreID <= 0 || TopMD >= BottomMD)
                    return 0;
                
                double length = BottomMD - TopMD;
                
                // If no drill string, return wellbore capacity
                if (DrillStringOD <= 0)
                {
                    return (WellboreID * WellboreID / 1029.4) * length;
                }
                
                // Annular volume
                double idSquared = WellboreID * WellboreID;
                double odSquared = DrillStringOD * DrillStringOD;
                
                if (idSquared <= odSquared)
                    return 0;
                
                return ((idSquared - odSquared) / 1029.4) * length;
            }
        }
    }
}

