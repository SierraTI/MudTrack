using Xunit;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Tests.Geometry
{
    public class PdcBitTests
    {
        [Fact]
        public void ValidatePdc_ReturnsInvalid_ForBadValues()
        {
            var bit = new PdcBit();
            var (isValid, message) = bit.ValidatePdc();
            Assert.False(isValid);
            Assert.NotNull(message);
        }

        [Fact]
        public void ValidatePdc_ReturnsValid_ForGoodValues()
        {
            var bit = new PdcBit
            {
                BitType = "PDC",
                GaugeIn = 8.75,
                NozzleCount = 3,
                AggressivenessRating = 4
            };

            var (isValid, message) = bit.ValidatePdc();
            Assert.True(isValid);
            Assert.Null(message);
        }

        [Fact]
        public void ValidateTfaConsistency_Warns_WhenNoJetsButNozzlesSpecified()
        {
            var bit = new PdcBit { NozzleCount = 2 };
            var (isConsistent, message) = bit.ValidateTfaConsistency();
            Assert.False(isConsistent);
            Assert.NotNull(message);
        }
    }
}
