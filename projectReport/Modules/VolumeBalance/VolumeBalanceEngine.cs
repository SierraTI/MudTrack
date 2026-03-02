using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectReport.Modules.VolumeBalance
{
    public class VolumeBalanceEngine
    {
        public class PipeSection
        {
            public double ID { get; set; } // Internal Diameter (in)
            public double OD { get; set; } // Outer Diameter (in)
            public double Length { get; set; } // Length (ft)
        }

        public class AnnularSection
        {
            public double HoleID { get; set; } // Casing/Hole ID (in)
            public double PipeOD { get; set; } // Pipe OD (in)
            public double Length { get; set; } // Length (ft)
        }

        public static double CalculateStringVolume(IEnumerable<PipeSection> sections)
        {
            // Capacity (bbl/ft) = ID^2 / 1029.4
            return sections.Sum(s => (Math.Pow(s.ID, 2) / 1029.4) * s.Length);
        }

        public static double CalculateAnnularVolume(IEnumerable<AnnularSection> sections)
        {
            // Annular Capacity (bbl/ft) = (ID_hole^2 - OD_pipe^2) / 1029.4
            return sections.Sum(s => ((Math.Pow(s.HoleID, 2) - Math.Pow(s.PipeOD, 2)) / 1029.4) * s.Length);
        }

        public static double CalculateTotalWellVolume(double stringVol, double annulusVol)
        {
            return stringVol + annulusVol;
        }

        public static double CalculateSurfaceEquipmentVolume(double internalDiameterInches, double lengthFeet)
        {
            // Capacity (bbl) = (ID^2 / 1029.4) * length
            return (Math.Pow(internalDiameterInches, 2) / 1029.4) * lengthFeet;
        }

        public static double CalculateSeepageRate(double volumeLossBbl, double timeElapsedHours)
        {
            if (timeElapsedHours <= 0) return 0;
            return volumeLossBbl / timeElapsedHours;
        }

        public static double CalculateTheoreticalDensity(
            double startVolume, double startSg,
            double addedChemicalVolume, double addedChemicalSg,
            double addedWaterVolume, double addedWaterSg = 1.0)
        {
            double totalVolume = startVolume + addedChemicalVolume + addedWaterVolume;
            if (totalVolume <= 0) return startSg; // Avoid division by zero

            double startMass = startVolume * startSg;
            double addedChemicalMass = addedChemicalVolume * addedChemicalSg;
            double addedWaterMass = addedWaterVolume * addedWaterSg;

            return (startMass + addedChemicalMass + addedWaterMass) / totalVolume;
        }
    }
}
