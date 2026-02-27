using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Inventory
{
    public class UsageBalanceItem : INotifyPropertyChanged
    {
        private string _productCode = "";
        public string ProductCode
        {
            get => _productCode;
            set { if (_productCode != value) { _productCode = value; OnPropertyChanged(); } }
        }

        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set { if (_productName != value) { _productName = value; OnPropertyChanged(); } }
        }

        private string _unit = "";
        public string Unit
        {
            get => _unit;
            set { if (_unit != value) { _unit = value; OnPropertyChanged(); } }
        }

        private double _initialQuantity;
        public double InitialQuantity
        {
            get => _initialQuantity;
            set { if (Math.Abs(_initialQuantity - value) > 0.0001) { _initialQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentStock)); } }
        }

        private double _receivedQuantity;
        public double ReceivedQuantity
        {
            get => _receivedQuantity;
            set { if (Math.Abs(_receivedQuantity - value) > 0.0001) { _receivedQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentStock)); } }
        }

        private double _returnQuantity;
        public double ReturnQuantity
        {
            get => _returnQuantity;
            set { if (Math.Abs(_returnQuantity - value) > 0.0001) { _returnQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentStock)); } }
        }

        private double _totalUsedQuantity;
        public double TotalUsedQuantity
        {
            get => _totalUsedQuantity;
            set { if (Math.Abs(_totalUsedQuantity - value) > 0.0001) { _totalUsedQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentStock)); OnPropertyChanged(nameof(DailyCost)); } }
        }

        public double CurrentStock => InitialQuantity + ReceivedQuantity - ReturnQuantity - TotalUsedQuantity;

        private double _unitCost;
        public double UnitCost
        {
            get => _unitCost;
            set { if (Math.Abs(_unitCost - value) > 0.0001) { _unitCost = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyCost)); } }
        }

        public double DailyCost => TotalUsedQuantity * UnitCost;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
