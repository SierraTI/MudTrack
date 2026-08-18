using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolChemicalAdded : INotifyPropertyChanged
    {
        private string _pitSystem = string.Empty;
        private string _fluidType = string.Empty;
        private string _subtype = string.Empty;
        private double _volume;

        // =========================
        // PIT SYSTEM (VIENE DE MEMORIA)
        // =========================
        public string PitSystem
        {
            get => _pitSystem;
            set => SetProperty(ref _pitSystem, value);
        }

        // =========================
        // FLUID TYPE
        // =========================
        public string FluidType
        {
            get => _fluidType;
            set => SetProperty(ref _fluidType, value);
        }

        // =========================
        // SUBTYPE
        // =========================
        public string Subtype
        {
            get => _subtype;
            set => SetProperty(ref _subtype, value);
        }

        // =========================
        // VOLUME (POR AHORA 0, LUEGO CALCULO REAL)
        // =========================
        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        // =========================
        // NOTIFICACIÓN UI
        // =========================
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}