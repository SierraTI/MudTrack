using System;

namespace ProjectReport.Models.Geometry.WellTest
{
    public class PumpDataPoint : BaseModel
    {
        private double _time;
        private double _pressure;
        private double _volume;

        public double Time { get => _time; set => SetProperty(ref _time, value); }
        public double Pressure { get => _pressure; set => SetProperty(ref _pressure, value); }
        public double Volume { get => _volume; set => SetProperty(ref _volume, value); }
    }
}
