using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;

namespace ProjectReport.Modules.VolumeBalance
{
    public class SurfaceTank : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(); } 
        }

        private double _volumeBbl;
        public double VolumeBbl 
        { 
            get => _volumeBbl; 
            set { _volumeBbl = value; OnPropertyChanged(); OnPropertyChanged(nameof(PercentFull)); } 
        }

        private double _maxCapacity;
        public double MaxCapacity 
        { 
            get => _maxCapacity; 
            set { _maxCapacity = value; OnPropertyChanged(); OnPropertyChanged(nameof(PercentFull)); } 
        }

        private string _classification = "Active";
        public string Classification 
        { 
            get => _classification; 
            set { _classification = value; OnPropertyChanged(); } 
        }

        private string _fluidType = string.Empty;
        public string FluidType 
        { 
            get => _fluidType; 
            set { _fluidType = value; OnPropertyChanged(); } 
        }

        private double _density;
        public double Density 
        { 
            get => _density; 
            set { _density = value; OnPropertyChanged(); } 
        }

        private double _yesterdayVol;
        public double YesterdayVol 
        { 
            get => _yesterdayVol; 
            set { _yesterdayVol = value; OnPropertyChanged(); } 
        }

        public double PercentFull => MaxCapacity > 0 ? Math.Round((VolumeBbl / MaxCapacity) * 100, 1) : 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SurfaceTanksModel
    {
        public List<SurfaceTank> Tanks { get; set; } = new List<SurfaceTank>();

        public double TotalSurfaceVolume => Tanks.Sum(t => t.VolumeBbl);
    }
}
