using System.Collections.ObjectModel;
using ProjectReport.Models.Rig;
using ProjectReport.Services;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolSystemViewModel
    {
        private readonly WellContextService _context;

        public VolSystemViewModel()
        {
            _context = WellContextService.Instance;
        }

        public ObservableCollection<RigPit> Pits =>
            _context.CurrentWell?.RigProfile?.Pits
            ?? new ObservableCollection<RigPit>();
    }
}