using Xunit;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Tests.Geometry
{
    public class MudMotorTests
    {
        [Fact]
        public void ValidateMotor_ReturnsInvalid_WhenValuesMissing()
        {
            var motor = new MudMotor();
            var (isValid, message) = motor.ValidateMotor();
            Assert.False(isValid);
            Assert.NotNull(message);
        }

        [Fact]
        public void ValidateMotor_ReturnsValid_ForGoodValues()
        {
            var motor = new MudMotor
            {
                BestFlowRateGpm = 120,
                StallPressurePsi = 800,
                MaxTorqueFtLbs = 1500
            };

            var (isValid, message) = motor.ValidateMotor();
            Assert.True(isValid);
            Assert.Null(message);
        }

        [Fact]
        public void WouldStall_ComparesSupplyAndAnnularLoss()
        {
            var motor = new MudMotor { StallPressurePsi = 800 };
            // supply 1000, annular loss 100 -> effective 900 -> not stalled
            Assert.False(motor.WouldStall(1000, 100));
            // supply 800, annular loss 50 -> effective 750 -> stalled
            Assert.True(motor.WouldStall(800, 50));
        }
    }
}
