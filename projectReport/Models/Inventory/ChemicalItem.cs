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

        private string _nombre = "";
        public string Nombre
        {
            get => _nombre;
            set { if (_nombre != value) { _nombre = value; OnPropertyChanged(); } }
        }

        private string _descripcion = "";
        public string Descripcion
        {
            get => _descripcion;
            set { if (_descripcion != value) { _descripcion = value; OnPropertyChanged(); } }
        }

        private string _estadoFisico = "";
        public string EstadoFisico
        {
            get => _estadoFisico;
            set { if (_estadoFisico != value) { _estadoFisico = value; OnPropertyChanged(); } }
        }

        private string _presentacion = "";
        public string Presentacion
        {
            get => _presentacion;
            set { if (_presentacion != value) { _presentacion = value; OnPropertyChanged(); } }
        }

        private double _cantidad = 0;
        public double Cantidad
        {
            get => _cantidad;
            set { if (_cantidad != value) { _cantidad = value; OnPropertyChanged(); } }
        }

        private string _unidad = "";
        public string Unidad
        {
            get => _unidad;
            set { if (_unidad != value) { _unidad = value; OnPropertyChanged(); } }
        }

        private double _sg = 0;
        public double SG
        {
            get => _sg;
            set { if (Math.Abs(_sg - value) > 0.0001) { _sg = value; OnPropertyChanged(); } }
        }

        private string _categoria = "";
        public string Categoria
        {
            get => _categoria;
            set { if (_categoria != value) { _categoria = value; OnPropertyChanged(); } }
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
