using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Geometry
{
    public class BitJetsConfig : INotifyPropertyChanged
    {
        private int _numberOfJets = 3;
        private int _diameterIn32nds = 12; // Default to 12/32"
        private double _tfa;

        public int NumberOfJets
        {
            get => _numberOfJets;
            set
            {
                if (_numberOfJets != value)
                {
                    _numberOfJets = value;
                    OnPropertyChanged();
                    RecalculateTFA();
                }
            }
        }

        public int DiameterIn32nds
        {
            get => _diameterIn32nds;
            set
            {
                if (_diameterIn32nds != value)
                {
                    _diameterIn32nds = value;
                    OnPropertyChanged();
                    RecalculateTFA();
                }
            }
        }

        public double TFA
        {
            get => _tfa;
            private set
            {
                if (_tfa != value)
                {
                    _tfa = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RecalculateTFA()
        {
            if (NumberOfJets <= 0 || DiameterIn32nds <= 0)
            {
                TFA = 0;
                return;
            }

            // Convert diameter from 32nds to inches
            double diameterInInches = DiameterIn32nds / 32.0;
            
            // Calculate area of one jet in square inches: π * r²
            double areaPerJet = Math.PI * Math.Pow(diameterInInches / 2, 2);
            
            // Total Flow Area is the sum of all jet areas
            TFA = NumberOfJets * areaPerJet;
        }
    }
}

