using System;
using ProjectReport.Models;

namespace ProjectReport.Models.Geometry.DrillString
{
    public class ToolJointConfig : BaseModel
    {
        private double _tjOD;
        private double _tjID;
        private double _tjLength;
        private double _weight;

        public double TJ_OD
        {
            get => _tjOD;
            set => SetProperty(ref _tjOD, value);
        }

        public double TJ_ID
        {
            get => _tjID;
            set => SetProperty(ref _tjID, value);
        }

        public double TJ_Length
        {
            get => _tjLength;
            set => SetProperty(ref _tjLength, value);
        }

        public double Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        private double _tjIDLength;
        public double TJ_ID_Length
        {
            get => _tjIDLength;
            set => SetProperty(ref _tjIDLength, value);
        }
    }
}
