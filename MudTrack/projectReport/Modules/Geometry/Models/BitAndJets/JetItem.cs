using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Geometry.BitAndJets
{
    public class JetItem : INotifyPropertyChanged
    {
        private int _number;
        private double _diameterInches; // Almacena el diámetro en pulgadas

        public int Number
        {
            get => _number;
            set
            {
                _number = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Diámetro del jet en pulgadas (ej: 0.375 para 12/32)
        /// </summary>
        public double DiameterInches
        {
            get => _diameterInches;
            set
            {
                _diameterInches = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
