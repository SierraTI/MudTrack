using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProjectReport.Models.Rig;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolSystemPit : INotifyPropertyChanged
    {
        private int _pitId;
        private string _pitName = string.Empty;
        private string _pitSystem = "Activo";
        private string _fluidType = string.Empty;
        private string _fluidSubtype = string.Empty;
        private double _previousVolume;
        private double _currentVolume;
        private double _density;
        private RigPit _sourcePit;

        public int PitId { get => _pitId; set => SetProperty(ref _pitId, value); }

        public string PitName { get => _pitName; set => SetProperty(ref _pitName, value); }

        public string PitSystem { get => _pitSystem; set => SetProperty(ref _pitSystem, value); }

        public string FluidType { get => _fluidType; set => SetProperty(ref _fluidType, value); }

        public string FluidSubtype { get => _fluidSubtype; set => SetProperty(ref _fluidSubtype, value); }

        public double PreviousVolume { get => _previousVolume; set => SetProperty(ref _previousVolume, value); }

        public double CurrentVolume { get => _currentVolume; set => SetProperty(ref _currentVolume, value); }

        public double Density { get => _density; set => SetProperty(ref _density, value); }

        public RigPit SourcePit { get => _sourcePit; set => SetProperty(ref _sourcePit, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
