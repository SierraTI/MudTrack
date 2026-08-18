using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class AdditionsLiquidVol : INotifyPropertyChanged
    {
        private string pitSystem;
        public string PitSystem
        {
            get => pitSystem;
            set
            {
                if (pitSystem != value)
                {
                    pitSystem = value;
                    OnPropertyChanged();
                }
            }
        }

        private string fluidSubtype;
        public string FluidSubtype
        {
            get => fluidSubtype;
            set
            {
                if (fluidSubtype != value)
                {
                    fluidSubtype = value;
                    OnPropertyChanged();
                }
            }
        }

        private double water;
        public double Water
        {
            get => water;
            set
            {
                if (water != value)
                {
                    water = value;
                    OnPropertyChanged();
                }
            }
        }

        private double dewateringWater;
        public double DewateringWater
        {
            get => dewateringWater;
            set
            {
                if (dewateringWater != value)
                {
                    dewateringWater = value;
                    OnPropertyChanged();
                }
            }
        }

        private double osmosisWater;
        public double OsmosisWater
        {
            get => osmosisWater;
            set
            {
                if (osmosisWater != value)
                {
                    osmosisWater = value;
                    OnPropertyChanged();
                }
            }
        }

        private double oilBased;
        public double OilBased
        {
            get => oilBased;
            set
            {
                if (oilBased != value)
                {
                    oilBased = value;
                    OnPropertyChanged();
                }
            }
        }

        private double iflux;
        public double Iflux
        {
            get => iflux;
            set
            {
                if (iflux != value)
                {
                    iflux = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}