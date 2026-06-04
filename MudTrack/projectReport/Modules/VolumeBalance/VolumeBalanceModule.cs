using ProjectReport.Modules.VolumeBalance.ViewModels;
using ProjectReport.Modules.VolumeBalance.Views;

namespace ProjectReport.Modules.VolumeBalance
{
    public class VolumeBalanceModule
    {
        public VolumeBalanceViewModel ViewModel { get; private set; }
        public VolumeBalanceView View { get; private set; }

        public VolumeBalanceModule()
        {
            ViewModel = new VolumeBalanceViewModel();
            View = new VolumeBalanceView { DataContext = ViewModel };
        }
    }
}
