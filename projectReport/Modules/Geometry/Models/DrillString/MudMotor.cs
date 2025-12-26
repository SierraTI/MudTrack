using System;

namespace ProjectReport.Models.Geometry.DrillString
{
    public class MudMotor : DrillStringComponent
    {
        // Motor-specific properties
        public string MotorType { get; set; } = "BentHousing"; // or "Straight"
        public double StallPressurePsi { get; set; } // psi
        public double BestFlowRateGpm { get; set; }
        public double MaxTorqueFtLbs { get; set; }

        public MudMotor()
        {
            ComponentType = ComponentType.Motor;
            Name = "Mud Motor";
        }

        // Simple validation returning (isValid, message)
        public (bool IsValid, string? Message) ValidateMotor()
        {
            if (BestFlowRateGpm <= 0)
                return (false, "BestFlowRateGpm must be > 0");
            if (StallPressurePsi <= 0)
                return (false, "StallPressurePsi must be > 0");
            if (MaxTorqueFtLbs < 0)
                return (false, "MaxTorqueFtLbs must be >= 0");
            return (true, null);
        }

        // Crude stall check: effective pressure at motor = supply - annular loss
        public bool WouldStall(double supplyPressurePsi, double annularPressureLossPsi)
        {
            var effective = supplyPressurePsi - annularPressureLossPsi;
            return effective < StallPressurePsi;
        }
    }
}
