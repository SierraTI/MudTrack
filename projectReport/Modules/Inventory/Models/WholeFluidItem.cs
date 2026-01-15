using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Inventory
{
    public class WholeFluidItem : INotifyPropertyChanged
    {
        private string _requisition = "";
        public string Requisition
        {
            get => _requisition;
            set { if (_requisition != value) { _requisition = value; OnPropertyChanged(); } }
        }

        private string _movementType = "Ingreso";
        public string MovementType
        {
            get => _movementType;
            set { if (_movementType != value) { _movementType = value; OnPropertyChanged(); } }
        }

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

        private double _quantity = 1;
        public double Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); } }
        }

        private double _unitPrice;
        public double UnitPrice
        {
            get => _unitPrice;
            set { if (_unitPrice != value) { _unitPrice = value; OnPropertyChanged(); } }
        }

        private string _context = "";
        public string Context
        {
            get => _context;
            set { if (_context != value) { _context = value; OnPropertyChanged(); } }
        }

        private DateTime _date = DateTime.Now;
        public DateTime Date
        {
            get => _date;
            set { if (_date != value) { _date = value; OnPropertyChanged(); } }
        }

        private string _observations = "";
        public string Observations
        {
            get => _observations;
            set { if (_observations != value) { _observations = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}