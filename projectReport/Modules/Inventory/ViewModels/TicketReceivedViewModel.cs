using System;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class TicketReceivedViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<Product> Products { get; }

        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        private string _origin = "";
        public string Origin
        {
            get => _origin;
            set => SetProperty(ref _origin, value);
        }

        private double _unitPrice;
        public double UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        private string _observations = "";
        public string Observations
        {
            get => _observations;
            set => SetProperty(ref _observations, value);
        }

        private string _user = Environment.UserName;
        public string User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        private string _error = "";
        public string Error
        {
            get => _error;
            set => SetProperty(ref _error, value);
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public event Action? RequestClose;

        public TicketReceivedViewModel(InventoryService service)
        {
            _service = service;
            Products = new ObservableCollection<Product>(_service.GetProducts().Where(p => p.Status == ProductStatus.Active));

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
        }

        private void Save()
        {
            Error = "";

            if (SelectedProduct == null) { Error = "Select a product."; return; }
            if (Quantity <= 0) { Error = "Quantity must be > 0."; return; }

            try
            {
                var ticket = new Ticket
                {
                    Type = TicketType.Received,
                    Date = DateTime.Now,
                    User = User,
                    Observations = Observations,
                    Line = new TicketLine
                    {
                        ProductCode = SelectedProduct.Code,
                        Quantity = Quantity,
                        UnitPrice = UnitPrice,
                        Context = Origin
                    }
                };

                _service.CreateTicketReceived(ticket);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
