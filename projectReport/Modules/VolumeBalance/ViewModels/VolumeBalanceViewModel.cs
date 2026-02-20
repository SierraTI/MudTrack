using System.Collections.ObjectModel;
using ProjectReport.Modules.VolumeBalance;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class ChemicalUsage
    {
        public string ProductCode { get; set; }
        public string Description { get; set; }
        public double QtyUsed { get; set; }
        public string Unit { get; set; }
        public double SG { get; set; }
        public double VolumeAdded { get; set; }
    }

    public class VolumeBalanceViewModel : BaseViewModel
    {
        // Wellbore
        public double StringTheoretical { get; set; }
        public double StringActual { get; set; }
        public double AnnulusTheoretical { get; set; }
        public double AnnulusActual { get; set; }
        public double TotalWellTheoretical => StringTheoretical + AnnulusTheoretical;
        public double TotalWellActual => StringActual + AnnulusActual;
        public double WellVariance => TotalWellActual - TotalWellTheoretical;

        // Surface
        public ObservableCollection<SurfaceTank> SurfaceTanks { get; set; } = new();
        public double TotalSurfaceVolume => SurfaceTanks.Sum(t => t.VolumeBbl);
        public double TotalSurfaceMaxCapacity => SurfaceTanks.Sum(t => t.MaxCapacity);

        public ProjectReport.Models.Rig.RigProfile CurrentRigProfile { get; set; }

        public void SyncFromRigProfile(ProjectReport.Models.Rig.RigProfile rigProfile)
        {
            CurrentRigProfile = rigProfile;
            SurfaceTanks.Clear();
            foreach (var pit in rigProfile.Pits.Where(p => p.IsActive))
            {
                SurfaceTanks.Add(new SurfaceTank
                {
                    Name = pit.PitName,
                    VolumeBbl = pit.CurrentVolume,
                    MaxCapacity = pit.MaxCapacity
                });
            }
            // Optionally, add surface lines volume, shaker tank, etc.
        }

        // Chemicals
        public ObservableCollection<ChemicalUsage> ChemicalUsages { get; set; } = new();

        // System
        public double TotalSystemTheoretical => TotalWellTheoretical;
        public double TotalSystemActual => TotalWellActual + TotalSurfaceVolume + ChemicalUsages.Sum(c => c.VolumeAdded);
        public double SystemVariance => TotalSystemActual - TotalSystemTheoretical;

        // Constructor, calculation, and sync logic would go here
    }
}
