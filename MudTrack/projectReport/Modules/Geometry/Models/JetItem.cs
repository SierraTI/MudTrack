using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectReport.Models.Geometry
{
    public class JetItem : ObservableObject
    {
        private double _diameter32;
        public double Diameter32
        {
            get => _diameter32;
            set
            {
                SetProperty(ref _diameter32, value);
                OnPropertyChanged(nameof(TFA));
            }
        }

        // Área por jet (in²) usando diámetro en 32nds
        public double TFA =>
            Math.PI * Math.Pow(((Diameter32 / 32.0) / 2.0), 2);
    }
}
