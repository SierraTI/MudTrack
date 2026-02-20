using System;
using System.Collections.Generic;

namespace ProjectReport.Modules.VolumeBalance
{
    public static class ChemicalVolumeConverter
    {
        // Converts chemical usage to bbl based on unit and SG
        public static double ToBarrels(double qty, string unit, double sg)
        {
            switch (unit.ToLower())
            {
                case "gal":
                    return qty / 42.0;
                case "lb":
                    return qty / (sg * 8.34 * 42.0);
                case "ton":
                    return (qty * 2000.0) / (sg * 8.34 * 42.0);
                case "big bag":
                    // Needs bag weight; placeholder: 2204.62 lb (1 metric ton)
                    return (qty * 2204.62) / (sg * 8.34 * 42.0);
                default:
                    return 0.0;
            }
        }
    }
}
