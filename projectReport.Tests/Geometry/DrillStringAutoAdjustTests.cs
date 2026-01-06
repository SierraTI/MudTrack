using System;
using System.Collections.Generic;
using Xunit;
using ProjectReport.Services.DrillString;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry;

namespace ProjectReport.Tests.Geometry
{
    public class DrillStringAutoAdjustTests
    {
        private readonly DrillStringAutoAdjustService _service;

        public DrillStringAutoAdjustTests()
        {
            _service = new DrillStringAutoAdjustService();
        }

        [Fact]
        public void CalculateDrillPipeLength_ValidInput_ReturnsCorrectLength()
        {
            // Arrange
            double bitDepth = 10000;
            var bhaComponents = new List<DrillStringComponent>
            {
                new DrillStringComponent { Length = 30, ComponentType = ComponentType.DC },
                new DrillStringComponent { Length = 470, ComponentType = ComponentType.DC }
            };
            // BHA Total = 500
            // Expected DP = 10000 - 500 = 9500

            // Act
            var result = _service.CalculateDrillPipeLength(bitDepth, bhaComponents);

            // Assert
            Assert.Equal(9500, result);
        }

        [Fact]
        public void CalculateDrillPipeLength_BHAExceedsDepth_ReturnsZero()
        {
            // Arrange
            double bitDepth = 400;
            var bhaComponents = new List<DrillStringComponent>
            {
                new DrillStringComponent { Length = 500, ComponentType = ComponentType.DC }
            };

            // Act
            var result = _service.CalculateDrillPipeLength(bitDepth, bhaComponents);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void ValidateBHADepth_ValidBHA_ReturnsNull()
        {
            // Arrange
            double bitDepth = 10000;
            var bhaComponents = new List<DrillStringComponent>
            {
                new DrillStringComponent { Length = 500, ComponentType = ComponentType.DC }
            };

            // Act
            var error = _service.ValidateBHADepth(bitDepth, bhaComponents);

            // Assert
            Assert.Null(error);
        }

        [Fact]
        public void ValidateBHADepth_BHATooLong_ReturnsErrorMessage()
        {
            // Arrange
            double bitDepth = 400;
            var bhaComponents = new List<DrillStringComponent>
            {
                new DrillStringComponent { Length = 500, ComponentType = ComponentType.DC }
            };

            // Act
            var error = _service.ValidateBHADepth(bitDepth, bhaComponents);

            // Assert
            Assert.NotNull(error);
            Assert.Contains("BHA Collision Alert", error);
        }

        [Fact]
        public void GetDrillPipeComponent_ReturnsFirstDrillPipe()
        {
            // Arrange
            var components = new List<DrillStringComponent>
            {
                new DrillStringComponent { ComponentType = ComponentType.DrillPipe, Name = "DP1" },
                new DrillStringComponent { ComponentType = ComponentType.HWDP },
                new DrillStringComponent { ComponentType = ComponentType.DrillPipe, Name = "DP2" }
            };

            // Act
            var result = _service.GetDrillPipeComponent(components);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DP1", result.Name);
        }
    }
}
