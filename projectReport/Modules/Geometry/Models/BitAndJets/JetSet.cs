using System;
using ProjectReport.Models;

namespace ProjectReport.Models.Geometry.BitAndJets
{
    public class JetSet : BaseModel
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int? _numberOfJets;
        public int? NumberOfJets
        {
            get => _numberOfJets;
            set
            {
                if (SetProperty(ref _numberOfJets, value))
                {
                    Recalculate();
                    OnPropertyChanged(nameof(TFACalculated));
                }
            }
        }

        private int? _jetDiameter32nds;
        public int? JetDiameter32nds
        {
            get => _jetDiameter32nds;
            set
            {
                if (SetProperty(ref _jetDiameter32nds, value))
                {
                    Recalculate();
                    OnPropertyChanged(nameof(TFACalculated));
                }
            }
        }

        private double? _tfaCalculated;
        public double? TFACalculated
        {
            get => _tfaCalculated;
            private set => SetProperty(ref _tfaCalculated, value);
        }

        public JetSet()
        {
        }

        public JetSet(int id, int? numberOfJets, int? jetDiameter32nds)
        {
            _id = id;
            _numberOfJets = numberOfJets;
            _jetDiameter32nds = jetDiameter32nds;
            Recalculate();
        }

        public void Recalculate()
        {
            TFACalculated = ProjectReport.Services.DrillString.JetCalculationService.CalculateTFA(NumberOfJets, JetDiameter32nds);
        }
    }
}
