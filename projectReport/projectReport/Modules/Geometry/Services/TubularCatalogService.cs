using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Modules.Geometry.Models;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Modules.Geometry.Services
{
    public class TubularCatalogService
    {
        private static TubularCatalogService? _instance;
        public static TubularCatalogService Instance => _instance ??= new TubularCatalogService();

        public ObservableCollection<CasingCatalogItem> CasingCatalog { get; } = new();
        public ObservableCollection<DrillPipeCatalogItem> DrillPipeCatalog { get; } = new();
        public ObservableCollection<HwdpDcCatalogItem> HwdpDcCatalog { get; } = new();
        public ObservableCollection<BitCatalogItem> BitCatalog { get; } = new();

        private TubularCatalogService()
        {
            SeedCatalog();
        }

        private void SeedCatalog()
        {
            // --- CASING/LINER ---
            CasingCatalog.Add(new CasingCatalogItem { Name = "13 3/8\" 54.50 lb/ft", OD = 13.375, Weight = 54.50, ID = 12.615, Drift = 12.459, Capacity = 0.1546 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "13 3/8\" 61.00 lb/ft", OD = 13.375, Weight = 61.00, ID = 12.515, Drift = 12.359, Capacity = 0.1521 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "13 3/8\" 68.00 lb/ft", OD = 13.375, Weight = 68.00, ID = 12.415, Drift = 12.259, Capacity = 0.1497 });
            
            CasingCatalog.Add(new CasingCatalogItem { Name = "9 5/8\" 36.00 lb/ft", OD = 9.625, Weight = 36.00, ID = 8.921, Drift = 8.765, Capacity = 0.0773 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "9 5/8\" 40.00 lb/ft", OD = 9.625, Weight = 40.00, ID = 8.835, Drift = 8.679, Capacity = 0.0758 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "9 5/8\" 43.50 lb/ft", OD = 9.625, Weight = 43.50, ID = 8.755, Drift = 8.599, Capacity = 0.0745 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "9 5/8\" 47.00 lb/ft", OD = 9.625, Weight = 47.00, ID = 8.681, Drift = 8.525, Capacity = 0.0732 });
            
            CasingCatalog.Add(new CasingCatalogItem { Name = "7\" 23.00 lb/ft", OD = 7.000, Weight = 23.00, ID = 6.366, Drift = 6.241, Capacity = 0.0394 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "7\" 26.00 lb/ft", OD = 7.000, Weight = 26.00, ID = 6.276, Drift = 6.151, Capacity = 0.0383 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "7\" 29.00 lb/ft", OD = 7.000, Weight = 29.00, ID = 6.184, Drift = 6.059, Capacity = 0.0371 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "7\" 32.00 lb/ft", OD = 7.000, Weight = 32.00, ID = 6.094, Drift = 5.969, Capacity = 0.0361 });
            
            CasingCatalog.Add(new CasingCatalogItem { Name = "4 1/2\" 11.60 lb/ft", OD = 4.500, Weight = 11.60, ID = 4.000, Drift = 3.875, Capacity = 0.0155 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "4 1/2\" 13.50 lb/ft", OD = 4.500, Weight = 13.50, ID = 3.920, Drift = 3.795, Capacity = 0.0149 });
            CasingCatalog.Add(new CasingCatalogItem { Name = "4 1/2\" 15.10 lb/ft", OD = 4.500, Weight = 15.10, ID = 3.826, Drift = 3.701, Capacity = 0.0142 });

            // --- DRILL PIPE ---
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "3 1/2\" 13.30 lb/ft", OD = 3.500, Weight = 13.30, ID = 2.764, Capacity = 0.0074, Displacement = 0.0051 });
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "3 1/2\" 15.50 lb/ft", OD = 3.500, Weight = 15.50, ID = 2.602, Capacity = 0.0066, Displacement = 0.0059 });
            
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "4\" 14.00 lb/ft", OD = 4.000, Weight = 14.00, ID = 3.340, Capacity = 0.0108, Displacement = 0.0054 });
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "4\" 15.70 lb/ft", OD = 4.000, Weight = 15.70, ID = 3.240, Capacity = 0.0102, Displacement = 0.0060 });
            
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "4 1/2\" 16.60 lb/ft", OD = 4.500, Weight = 16.60, ID = 3.826, Capacity = 0.0142, Displacement = 0.0064 });
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "4 1/2\" 20.00 lb/ft", OD = 4.500, Weight = 20.00, ID = 3.640, Capacity = 0.0129, Displacement = 0.0077 });

            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "5\" 19.50 lb/ft", OD = 5.000, Weight = 19.50, ID = 4.276, Capacity = 0.0178, Displacement = 0.0075 });
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "5\" 25.60 lb/ft", OD = 5.000, Weight = 25.60, ID = 4.000, Capacity = 0.0155, Displacement = 0.0098 });

            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "5 1/2\" 21.90 lb/ft", OD = 5.500, Weight = 21.90, ID = 4.778, Capacity = 0.0222, Displacement = 0.0084 });
            DrillPipeCatalog.Add(new DrillPipeCatalogItem { Name = "5 1/2\" 24.70 lb/ft", OD = 5.500, Weight = 24.70, ID = 4.670, Capacity = 0.0212, Displacement = 0.0094 });

            // --- HWDP / Drill Collars ---
            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "5\" HWDP", ComponentType = "HWDP", OD = 5.000, ID = 3.000, Capacity = 0.0087, Displacement = 0.0155 });
            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "4 1/2\" HWDP", ComponentType = "HWDP", OD = 4.500, ID = 2.750, Capacity = 0.0073, Displacement = 0.0123 });
            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "4\" HWDP", ComponentType = "HWDP", OD = 4.000, ID = 2.563, Capacity = 0.0064, Displacement = 0.0092 });
            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "3 1/2\" HWDP", ComponentType = "HWDP", OD = 3.500, ID = 2.063, Capacity = 0.0041, Displacement = 0.0078 });

            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "8\" DC", ComponentType = "Drill Collar", OD = 8.000, ID = 2.813, Capacity = 0.0077, Displacement = 0.0545 });
            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "6 1/4\" DC", ComponentType = "Drill Collar", OD = 6.250, ID = 2.813, Capacity = 0.0077, Displacement = 0.0303 });
            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "4 3/4\" DC", ComponentType = "Drill Collar", OD = 4.750, ID = 2.250, Capacity = 0.0049, Displacement = 0.0170 });
            HwdpDcCatalog.Add(new HwdpDcCatalogItem { Name = "3 1/8\" DC", ComponentType = "Drill Collar", OD = 3.125, ID = 1.250, Capacity = 0.0015, Displacement = 0.0080 });

            // --- BITS & OPEN HOLE SIZES ---
            BitCatalog.Add(new BitCatalogItem { Size = "17 1/2\"", Diameter = 17.500 });
            BitCatalog.Add(new BitCatalogItem { Size = "16\"", Diameter = 16.000 });
            BitCatalog.Add(new BitCatalogItem { Size = "14 3/4\"", Diameter = 14.750 });
            BitCatalog.Add(new BitCatalogItem { Size = "12 1/4\"", Diameter = 12.250 });
            BitCatalog.Add(new BitCatalogItem { Size = "10 5/8\"", Diameter = 10.625 });
            BitCatalog.Add(new BitCatalogItem { Size = "9 7/8\"", Diameter = 9.875 });
            BitCatalog.Add(new BitCatalogItem { Size = "8 1/2\"", Diameter = 8.500 });
            BitCatalog.Add(new BitCatalogItem { Size = "7 7/8\"", Diameter = 7.875 });
            BitCatalog.Add(new BitCatalogItem { Size = "6 1/8\"", Diameter = 6.125 });
            BitCatalog.Add(new BitCatalogItem { Size = "6\"", Diameter = 6.000 });
            BitCatalog.Add(new BitCatalogItem { Size = "4 3/4\"", Diameter = 4.750 });
            BitCatalog.Add(new BitCatalogItem { Size = "4 5/8\"", Diameter = 4.625 });
            BitCatalog.Add(new BitCatalogItem { Size = "3 7/8\"", Diameter = 3.875 });
        }

        public IEnumerable<TubularCatalogItem> GetOptionsForWellbore(ProjectReport.Models.Geometry.DrillString.ComponentType type)
        {
            if (type == ProjectReport.Models.Geometry.DrillString.ComponentType.OpenHole)
            {
                return BitCatalog.Select(b => new TubularCatalogItemWrapper(b));
            }
            // For Casing and Liner
            return CasingCatalog;
        }

        public IEnumerable<TubularCatalogItem> GetOptionsForDrillString(ProjectReport.Models.Geometry.DrillString.ComponentType type)
        {
            if (type == ProjectReport.Models.Geometry.DrillString.ComponentType.DrillPipe)
            {
                return DrillPipeCatalog;
            }
            if (type == ProjectReport.Models.Geometry.DrillString.ComponentType.HWDP)
            {
                return HwdpDcCatalog.Where(x => x.ComponentType == "HWDP");
            }
            if (type == ProjectReport.Models.Geometry.DrillString.ComponentType.DC)
            {
                return HwdpDcCatalog.Where(x => x.ComponentType == "Drill Collar");
            }
            if (type == ProjectReport.Models.Geometry.DrillString.ComponentType.Bit)
            {
                return BitCatalog.Select(b => new TubularCatalogItemWrapper(b));
            }

            return Enumerable.Empty<TubularCatalogItem>();
        }
        
        // Internal wrapper for BitCatalogItem since it doesn't have ID/Capacity/etc., just an OD
        private class TubularCatalogItemWrapper : TubularCatalogItem
        {
            public TubularCatalogItemWrapper(BitCatalogItem bit)
            {
                Name = bit.Size;
                OD = bit.Diameter;
                ID = 0; // Open Hole doesn't have an ID
                Capacity = Math.Pow(bit.Diameter, 2) / 1029.4;
            }
        }
    }
}
