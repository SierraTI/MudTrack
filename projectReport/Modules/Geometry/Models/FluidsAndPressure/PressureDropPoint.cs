using ProjectReport.Models;

namespace ProjectReport.Models.Geometry.FluidsAndPressure
{
    public class PressureDropPoint : BaseModel
    {
        private double _flowRate;
        private double _pressureDrop;
        private string _notes = string.Empty;

        public double FlowRate
        {
            get => _flowRate;
            set => SetProperty(ref _flowRate, value);
        }

        public double PressureDrop
        {
            get => _pressureDrop;
            set => SetProperty(ref _pressureDrop, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
    }
}
