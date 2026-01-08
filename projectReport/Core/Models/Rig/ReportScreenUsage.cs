using System;

namespace ProjectReport.Models.Rig
{
    public class ReportScreenUsage : BaseModel
    {
        private string _shakerName = string.Empty;
        private string _screenType = string.Empty;
        private int _quantity;
        private bool _isDeducted;

        public string ShakerName
        {
            get => _shakerName;
            set => SetProperty(ref _shakerName, value);
        }

        public string ScreenType
        {
            get => _screenType;
            set => SetProperty(ref _screenType, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public bool IsDeducted
        {
            get => _isDeducted;
            set => SetProperty(ref _isDeducted, value);
        }
    }
}
