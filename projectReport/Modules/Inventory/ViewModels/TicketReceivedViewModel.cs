using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Windows;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        public RelayCommand RefreshCommand { get; }

        public event Action? RequestClose;

        public TicketReceivedViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            Products = new ObservableCollection<Product>();

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
            RefreshCommand = new RelayCommand(_ => LoadProductsFromExcelOrRepo());

            // Carga inicial: intenta desde Data\Lista.xlsx (output) y si no existe usa el repo
            LoadProductsFromExcelOrRepo();
        }

        private void LoadProductsFromExcelOrRepo()
        {
            try
            {
                Products.Clear();

                // Ruta del fichero copiado al output por el proyecto
                var excelPath = Path.Combine(AppContext.BaseDirectory, "Data", "Lista.xlsx");
                if (!File.Exists(excelPath))
                {
                    // Fallback: intentar en la raíz del output por si el archivo fue copiado ahí
                    var alt = Path.Combine(AppContext.BaseDirectory, "Lista.xlsx");
                    if (File.Exists(alt)) excelPath = alt;
                }

                List<Product> loaded = new();

                if (File.Exists(excelPath))
                {
                    var importer = new InventoryExcelImportService();
                    var uni = importer.LoadUniversalProducts(excelPath);

                    foreach (var u in uni)
                    {
                        var p = new Product
                        {
                            Code = u.Codigo ?? string.Empty,
                            Name = string.IsNullOrWhiteSpace(u.Nombre) ? (u.Codigo ?? string.Empty) : u.Nombre,
                            Description = string.IsNullOrWhiteSpace(u.Categoria) ? string.Empty : u.Categoria,
                            Category = u.Categoria ?? string.Empty,
                            Unit = string.IsNullOrWhiteSpace(u.Unidad) ? "Each" : u.Unidad,
                            StockQty = 0,
                            CurrentUnitCost = 0,
                            Status = ProductStatus.Active
                        };
                        loaded.Add(p);
                    }
                }
                else
                {
                    // No hay Excel: cargar desde el repositorio (persistido)
                    loaded = _service.GetProducts().Where(p => p.Status == ProductStatus.Active).OrderBy(p => p.Name).ToList();
                }

                // Añadir a la colección ObservableCollection en el hilo UI
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() =>
                    {
                        foreach (var p in loaded) Products.Add(p);

                        // Inicializar FilteredProducts y seleccionar el primero
                        UpdateFilter(string.Empty);

                        if (FilteredProducts.Count > 0)
                        {
                            SelectedProduct = FilteredProducts.First();
                        }
                    });
                }
                else
                {
                    foreach (var p in loaded) Products.Add(p);
                    UpdateFilter(string.Empty);
                    if (FilteredProducts.Count > 0) SelectedProduct = FilteredProducts.First();
                }

                // Aviso mínimo para depuración si no hay productos
                if (loaded.Count == 0)
                {
                    MessageBox.Show("No products found in Data\\Lista.xlsx or repository.", "No products", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // Mostrar error y fallback al repo
                MessageBox.Show($"Error loading list from Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Products.Clear();
                var list = _service.GetProducts().Where(p => p.Status == ProductStatus.Active).OrderBy(p => p.Name);
                foreach (var p in list) Products.Add(p);
                UpdateFilter(string.Empty);
                if (FilteredProducts.Count > 0) SelectedProduct = FilteredProducts.First();
            }
        }

        private void UpdateFilter(string text)
        {
            FilteredProducts.Clear();

            if (string.IsNullOrWhiteSpace(text))
            {
                foreach (var p in Products) FilteredProducts.Add(p);
                return;
            }

            var q = text.Trim();
            var matches = Products.Where(p =>
                (!string.IsNullOrEmpty(p.Code) && p.Code.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(p.Name) && p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var m in matches) FilteredProducts.Add(m);
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
                    // Requisition removed from UI — do not set here
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
