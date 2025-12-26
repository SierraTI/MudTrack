using System;

namespace ProjectReport.Models.Geometry.DrillString
{
    public class PdcBit : DrillStringComponent
    {
        public string BitType { get; set; } = "PDC"; // or RollerCone
        public double GaugeIn { get; set; } // inches
        public int NozzleCount { get; set; }
        public int AggressivenessRating { get; set; } = 3; // 1-5

        public PdcBit()
        {
            ComponentType = ComponentType.Bit;
            Name = "PDC Bit";
        }

        public (bool IsValid, string? Message) ValidatePdc()
        {
            if (string.IsNullOrWhiteSpace(BitType)) return (false, "BitType is required");
            if (GaugeIn <= 0) return (false, "GaugeIn must be > 0");
            if (NozzleCount < 0) return (false, "NozzleCount must be >= 0");
            if (AggressivenessRating < 1 || AggressivenessRating > 5) return (false, "AggressivenessRating must be 1-5");
            return (true, null);
        }

        // Basic check that jets/nozzles map to TFA config
        public (bool IsConsistent, string? Message) ValidateTfaConsistency()
        {
            // If NozzleCount is specified but jets are empty, warn
            var jetCount = Jets?.Diameters?.Count ?? 0;
            if (NozzleCount > 0 && jetCount == 0)
                return (false, "Nozzles specified but jets are not configured on the bit");
            return (true, null);
        }
    }
}
