using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Inventory
{
    public class ChemicalItem : INotifyPropertyChanged
    {
        private string _code = "";
        public string Code
        {
            get => _code;
            set { if (_code != value) { _code = value; OnPropertyChanged(); } }
        }

        private string _Name = "";
        public string Name
        {
            get => _Name;
            set { if (_Name != value) { _Name = value; OnPropertyChanged(); } }
        }

        private string _Description = "";
        public string Description
        {
            get => _Description;
            set { if (_Description != value) { _Description = value; OnPropertyChanged(); } }
        }

        private string _PhysicalState = "";
        public string PhysicalState
        {
            get => _PhysicalState;
            set { if (_PhysicalState != value) { _PhysicalState = value; OnPropertyChanged(); } }
        }

        private string _Presentation = "";
        public string Presentation
        {
            get => _Presentation;
            set { if (_Presentation != value) { _Presentation = value; OnPropertyChanged(); } }
        }

        private double _Quantity = 0;
        public double Quantity
        {
            get => _Quantity;
            set { if (_Quantity != value) { _Quantity = value; OnPropertyChanged(); } }
        }

        private string _Unit = "";
        public string Unit
        {
            get => _Unit;
            set { if (_Unit != value) { _Unit = value; OnPropertyChanged(); } }
        }

        private double _sg = 0;
        public double SG
        {
            get => _sg;
            set { if (Math.Abs(_sg - value) > 0.0001) { _sg = value; OnPropertyChanged(); } }
        }

        private string _Category = "";
        public string Category
        {
            get => _Category;
            set { if (_Category != value) { _Category = value; OnPropertyChanged(); } }
        }

        private double _unitPrice = 0;
        public double UnitPrice
        {
            get => _unitPrice;
            set { if (Math.Abs(_unitPrice - value) > 0.0001) { _unitPrice = value; OnPropertyChanged(); } }
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

