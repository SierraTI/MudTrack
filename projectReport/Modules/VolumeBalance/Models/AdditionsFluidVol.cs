using ProjectReport.ViewModels;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class AdditionsFluidVol : BaseViewModel
    {
        private string _fluidName = string.Empty;
        public string FluidName
        {
            get => _fluidName;
            set
            {
                _fluidName = value;
                OnPropertyChanged();
            }
        }

        private double? _volume;
        public double? Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged();
            }
        }

        private string _fluidType = string.Empty;
        public string FluidType
        {
            get => _fluidType;
            set
            {
                _fluidType = value;
                OnPropertyChanged();
            }
        }

        private double? _concen;
        public double? Concen
        {
            get => _concen;
            set
            {
                _concen = value;
                OnPropertyChanged();
            }
        }
    }
}