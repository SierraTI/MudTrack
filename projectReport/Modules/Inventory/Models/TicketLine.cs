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
            set
            {
                if (_productCode != value)
                {
                    _productCode = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }

        // Optional product name when creating a product from a ticket
        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set
            {
                if (_productName != value)
                {
                    _productName = value;
                    OnPropertyChanged();
                }
            }
        }

        // Solo aplica fuerte en Received (histórico)
        private double _unitPrice;
        public double UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        // Contexto (origen o uso)
        private string _context = "";
        public string Context
        {
            get => _context;
            set
            {
                if (_context != value)
                {
                    _context = value;
                    OnPropertyChanged();
                }
            }
        }

        // Nuevo: Observaciones por línea
        private string _observations = "";
        public string Observations
        {
            get => _observations;
            set
            {
                if (_observations != value)
                {
                    _observations = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
