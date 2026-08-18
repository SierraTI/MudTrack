using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class TransfersVol : INotifyPropertyChanged
    {
        // =========================
        // OPCIÓN DE TRANSFERENCIA
        // =========================

        public class TransferOption
        {
            public string PitSystem { get; set; } = string.Empty;

            public string FluidType { get; set; } = string.Empty;

            public string FluidSubtype { get; set; } = string.Empty;

            // Indica si es un placeholder ("Seleccione FROM..." o "Seleccione TO...")
            public bool IsPlaceholder { get; set; }

            public string DisplayName
            {
                get
                {
                    if (IsPlaceholder)
                        return PitSystem;

                    return $"{PitSystem} | {FluidType} | {FluidSubtype}";
                }
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        // =========================
        // CAMPOS
        // =========================

        private TransferOption? _from;
        private TransferOption? _to;
        private double _vol;

        // =========================
        // FROM
        // =========================

        public TransferOption? From
        {
            get => _from;
            set
            {
                if (SetProperty(ref _from, value))
                {
                    OnPropertyChanged(nameof(FromDisplay));
                }
            }
        }

        // =========================
        // TO
        // =========================

        public TransferOption? To
        {
            get => _to;
            set
            {
                if (SetProperty(ref _to, value))
                {
                    OnPropertyChanged(nameof(ToDisplay));
                }
            }
        }

        // =========================
        // DISPLAY
        // =========================

        public string FromDisplay
        {
            get
            {
                return From == null
                    ? string.Empty
                    : From.DisplayName;
            }
        }

        public string ToDisplay
        {
            get
            {
                return To == null
                    ? string.Empty
                    : To.DisplayName;
            }
        }

        // =========================
        // VOLUMEN
        // =========================

        public double Vol
        {
            get => _vol;
            set => SetProperty(ref _vol, value);
        }

        // =========================
        // INotifyPropertyChanged
        // =========================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;

            OnPropertyChanged(propertyName);

            return true;
        }

        protected void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}