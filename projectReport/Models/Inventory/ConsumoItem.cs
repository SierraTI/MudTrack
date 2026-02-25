using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Inventory
{
    /// <summary>
    /// Industry-standard Usage (Consumos) item with product linking and automated calculations.
    /// Aligns with SLB/Halliburton/Weatherford standards.
    /// </summary>
    public class ConsumoItem : INotifyPropertyChanged
    {
        // Product/Chemical linking
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

        private string _category = "";
        public string Category
        {
            get => _category;
            set { if (_category != value) { _category = value; OnPropertyChanged(); } }
        }

        // Unit of measurement (Sack, Barrel, Liter, etc.)
        private string _unit = "Units";
        public string Unit
        {
            get => _unit;
            set { if (_unit != value) { _unit = value; OnPropertyChanged(); } }
        }

        private double _sg = 1.0;
        /// <summary>Specific Gravity of the product (default: 1.0 for water-based or generic).</summary>
        public double SG
        {
            get => _sg;
            set { if (Math.Abs(_sg - value) > 0.0001) { _sg = value; OnPropertyChanged(); } }
        }

        // Concentration (ppb/SG) - for mud calculations
        private double _concentration = 0;
        public double Concentration
        {
            get => _concentration;
            set { if (Math.Abs(_concentration - value) > 0.0001) { _concentration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyCost)); } }
        }

        // Quantity used
        private double _quantity = 0;
        public double Quantity
        {
            get => _quantity;
            set { if (Math.Abs(_quantity - value) > 0.0001) { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyCost)); } }
        }

        // Current available stock (read-only from Stock table)
        private double _currentStock = 0;
        public double CurrentStock
        {
            get => _currentStock;
            set { if (Math.Abs(_currentStock - value) > 0.0001) { _currentStock = value; OnPropertyChanged(); } }
        }

        // Unit cost (USD) - populated from Stock table or manual entry
        private double _unitCost = 0;
        public double UnitCost
        {
            get => _unitCost;
            set { if (Math.Abs(_unitCost - value) > 0.0001) { _unitCost = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyCost)); } }
        }

        // Calculated daily cost
        public double DailyCost => Quantity * UnitCost;

        // Timestamp
        private DateTime _date = DateTime.Now;
        public DateTime Date
        {
            get => _date;
            set { if (_date != value) { _date = value; OnPropertyChanged(); } }
        }

        // Notes/Observations
        private string _notes = "";
        public string Notes
        {
            get => _notes;
            set { if (_notes != value) { _notes = value; OnPropertyChanged(); } }
        }

        // Validation flag
        private string _validationError = "";
        public string ValidationError
        {
            get => _validationError;
            set { if (_validationError != value) { _validationError = value; OnPropertyChanged(); } }
        }

        // Check if quantity exceeds available stock
        public bool IsInsufficientStock => Quantity > CurrentStock && CurrentStock > 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
