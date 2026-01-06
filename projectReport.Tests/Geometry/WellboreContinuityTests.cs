using System;
using Xunit;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Tests.Geometry
{
    public class WellboreContinuityTests
    {
        [Fact]
        public void SetAsFirstRow_SetsTopMDToZero()
        {
            // Arrange
            var component = new WellboreComponent();
            component.TopMD = 100;

            // Act
            component.SetAsFirstRow(true);

            // Assert
            Assert.True(component.IsFirstRow);
            Assert.Equal(0, component.TopMD);
            Assert.False(component.IsTopMDEditable);
        }

        [Fact]
        public void AutoLinkTopMD_UpdatesTopMD_FromPreviousBottomMD()
        {
            // Arrange
            var prevBottomMD = 1500.0;
            var component = new WellboreComponent();
            component.SetAsFirstRow(false); // Ensure second row

            // Act
            component.AutoLinkTopMD(prevBottomMD);

            // Assert
            Assert.Equal(prevBottomMD, component.TopMD);
            Assert.False(component.IsTopMDEditable); // Should be read-only if linked
        }

        [Fact]
        public void AutoLinkTopMD_DoesNotUpdate_IfFirstRow()
        {
            // Arrange
            var prevBottomMD = 1500.0;
            var component = new WellboreComponent();
            component.SetAsFirstRow(true);

            // Act
            component.AutoLinkTopMD(prevBottomMD);

            // Assert
            Assert.Equal(0, component.TopMD); // Should remain 0
        }
    }
}
