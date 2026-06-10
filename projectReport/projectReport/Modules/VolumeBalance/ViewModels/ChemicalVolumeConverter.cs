using System;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public static class ChemicalVolumeConverter
    {
        /// <summary>
        /// Converts chemical quantity and unit into barrels (bbl).
        /// Standard industry conversions.
        /// </summary>
        public static double ToBarrels(double qty, string unit, double sg)
        {
            if (qty <= 0) return 0;

            string u = unit?.ToLowerInvariant() ?? string.Empty;

            double volumeBbl = u switch
            {
                "barrel" or "bbl" => qty,
                "gallon" or "gal" => qty / 42.0,
                "sack" or "sk" or "sx" => (qty * 100.0) / (350.0 * sg), // Assuming 100lb sack, 350lb/bbl water
                "drum" or "dr" => qty * 1.3095, // 55 gallon drum ~ 1.3 bbl
                "liter" or "l" => qty / 158.987,
                "m3" => qty * 6.2898,
                _ => 0
            };

            return Math.Round(volumeBbl, 2);
        }
    }
}
