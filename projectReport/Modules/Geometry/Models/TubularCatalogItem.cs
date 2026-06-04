using System;

namespace ProjectReport.Modules.Geometry.Models
{
    public abstract class TubularCatalogItem
    {
        public string Name { get; set; } = string.Empty;
        public double OD { get; set; }
        public double ID { get; set; }
        public double Capacity { get; set; } // bbl/ft
        
        public override string ToString() => Name;
    }

    public class CasingCatalogItem : TubularCatalogItem
    {
        public double Weight { get; set; } // lb/ft
        public double Drift { get; set; }
    }

    public class DrillPipeCatalogItem : TubularCatalogItem
    {
        public double Weight { get; set; } // nominal lb/ft
        public double Displacement { get; set; } // bbl/ft
    }

    public class HwdpDcCatalogItem : TubularCatalogItem
    {
        public string ComponentType { get; set; } = string.Empty; // "HWDP" or "Drill Collar"
        public double Displacement { get; set; } // bbl/ft
    }

    public class BitCatalogItem
    {
        public string Size { get; set; } = string.Empty;
        public double Diameter { get; set; } // decimal OD
        
        public override string ToString() => Size;
    }
}
