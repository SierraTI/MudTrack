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
using ProjectReport.Services;
using ProjectReport.Models;

namespace ProjectReport.ViewModels.Inventory
{
    public partial class TicketReceivedViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<Product> Products { get; }
        public ObservableCollection<Product> FilteredProducts { get; } = new();

        // Draft lines stored until Save
        public ObservableCollection<TicketLine> Lines { get; } = new();

        private bool _isRigFilterEnabled;
        public bool IsRigFilterEnabled
        {
            get => _isRigFilterEnabled;
            set
            {
                if (SetProperty(ref _isRigFilterEnabled, value))
                    UpdateFilter(ProductName);
            }
        }

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
                        Category = _selectedProduct.Category ?? string.Empty;
                        Unit = _selectedProduct.Unit ?? string.Empty;
                    }
                }
            }
        }

        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set
            {
                if (SetProperty(ref _productName, value))
                {
                    UpdateFilter(_productName);
                }
            }
        }

        private string _productCode = "";
        public string ProductCode
        {
            get => _productCode;
            set => SetProperty(ref _productCode, value);
        }

        // Requisition visible in UI (shared for all lines)
        private string _requisition = "";
        public string Requisition
        {
            get => _requisition;
            set => SetProperty(ref _requisition, value);
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

        // Category & Unit for creating new product when needed
        private string _category = "";
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _unit = "";
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
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

        public RelayCommand AddLineCommand { get; }
        public RelayCommand RemoveLineCommand { get; }

        public event Action? RequestClose;

        public TicketReceivedViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            Products = new ObservableCollection<Product>();

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
            RefreshCommand = new RelayCommand(_ => LoadProductsFromExcelOrRepo());

            AddLineCommand = new RelayCommand(_ => AddLine());
            RemoveLineCommand = new RelayCommand(param => RemoveLine(param as TicketLine));

            LoadProductsFromExcelOrRepo();
        }

        // Nuevo: añade una fila vacía (editable) al borrador y prefill Context con Origin.
        private void AddLine()
        {
            Error = "";

            try
            {
                var line = new TicketLine
                {
                    ProductCode = string.Empty,
                    ProductName = string.Empty,
                    Quantity = 1,           // valor por defecto para facilitar edición
                    UnitPrice = 0,
                    Context = Origin        // prefill origin desde el campo superior
                };

                Lines.Add(line);

                // Log + feedback para depuración
                System.Diagnostics.Debug.WriteLine($"[TicketReceived] AddLine invoked: Lines.Count = {Lines.Count}");
                Error = $"Línea agregada en borrador. Total líneas: {Lines.Count}";

                // Limpiar sólo inputs de entrada rápida (mantener Requisition y Origin)
                Quantity = 0;
                UnitPrice = 0;
                ProductName = string.Empty;
                ProductCode = string.Empty;
                SelectedProduct = null;
                Category = string.Empty;
                Unit = string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TicketReceived] AddLine exception: {ex}");
                Error = "Error al agregar línea: " + ex.Message;
            }
        }

        private void RemoveLine(TicketLine? line)
        {
            if (line == null) return;
            Lines.Remove(line);
        }

        private void LoadProductsFromExcelOrRepo()
        {
            try
            {
                Products.Clear();

                var excelPath = Path.Combine(AppContext.BaseDirectory, "Data", "Lista.xlsx");
                if (!File.Exists(excelPath))
                {
                    var alt = Path.Combine(AppContext.BaseDirectory, "Lista.xlsx");
                    if (File.Exists(alt)) excelPath = alt;
                }

                var loaded = new List<Product>();

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
                    loaded = _service.GetProducts().Where(p => p.Status == ProductStatus.Active).OrderBy(p => p.Name).ToList();
                }

                var app = Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() =>
                    {
                        foreach (var p in loaded) Products.Add(p);
                        UpdateFilter(string.Empty);
                        if (FilteredProducts.Count > 0) SelectedProduct = FilteredProducts.First();
                    });
                }
                else
                {
                    foreach (var p in loaded) Products.Add(p);
                    UpdateFilter(string.Empty);
                    if (FilteredProducts.Count > 0) SelectedProduct = FilteredProducts.First();
                }

                if (loaded.Count == 0)
                {
                    MessageBox.Show("No products found in Data\\Lista.xlsx or repository.", "No products", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading list from Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Products.Clear();
                var list = _service.GetProducts().Where(p => p.Status == ProductStatus.Active).OrderBy(p => p.Name);
                foreach (var p in list) Products.Add(p);
                UpdateFilter(string.Empty);
                if (FilteredProducts.Count > 0) SelectedProduct = FilteredProducts.First();
            }
        }

        // Reemplaza el método UpdateFilter por este para buscar en SearchLabel (case-insensitive)
        private void UpdateFilter(string text)
        {
            FilteredProducts.Clear();

            var query = (text ?? "").Trim();
            if (string.IsNullOrEmpty(query))
            {
                foreach (var p in Products) FilteredProducts.Add(p);
                return;
            }

            var normalized = query.ToUpperInvariant();

            var allProducts = Products.ToList();

            var rig = WellContextService.Instance.CurrentWell?.RigProfile;
            var shakerKeywords = new List<string>();
            if (IsRigFilterEnabled && rig != null)
            {
                shakerKeywords = rig.SolidsControl
                    .Where(sc => sc.Type?.IndexOf("Shaker", StringComparison.OrdinalIgnoreCase) >= 0)
                    .SelectMany(sc => new[] { sc.Manufacturer, sc.Model })
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();
            }

            foreach (var p in allProducts)
            {
                var label = (p.SearchLabel ?? "").ToUpperInvariant();

                // Si está filtrado por Rig mostrar solo pantallas relevantes
                if (IsRigFilterEnabled && shakerKeywords.Count > 0 &&
                    p.Category?.IndexOf("Screen", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool isMatchRig = shakerKeywords.Any(k =>
                        (!string.IsNullOrEmpty(p.Name) && p.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(p.Code) && p.Code.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));

                    if (!isMatchRig) continue;
                }

                if (label.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0)
                    FilteredProducts.Add(p);
            }
        }

        private string GenerateCodeFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();

            // Simple sanitized code: take alphanumerics, replace spaces with underscore, uppercase, truncate
            var cleaned = new string(name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
            var parts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var baseCode = string.Join("_", parts).Replace("__", "_");
            if (baseCode.Length > 20) baseCode = baseCode.Substring(0, 20);
            // ensure uniqueness suffix
            var suffix = DateTime.Now.Ticks % 10000;
            return $"{baseCode}_{suffix}";
        }

        // Nuevo: Id del ticket que se está editando (si aplica)
        private string? _editingTicketId;
        public string? EditingTicketId
        {
            get => _editingTicketId;
            private set => SetProperty(ref _editingTicketId, value);
        }

        // Nuevo: Requisition por la que se está editando (si aplica)
        private string? _editingRequisition;
        public string? EditingRequisition
        {
            get => _editingRequisition;
            private set => SetProperty(ref _editingRequisition, value);
        }

        // Nuevo: cargar por Requisition (agrupa movimientos Received)
        public void LoadByRequisition(string requisition)
        {
            if (string.IsNullOrWhiteSpace(requisition)) return;

            var movements = _service.GetMovements()
                .Where(m => !string.IsNullOrWhiteSpace(m.Requisition) &&
                            string.Equals(m.Requisition, requisition, StringComparison.OrdinalIgnoreCase) &&
                            m.Type == TicketType.Received)
                .OrderBy(m => m.Date)
                .ToList();

            if (movements.Count == 0) return;

            EditingTicketId = null;
            EditingRequisition = requisition;
            var first = movements.First();
            Requisition = first.Requisition ?? string.Empty;
            Origin = first.OriginOrUse ?? string.Empty;
            Observations = first.Observations ?? string.Empty;
            User = first.User ?? Environment.UserName;

            Lines.Clear();

            foreach (var mv in movements)
            {
                Lines.Add(new TicketLine
                {
                    ProductCode = mv.ProductCode ?? string.Empty,
                    ProductName = mv.ProductName ?? string.Empty,
                    Quantity = mv.Quantity,
                    UnitPrice = mv.UnitPrice,
                    Context = mv.OriginOrUse ?? string.Empty,
                    Observations = mv.Observations ?? string.Empty
                });
            }

            OnPropertyChanged(nameof(Lines));
        }

        private void Save()
        {
            Error = "";

            if (Lines.Count == 0)
            {
                Error = "No hay líneas para guardar.";
                return;
            }

            // Validar cada línea antes de persistir
            for (int i = 0; i < Lines.Count; i++)
            {
                var ln = Lines[i];
                if (string.IsNullOrWhiteSpace(ln.ProductName) && string.IsNullOrWhiteSpace(ln.ProductCode))
                {
                    Error = $"Línea {i + 1}: producto requerido.";
                    return;
                }
                if (ln.Quantity <= 0)
                {
                    Error = $"Línea {i + 1}: la cantidad debe ser mayor que 0.";
                    return;
                }
            }

            // Ensure all products exist in catalog before creating ticket
            var currentProducts = _service.GetProducts();
            foreach (var ln in Lines)
            {
                var code = (ln.ProductCode ?? "").Trim();
                var name = (ln.ProductName ?? "").Trim();

                Product? existing = null;
                if (!string.IsNullOrEmpty(code))
                {
                    existing = currentProducts.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
                }

                if (existing == null && !string.IsNullOrEmpty(name))
                {
                    existing = currentProducts.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                }

                if (existing == null)
                {
                    // create new product and persist
                    var newProd = new Product
                    {
                        Code = string.IsNullOrWhiteSpace(code) ? GenerateCodeFromName(name) : code,
                        Name = string.IsNullOrWhiteSpace(name) ? newProdCodePlaceholder(code) : name,
                        Description = string.Empty,
                        Category = Category ?? string.Empty,
                        Unit = string.IsNullOrWhiteSpace(Unit) ? "Each" : Unit,
                        StockQty = 0,
                        CurrentUnitCost = ln.UnitPrice,
                        Status = ProductStatus.Active
                    };

                    // Upsert via service
                    _service.UpsertProduct(newProd);

                    // update local list and assign code to line
                    currentProducts = _service.GetProducts();
                    ln.ProductCode = newProd.Code;
                    ln.ProductName = newProd.Name;
                }
                else
                {
                    // ensure line has product code set
                    ln.ProductCode = existing.Code;
                    ln.ProductName = existing.Name;
                }
            }

            var ticket = new Ticket
            {
                Type = TicketType.Received,
                Date = DateTime.Now,
                User = User,
                Observations = Observations,
                Requisition = Requisition ?? string.Empty,
                Lines = Lines.ToList()
            };

            try
            {
                // If editing an existing ticket by TicketId, remove previous movements for that ticket
                if (!string.IsNullOrWhiteSpace(EditingTicketId))
                {
                    _service.DeleteMovementsForTicket(EditingTicketId, removeLinkedByRequisition: false);
                    ticket.TicketId = EditingTicketId;
                }
                // If editing by requisition (loaded via LoadByRequisition), remove movements with that requisition
                else if (!string.IsNullOrWhiteSpace(EditingRequisition))
                {
                    _service.DeleteMovementsByRequisition(EditingRequisition);
                    ticket.Requisition = EditingRequisition;
                }

                _service.CreateTicketReceived(ticket);

                // Clear draft lines and close
                Lines.Clear();
                Error = "Ticket guardado correctamente.";
                EditingTicketId = null;
                EditingRequisition = null;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        // Helper to provide fallback name when only code provided
        private string newProdCodePlaceholder(string code)
        {
            return string.IsNullOrWhiteSpace(code) ? $"PROD_{DateTime.Now.Ticks % 100000}" : code;
        }
    }
}
