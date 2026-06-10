using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProjectReport.Models.Geometry.BitAndJets
{
    public class BitJetSet : ProjectReport.Models.BaseModel
    {
        private ObservableCollection<double> _diameters = new();
        private double _tfa;

        public ObservableCollection<double> Diameters
        {
            get => _diameters;
            set => SetProperty(ref _diameters, value);
        }

        public double TFA
        {
            get => _tfa;
            private set => SetProperty(ref _tfa, value);
        }

        public void RecalculateTFA()
        {
            TFA = Math.Round(
                Diameters.Sum(d => Math.PI * Math.Pow(d / 2, 2)),
                4
            );
        }

        public void AddJet(double diameterInInches)
        {
            if (diameterInInches <= 0)
                return;

            Diameters.Add(diameterInInches);
            RecalculateTFA();
        }

        public void Clear()
        {
            Diameters.Clear();
            TFA = 0;
        }
    }
}
