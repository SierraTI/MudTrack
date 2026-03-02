using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Inventory
{
    public class TicketLine : INotifyPropertyChanged
    {
        private string _productCode = "";
        public string ProductCode
        {
            get => _productCode;
            set { if (_productCode != value) { _productCode = value; OnPropertyChanged(); } }
        }

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); } }
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

        private string _observations = "";
        public string Observations
        {
            get => _observations;
            set { if (_observations != value) { _observations = value; OnPropertyChanged(); } }
        }

        private string _requisition = "";
        public string Requisition
        {
            get => _requisition;
            set { if (_requisition != value) { _requisition = value; OnPropertyChanged(); } }
        }

        private string _movementType = "Incoming";
        public string MovementType
        {
            get => _movementType;
            set { if (_movementType != value) { _movementType = value; OnPropertyChanged(); } }
        }

        private DateTime _date = DateTime.Now;
        public DateTime Date
        {
            get => _date;
            set { if (_date != value) { _date = value; OnPropertyChanged(); } }
        }

        // NEW: Origin/Supplier information
        private string _origin = "";
        public string Origin
        {
            get => _origin;
            set { if (_origin != value) { _origin = value; OnPropertyChanged(); } }
        }

        // NEW: Supplier name (for incoming shipments from vendors)
        private string _supplierName = "";
        public string SupplierName
        {
            get => _supplierName;
            set { if (_supplierName != value) { _supplierName = value; OnPropertyChanged(); } }
        }

        // NEW: Destination (for returns/transfers)
        private string _destination = "";
        public string Destination
        {
            get => _destination;
            set { if (_destination != value) { _destination = value; OnPropertyChanged(); } }
        }

        // NEW: Quantity that was originally received (for returns validation)
        private double _quantityReceived;
        public double QuantityReceived
        {
            get => _quantityReceived;
            set { if (_quantityReceived != value) { _quantityReceived = value; OnPropertyChanged(); } }
        }

        // NEW: Condition of product (Sealed, Open, Damaged)
        private string _condition = "Sealed";
        public string Condition
        {
            get => _condition;
            set { if (_condition != value) { _condition = value; OnPropertyChanged(); } }
        }

        // NEW: Current stock for validation (read-only, populated from inventory)
        private double _currentStock = 0;
        public double CurrentStock
        {
            get => _currentStock;
            set { if (Math.Abs(_currentStock - value) > 0.0001) { _currentStock = value; OnPropertyChanged(); } }
        }

        // NEW: Validation error message
        private string _validationError = "";
        public string ValidationError
        {
            get => _validationError;
            set { if (_validationError != value) { _validationError = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Validates that quantity being returned doesn't exceed current stock
        /// </summary>
        public bool ValidateReturnQuantity()
        {
            if (Quantity > CurrentStock)
            {
                ValidationError = $"Cannot return {Quantity}. Only {CurrentStock} {Unit} available in stock.";
                return false;
            }
            ValidationError = "";
            return true;
        }

        private bool _isAddedToFluid = true;
        public bool IsAddedToFluid
        {
            get => _isAddedToFluid;
            set { if (_isAddedToFluid != value) { _isAddedToFluid = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}

