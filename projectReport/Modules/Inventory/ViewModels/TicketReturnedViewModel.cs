using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Inventory
{
    public class TicketReturnedViewModel : BaseViewModel
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

        public TicketReturnedViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // Inicializar colección
            Products = new ObservableCollection<Product>();

            // Cargar inicial
            RefreshProductsFromService();

            // Suscribirse para refrescar automáticamente cuando cambie inventario
            _service.InventoryUpdated += OnInventoryUpdated;

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
        }

        // Actualiza la colección Products (en hilo UI)
        private void RefreshProductsFromService()
        {
            var list = _service.GetProducts()
                .Where(p => p.Status == ProductStatus.Active && p.StockQty > 0)
                .OrderBy(p => p.Name)
                .ToList();

            var app = Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    Products.Clear();
                    foreach (var p in list) Products.Add(p);
                });
            }
            else
            {
                Products.Clear();
                foreach (var p in list) Products.Add(p);
            }
        }

        private void OnInventoryUpdated()
        {
            try
            {
                RefreshProductsFromService();
            }
            catch
            {
                // No propagar excepción desde el hilo del evento
            }
        }

        // Si la instancia se desecha, es buena práctica desuscribirse.
        // Llamar a este método desde el cierre de la vista si procede.
        public void DisposeSubscriptions()
        {
            try { _service.InventoryUpdated -= OnInventoryUpdated; } catch { }
        }

        private void Save()
        {
            Error = "";

            if (SelectedProduct == null) { Error = "Select a product."; return; }
            if (Quantity <= 0) { Error = "Quantity must be > 0."; return; }

            if (Quantity > SelectedProduct.StockQty)
            {
                Error = $"Quantity exceeds available stock ({SelectedProduct.StockQty}).";
                return;
            }

            try
            {
                var ticket = new Ticket
                {
                    Type = TicketType.Returned,
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

                _service.CreateTicketReturned(ticket);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
