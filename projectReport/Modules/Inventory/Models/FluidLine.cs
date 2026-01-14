using ProjectReport.ViewModels;

namespace ProjectReport.Models.Inventory
{
    public class FluidLine : BaseViewModel
    {
        public string ProductCode { get; set; } = "";
        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        private double _barrels;
        public double Barrels
        {
            get => _barrels;
            set
            {
                if (SetProperty(ref _barrels, value))
                    OnPropertyChanged(nameof(Total));
            }
        }

        private double _price;
        public double Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                    OnPropertyChanged(nameof(Total));
            }
        }

        public double Total => Barrels * Price;

        // Stock disponible (si se conoce desde catálogo)
        private double _availableStock;
        public double AvailableStock
        {
            get => _availableStock;
            set => SetProperty(ref _availableStock, value);
        }

        private string _observations = "";
        public string Observations
        {
            get => _observations;
            set => SetProperty(ref _observations, value);
        }
    }
}