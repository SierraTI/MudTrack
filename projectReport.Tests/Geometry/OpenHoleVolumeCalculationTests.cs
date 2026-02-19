using System;
using Xunit;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Tests.Geometry
{
    /// <summary>
    /// Unit tests to verify the OpenHole volume calculation matches the technical specification
    /// </summary>
    public class OpenHoleVolumeCalculationTests
    {
        private const double TOLERANCE = 0.01; // 0.01 bbl tolerance for floating point comparison

        [Fact]
        public void OpenHole_VolumeCalculation_MatchesSpecExample()
        {
            // Arrange - Example from specification
            // Section: 8.500" hole, 12.50% washout, 100 ft length
            // Formula: V = [OD × √(1 + W/100)]² × L / 1029.4
            // Expected Volume: ~7.89 bbl
            var component = new WellboreComponent
            {
                SectionType = ComponentType.OpenHole,
                OD = 8.500,  // Hole diameter in inches
                ID = 0.000,  // OpenHole has no inner pipe
                TopMD = 200,
                BottomMD = 300,  // Length = 100 ft
                Washout = 12.50  // 12.50% washout
            };

            // Act
            double volume = component.Volume;

            // Assert
            // Formula: V = [OD × √(1 + W/100)]² × L / 1029.4
            // OD_eff = 8.500 × √(1.125) = 8.500 × 1.0606 = 9.0148"
            // Volume = (9.0148² × 100) / 1029.4 ≈ 7.89 bbl
            
            double expectedVolume = 7.89;
            Assert.InRange(volume, expectedVolume - TOLERANCE, expectedVolume + TOLERANCE);
        }

        [Theory]
        [InlineData(8.500, 0.00, 100, 5.42)]   // No washout: (8.5² × 100)/1029.4 = 5.42
        [InlineData(8.500, 5.00, 100, 5.69)]   // 5% washout: [8.5 × √1.05]² × 100 / 1029.4 ≈ 5.69
        [InlineData(8.500, 10.00, 100, 5.98)]  // 10% washout: [8.5 × √1.10]² × 100 / 1029.4 ≈ 5.98
        [InlineData(8.500, 12.50, 100, 7.89)]  // 12.5% washout (spec example): [8.5 × √1.125]² × 100 / 1029.4 ≈ 7.89
        [InlineData(8.500, 25.00, 100, 8.17)]  // 25% washout: [8.5 × √1.25]² × 100 / 1029.4 ≈ 8.17
        [InlineData(12.250, 10.00, 500, 50.58)] // Larger hole, longer section
        public void OpenHole_VolumeCalculation_WithVariousWashoutPercentages(
            double holeDiameter, 
            double washoutPercent, 
            double length, 
            double expectedVolume)
        {
            // Arrange
            var component = new WellboreComponent
            {
                SectionType = ComponentType.OpenHole,
                OD = holeDiameter,
                ID = 0.000,
                TopMD = 0,
                BottomMD = length,
                Washout = washoutPercent
            };

            // Act
            double volume = component.Volume;

            // Assert
            Assert.InRange(volume, expectedVolume - TOLERANCE, expectedVolume + TOLERANCE);
        }

        [Fact]
        public void CasingLiner_VolumeCalculation_UsesID()
        {
            // Arrange - Casing section (annular volume based on ID)
            var component = new WellboreComponent
            {
                SectionType = ComponentType.Casing,
                OD = 13.375,  // Outer diameter
                ID = 12.615,  // Inner diameter
                TopMD = 0,
                BottomMD = 1000,  // 1000 ft length
                Washout = 0  // Washout not applicable for casing
            };

            // Act
            double volume = component.Volume;

            // Assert
            // Formula: Volume = ID² × Length / 1029.4
            // Volume = 12.615² × 1000 / 1029.4
            // Volume = 159.138225 × 1000 / 1029.4
            // Volume ≈ 154.57 bbl
            
            double expectedVolume = 154.57;
            Assert.InRange(volume, expectedVolume - TOLERANCE, expectedVolume + TOLERANCE);
        }

        [Fact]
        public void OpenHole_WashoutValidation_MinimumThreshold()
        {
            // Arrange
            var component = new WellboreComponent
            {
                SectionType = ComponentType.OpenHole,
                OD = 8.500,
                ID = 0.000,
                TopMD = 0,
                BottomMD = 100,
                Washout = 0.005  // Below minimum threshold of 0.01%
            };

            // Act - Trigger validation
            // The component should have validation errors

            // Assert
            Assert.True(component.HasErrors, "Component should have validation errors for washout < 0.01%");
        }

        [Fact]
        public void OpenHole_IDMustBeZero()
        {
            // Arrange
            var component = new WellboreComponent
            {
                SectionType = ComponentType.OpenHole,
                OD = 8.500,
                TopMD = 0,
                BottomMD = 100,
                Washout = 10.0
            };

            // Act
            double id = component.ID.GetValueOrDefault();

            // Assert
            Assert.Equal(0.0, id, 3); // ID should be exactly 0.000
        }

        [Fact]
        public void ManualVolumeCalculation_VerifyFormula()
        {
            // This test manually calculates the volume to verify the formula
            // Spec example: 8.500" hole, 12.50% washout, 100 ft
            
            double holeDiameter = 8.500;
            double washoutPercent = 12.50;
            double length = 100;
            double factor = 1029.4;

            // Step 1: Calculate effective diameter using CORRECT formula with square root
            double effectiveOD = holeDiameter * Math.Sqrt(1 + washoutPercent / 100.0);
            Assert.Equal(9.0148, effectiveOD, 3);

            // Step 2: Calculate volume
            double volume = (effectiveOD * effectiveOD * length) / factor;
            
            // Expected: (9.0148² × 100) / 1029.4 = 81.27 × 100 / 1029.4 ≈ 7.89 bbl
            double expectedVolume = 7.89;
            Assert.InRange(volume, expectedVolume - 0.05, expectedVolume + 0.05);
        }
    }
}
