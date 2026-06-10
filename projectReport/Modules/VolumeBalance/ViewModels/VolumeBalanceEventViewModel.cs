using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProjectReport.Modules.VolumeBalance.Views;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolumeBalanceEventViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _selectedTab;

        public string SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab == value) return;

                _selectedTab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        public object CurrentView => SelectedTab switch
        {
            "VolSystem" => new VolSystemView(),
            "Additions" => new AdditionsView(),
            "Losses" => new LossesView(),
            "Concentrations" => new ConcentrationsView(),
            _ => new VolSystemView()
        };

        public VolumeBalanceEventViewModel()
        {
            SelectedTab = "VolSystem";
        }
    }
}