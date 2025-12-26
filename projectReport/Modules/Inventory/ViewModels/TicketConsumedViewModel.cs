using System;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class TicketConsumedViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<Product> Products { get; }

        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        private string _useContext = "";
        public string UseContext
        {
            get => _useContext;
            set => SetProperty(ref _useContext, value);
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

        public TicketConsumedViewModel(InventoryService service)
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

            // Validación “alerta roja”
            if (SelectedProduct.StockQty < Quantity)
            {
                Error = $"INSUFFICIENT STOCK. Available: {SelectedProduct.StockQty}, required: {Quantity}";
                return;
            }

            try
            {
                var ticket = new Ticket
                {
                    Type = TicketType.Consumed,
                    Date = DateTime.Now,
                    User = User,
                    Observations = Observations,
                    Line = new TicketLine
                    {
                        ProductCode = SelectedProduct.Code,
                        Quantity = Quantity,
                        UnitPrice = 0,
                        Context = UseContext
                    }
                };

                _service.CreateTicketConsumed(ticket);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
