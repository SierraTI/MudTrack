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
        public ObservableCollection<Product> FilteredProducts { get; } = new();

        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    if (_selectedProduct != null)
                    {
                        ProductCode = _selectedProduct.Code;
                        ProductName = _selectedProduct.Name;
                        ProductSearchText = _selectedProduct.Name;
                    }
                }
            }
        }

        private string _productSearchText = "";
        public string ProductSearchText
        {
            get => _productSearchText;
            set
            {
                if (SetProperty(ref _productSearchText, value))
                {
                    UpdateFilter(_productSearchText);
                }
            }
        }

        private string _productCode = "";
        public string ProductCode
        {
            get => _productCode;
            set => SetProperty(ref _productCode, value);
        }

        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
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

            // Initialize filtered list
            foreach (var p in Products)
                FilteredProducts.Add(p);

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
        }

        private void UpdateFilter(string text)
        {
            FilteredProducts.Clear();

            if (string.IsNullOrWhiteSpace(text))
            {
                foreach (var p in Products)
                    FilteredProducts.Add(p);
                return;
            }

            var q = text.Trim();
            var matches = Products.Where(p =>
                (!string.IsNullOrEmpty(p.Code) && p.Code.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(p.Name) && p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var m in matches)
                FilteredProducts.Add(m);
        }

        private void Save()
        {
            Error = "";

            var code = SelectedProduct?.Code ?? ProductCode?.Trim();
            var name = SelectedProduct?.Name ?? ProductName?.Trim();

            if (string.IsNullOrWhiteSpace(code)) { Error = "Product code is required."; return; }
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
                        ProductCode = code,
                        ProductName = name,
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
