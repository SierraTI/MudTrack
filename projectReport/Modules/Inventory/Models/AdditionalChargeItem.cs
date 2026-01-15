using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Inventory
{
    public class AdditionalChargeItem : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); } }
        }

        private string _unit = "Each";
        public string Unit
        {
            get => _unit;
            set { if (_unit != value) { _unit = value; OnPropertyChanged(); } }
        }

        private double _unitPrice;
        public double UnitPrice
        {
            get => _unitPrice;
            set { if (Math.Abs(_unitPrice - value) > 0.0001) { _unitPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); } }
        }

        private double _quantity = 1;
        public double Quantity
        {
            get => _quantity;
            set { if (Math.Abs(_quantity - value) > 0.0001) { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); } }
        }

        public double Total => Math.Round(UnitPrice * Quantity, 2);

        private string _observations = "";
        public string Observations
        {
            get => _observations;
            set { if (_observations != value) { _observations = value; OnPropertyChanged(); } }
        }

        private string _currency = "USD";
        public string Currency
        {
            get => _currency;
            set { if (_currency != value) { _currency = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}