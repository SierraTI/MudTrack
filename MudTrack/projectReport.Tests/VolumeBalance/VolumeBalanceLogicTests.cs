using Xunit;
using ProjectReport.Modules.VolumeBalance.ViewModels;
using ProjectReport.Modules.VolumeBalance;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Services;

namespace ProjectReport.Tests.VolumeBalance
{
    public class VolumeBalanceLogicTests
    {
        [Fact]
        public void ChemicalConverter_Barrels_ReturnsCorrectValue()
        {
            Assert.Equal(10, ProjectReport.Modules.VolumeBalance.ViewModels.ChemicalVolumeConverter.ToBarrels(10, "barrel", 1.0));
            Assert.Equal(10, ProjectReport.Modules.VolumeBalance.ViewModels.ChemicalVolumeConverter.ToBarrels(10, "bbl", 1.2));
        }

        [Fact]
        public void ChemicalConverter_Gallons_ReturnsCorrectValue()
        {
            Assert.Equal(1.0, ProjectReport.Modules.VolumeBalance.ViewModels.ChemicalVolumeConverter.ToBarrels(42, "gallon", 1.0));
            Assert.Equal(2.0, ProjectReport.Modules.VolumeBalance.ViewModels.ChemicalVolumeConverter.ToBarrels(84, "gal", 1.0));
        }

        [Fact]
        public void ChemicalConverter_Sacks_ReturnsCorrectValue()
        {
            double result = ProjectReport.Modules.VolumeBalance.ViewModels.ChemicalVolumeConverter.ToBarrels(3.5, "sack", 1.0);
            Assert.InRange(result, 0.9, 1.1);
        }

        [Fact]
        public void TheoreticalDensity_UsesWeightedAverageWithAdditions()
        {
            var vm = new VolumeBalanceViewModel();
            vm.SurfaceTanks.Add(new SurfaceTank { Classification = "Active", VolumeBbl = 100, YesterdayVol = 100, Density = 10.0 });
            vm.WaterAdded = 9.0;
            vm.ChemicalUsages.Add(new ChemicalUsage { QtyUsed = 42, Unit = "gal", SG = 2.0 }); // 1 bbl @ SG2

            Assert.Equal(9.19, vm.TheoreticalSystemDensity, 2);
        }

        [Fact]
        public void PhysicalTotal_IncludesOnlyActivePitsAndSurfaceEquipment()
        {
            WellContextService.Instance.CurrentWell = new Well
            {
                RigProfile = new RigProfile
                {
                    SurfaceEquipment =
                    {
                        new RigSurfaceEquipment { InternalDiameter = 3.5, Length = 40.0 }
                    },
                    ServiceLine =
                    {
                        new RigSurfaceEquipment { InternalDiameter = 2.0, Length = 10.0 }
                    }
                }
            };

            var vm = new VolumeBalanceViewModel
            {
                StringActual = 50,
                AnnulusActual = 25
            };

            vm.SurfaceTanks.Add(new SurfaceTank { Classification = "Active", VolumeBbl = 100, YesterdayVol = 100, Density = 10 });
            vm.SurfaceTanks.Add(new SurfaceTank { Classification = "Reserve", VolumeBbl = 30, YesterdayVol = 30, Density = 10 });

            var expectedEquipment = VolumeBalanceEngine.CalculateSurfaceEquipmentVolume(3.5, 40.0)
                                  + VolumeBalanceEngine.CalculateSurfaceEquipmentVolume(2.0, 10.0);

            Assert.Equal(100, vm.TotalActiveSurfaceVolume, 3);
            Assert.Equal(expectedEquipment, vm.TheoreticalSurfaceEquipmentVolume, 3);
            Assert.Equal(75 + 100 + expectedEquipment, vm.PhysicalTotal, 3);
        }

        [Fact]
        public void VarianceStatus_UsesTwoPercentTolerance()
        {
            var vm = new VolumeBalanceViewModel
            {
                YesterdayWellboreVol = 0,
                StringActual = 0,
                AnnulusActual = 0,
                TransfersOut = 0
            };

            vm.SurfaceTanks.Add(new SurfaceTank { Classification = "Active", VolumeBbl = 100, YesterdayVol = 100, Density = 10 });

            Assert.Equal("Balanced", vm.VarianceStatus);

            vm.TransfersOut = 10; // accounting = 90, variance approx +10 -> outside 2%
            Assert.Equal("Possible Gain / Kick", vm.VarianceStatus);
            Assert.Equal("#D32F2F", vm.VarianceColor);
        }
    }
}

