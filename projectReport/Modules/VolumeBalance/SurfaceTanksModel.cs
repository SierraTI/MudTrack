using System.Collections.Generic;

namespace ProjectReport.Modules.VolumeBalance
{
    public class SurfaceTank
    {
        public string Name { get; set; }
        public double VolumeBbl { get; set; } // User input in bbl
        public double MaxCapacity { get; set; } // Pulled from RigProfile
        public double PercentFull => MaxCapacity > 0 ? Math.Round((VolumeBbl / MaxCapacity) * 100, 1) : 0;
    }

    public class SurfaceTanksModel
    {
        public List<SurfaceTank> Tanks { get; set; } = new List<SurfaceTank>();

        public double TotalSurfaceVolume => Tanks.Sum(t => t.VolumeBbl);
    }
}
