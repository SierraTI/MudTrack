using System.Collections.ObjectModel;
using ProjectReport.Models.Geometry.FluidsAndPressure;

namespace ProjectReport.Models.Geometry
{
    public class PressureDropConfig
    {
        public ObservableCollection<PressureDropPoint> Data { get; set; } = new ObservableCollection<PressureDropPoint>();
        public double MudDensity { get; set; }
    }
}

