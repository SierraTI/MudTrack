using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;
using ProjectReport.Models;
using ProjectReport.Services;

namespace ProjectReport.ViewModels.Inventory
{
    public class TicketReturnedViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        // Lista de productos y líneas (tabla)
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<TicketLine> Lines { get; } = new();

        // Productos disponibles para el Combo al añadir (filtrados por movimientos Received del pozo)
        public ObservableCollection<Product> AvailableProductsForAdd { get; } = new();

        private string _requisition = "";
        public string Requisition
        {
            get => _requisition;
            set => SetProperty(ref _requisition, value);
        }

        private string _destination = "";
        public string Destination
        {
            get => _destination;
            set => SetProperty(ref _destination, value);
        }

        private string _supplierName = "";
        public string SupplierName
        {
            get => _supplierName;
            set => SetProperty(ref _supplierName, value);
        }

        private string _shipmentMethod = "";
        public string ShipmentMethod
        {
            get => _shipmentMethod;
            set => SetProperty(ref _shipmentMethod, value);
        }

        private string _shipmentReference = "";
        public string ShipmentReference
        {
            get => _shipmentReference;
            set => SetProperty(ref _shipmentReference, value);
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
        public RelayCommand AddLineCommand { get; }
        public RelayCommand RemoveLineCommand { get; }
        public RelayCommand SelectProductsCommand { get; }
        public RelayCommand LoadFromReceivedCommand { get; }

        public event Action? RequestClose;
        public event Action? RequestSelectProducts;

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

        public TicketReturnedViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
            AddLineCommand = new RelayCommand(_ => AddLine());
            RemoveLineCommand = new RelayCommand(param => RemoveLine(param as TicketLine));
            SelectProductsCommand = new RelayCommand(_ => LoadFromInventory());
            LoadFromReceivedCommand = new RelayCommand(_ => LoadFromInventory());

            LoadProducts();

            // precargar productos disponibles para ADD (basado en pozo actual)
            LoadProductsForAdd();

            // NO cargar automáticamente las líneas al abrir la vista
            
            // Subscribe to batch selection event
            WellContextService.Instance.ChemicalSelectionUpdated += OnChemicalSelectionUpdated;

            // refrescar listas cuando cambie inventario
            _service.InventoryUpdated += () =>
            {
                LoadProducts();
                LoadProductsForAdd();
            };
        }

        private void OnChemicalSelectionUpdated(object? sender, ChemicalSelectionUpdatedEventArgs e)
        {
            if (e.SelectedItems == null || !e.SelectedItems.Any()) return;

            var productUnits = _service.GetProducts()
                .ToDictionary(
                    p => p.Code ?? string.Empty,
                    p => p.Unit ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var item in e.SelectedItems)
            {
                // Avoid adding the same product twice in the same ticket draft
                bool alreadyExists = Lines.Any(l => string.Equals(l.ProductCode, item.Code, StringComparison.OrdinalIgnoreCase));
                if (alreadyExists) continue;

                var unitFromChemical = item.Unidad ?? string.Empty;
                var unitFromProduct = productUnits.TryGetValue(item.Code ?? string.Empty, out var mappedUnit)
                    ? mappedUnit
                    : string.Empty;
                var resolvedUnit = !string.IsNullOrWhiteSpace(unitFromChemical)
                    ? unitFromChemical
                    : (!string.IsNullOrWhiteSpace(unitFromProduct) ? unitFromProduct : "Each");

                Lines.Add(new TicketLine
                {
                    ProductCode = item.Code,
                    ProductName = item.Nombre ?? item.Code,
                    Unit = resolvedUnit,
                    Quantity = 1,
                    Context = Destination
                });
            }

            Error = $"{e.SelectedItems.Count} products added from selection.";
        }

        private void LoadProducts()
        {
            Products.Clear();
            var list = _service.GetProducts()
                               .Where(p => p.Status == ProductStatus.Active && p.IsSelectedForReport)
                               .OrderBy(p => p.Name)
                               .ToList();
            foreach (var p in list) Products.Add(p);
        }

        private void LoadProductsForAdd()
        {
            try
            {
                AvailableProductsForAdd.Clear();

                var movements = _service.GetMovements()
                    .Where(m => m.Type == TicketType.Received)
                    .ToList();

                var currentWell = WellContextService.Instance.CurrentWell;
                if (currentWell != null && !string.IsNullOrWhiteSpace(currentWell.WellName))
                {
                    var wellName = currentWell.WellName.Trim();
                    movements = movements
                        .Where(m => !string.IsNullOrWhiteSpace(m.OriginOrUse) &&
                                    m.OriginOrUse.IndexOf(wellName, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                }

                var catalog = _service.GetProducts()
                    .Where(p => p.Status == ProductStatus.Active && p.IsSelectedForReport)
                    .ToDictionary(p => (p.Code ?? "").ToUpperInvariant(), p => p, StringComparer.OrdinalIgnoreCase);

                foreach (var g in movements.GroupBy(m => (m.ProductCode ?? "").ToUpperInvariant()))
                {
                    var code = g.Key;
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    if (catalog.TryGetValue(code, out var prodCatalog))
                    {
                        if (prodCatalog.StockQty <= 0) continue; 
                        if (!AvailableProductsForAdd.Any(p => string.Equals(p.Code, prodCatalog.Code, StringComparison.OrdinalIgnoreCase)))
                        {
                            AvailableProductsForAdd.Add(new Product
                            {
                                Code = prodCatalog.Code,
                                Name = prodCatalog.Name,
                                CurrentUnitCost = prodCatalog.CurrentUnitCost,
                                Status = prodCatalog.Status,
                                StockQty = prodCatalog.StockQty
                            });
                        }
                    }
                    else
                    {
                        var totalReceived = g.Sum(x => x.Quantity);
                        if (totalReceived <= 0) continue;

                        var first = g.First();
                        var p = new Product
                        {
                            Code = first.ProductCode ?? string.Empty,
                            Name = first.ProductName ?? first.ProductCode ?? string.Empty,
                            CurrentUnitCost = first.UnitPrice
                        };

                        if (!AvailableProductsForAdd.Any(x => string.Equals(x.Code, p.Code, StringComparison.OrdinalIgnoreCase)))
                            AvailableProductsForAdd.Add(p);
                    }
                }
            }
            catch
            {
                // No propagar excepción al UI
            }
        }

        private void AddLine()
        {
            Lines.Add(new TicketLine
            {
                ProductCode = string.Empty,
                ProductName = string.Empty,
                Quantity = 0,
                UnitPrice = 0,
                Context = Destination ?? string.Empty,
                Observations = string.Empty
            });

            Error = $"Línea agregada. Total líneas: {Lines.Count}";
        }

        private void RemoveLine(TicketLine? line)
        {
            if (line == null) return;
            Lines.Remove(line);
        }

        private void LoadFromInventory()
        {
            try
            {
                Lines.Clear();

                var movements = _service.GetMovements();
                var receivedProductCodes = movements
                    .Where(m => m.Type == TicketType.Received)
                    .Select(m => m.ProductCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var products = _service.GetProducts()
                    .Where(p => receivedProductCodes.Contains(p.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    .OrderBy(p => p.Name)
                    .ToList();

                int addedCount = 0;
                foreach (var p in products)
                {
                    bool alreadyExists = Lines.Any(l => string.Equals(l.ProductCode, p.Code, StringComparison.OrdinalIgnoreCase));
                    if (alreadyExists) continue;

                    var line = new TicketLine
                    {
                        ProductCode = p.Code ?? string.Empty,
                        ProductName = p.Name ?? string.Empty,
                        Unit = string.IsNullOrWhiteSpace(p.Unit) ? "Each" : p.Unit,
                        Quantity = 0,
                        UnitPrice = p.CurrentUnitCost,
                        Context = Destination ?? string.Empty,
                        Observations = string.Empty
                    };

                    // Calculate quantity received for this product
                    var quantityReceived = movements
                        .Where(m => m.Type == TicketType.Received && 
                                    string.Equals(m.ProductCode, p.Code, StringComparison.OrdinalIgnoreCase))
                        .Sum(m => m.Quantity);

                    line.QuantityReceived = quantityReceived;
                    line.CurrentStock = p.StockQty;
                    ValidateReturnQuantity(line);

                    Lines.Add(line);
                    addedCount++;
                }

                Error = $"Cargadas {addedCount} línea(s) desde inventario recibido.";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        public void LoadTicket(string ticketId)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) return;

            var movements = _service.GetMovements()
                .Where(m => string.Equals(m.TicketId, ticketId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Date)
                .ToList();

            if (movements.Count == 0) return;

            var first = movements.First();
            EditingTicketId = ticketId;
            Requisition = first.Requisition ?? string.Empty;
            Destination = first.OriginOrUse ?? string.Empty;
            SupplierName = first.SupplierName ?? string.Empty;
            ShipmentMethod = first.ShipmentMethod ?? string.Empty;
            ShipmentReference = first.Remision ?? string.Empty;
            Observations = first.Observations ?? string.Empty;
            User = first.User ?? Environment.UserName;

            Lines.Clear();

            foreach (var mv in movements.Where(m => m.Type == TicketType.Returned))
            {
                var line = new TicketLine
                {
                    ProductCode = mv.ProductCode ?? string.Empty,
                    ProductName = mv.ProductName ?? string.Empty,
                    Unit = _service.GetProducts().FirstOrDefault(p => string.Equals(p.Code, mv.ProductCode, StringComparison.OrdinalIgnoreCase))?.Unit ?? "Each",
                    Quantity = mv.Quantity,
                    UnitPrice = mv.UnitPrice,
                    Context = mv.OriginOrUse ?? string.Empty,
                    Observations = mv.Observations ?? string.Empty
                };

                // Calculate quantity received for this product
                var quantityReceived = _service.GetMovements()
                    .Where(m => m.Type == TicketType.Received && 
                                string.Equals(m.ProductCode, mv.ProductCode, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.Quantity);

                line.QuantityReceived = quantityReceived;
                ValidateReturnQuantity(line);

                Lines.Add(line);
            }

            OnPropertyChanged(nameof(Lines));
        }

        public void LoadByRequisition(string requisition)
        {
            if (string.IsNullOrWhiteSpace(requisition)) return;

            var movements = _service.GetMovements()
                .Where(m => !string.IsNullOrWhiteSpace(m.Requisition) &&
                            string.Equals(m.Requisition, requisition, StringComparison.OrdinalIgnoreCase) &&
                            m.Type == TicketType.Returned)
                .OrderBy(m => m.Date)
                .ToList();

            if (movements.Count == 0) return;

            EditingTicketId = null;
            EditingRequisition = requisition;
            var first = movements.First();
            Requisition = first.Requisition ?? string.Empty;
            Destination = first.OriginOrUse ?? string.Empty;
            SupplierName = first.SupplierName ?? string.Empty;
            ShipmentMethod = first.ShipmentMethod ?? string.Empty;
            ShipmentReference = first.Remision ?? string.Empty;
            Observations = first.Observations ?? string.Empty;
            User = first.User ?? Environment.UserName;

            Lines.Clear();

            foreach (var mv in movements)
            {
                var line = new TicketLine
                {
                    ProductCode = mv.ProductCode ?? string.Empty,
                    ProductName = mv.ProductName ?? string.Empty,
                    Unit = _service.GetProducts().FirstOrDefault(p => string.Equals(p.Code, mv.ProductCode, StringComparison.OrdinalIgnoreCase))?.Unit ?? "Each",
                    Quantity = mv.Quantity,
                    UnitPrice = mv.UnitPrice,
                    Context = mv.OriginOrUse ?? string.Empty,
                    Observations = mv.Observations ?? string.Empty
                };

                // Calculate quantity received for this product
                var quantityReceived = _service.GetMovements()
                    .Where(m => m.Type == TicketType.Received && 
                                string.Equals(m.ProductCode, mv.ProductCode, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.Quantity);

                line.QuantityReceived = quantityReceived;
                ValidateReturnQuantity(line);

                Lines.Add(line);
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

            for (int i = 0; i < Lines.Count; i++)
            {
                var ln = Lines[i];
                if (string.IsNullOrWhiteSpace(ln.ProductCode) && string.IsNullOrWhiteSpace(ln.ProductName))
                {
                    Error = $"Línea {i + 1}: producto requerido.";
                    return;
                }
                if (ln.Quantity <= 0)
                {
                    Error = $"Línea {i + 1}: la cantidad debe ser mayor que 0.";
                    return;
                }

                // Validate that return quantity doesn't exceed received quantity
                if (ln.Quantity > ln.QuantityReceived)
                {
                    Error = $"Línea {i + 1}: No puede devolver {ln.Quantity} {ln.ProductName}. Solo fue recibido {ln.QuantityReceived}.";
                    return;
                }
            }

            try
            {
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
                        var newProd = new Product
                        {
                            Code = string.IsNullOrWhiteSpace(code) ? GenerateCodeFromName(name) : code,
                            Name = string.IsNullOrWhiteSpace(name) ? $"PROD_{DateTime.Now.Ticks % 100000}" : name,
                            Description = string.Empty,
                            Category = string.Empty,
                            Unit = string.IsNullOrWhiteSpace(ln.Unit) ? "Each" : ln.Unit,
                            StockQty = 0,
                            CurrentUnitCost = ln.UnitPrice,
                            Status = ProductStatus.Active
                        };
                        _service.UpsertProduct(newProd);
                        currentProducts = _service.GetProducts();
                        ln.ProductCode = newProd.Code;
                        ln.ProductName = newProd.Name;
                    }
                    else
                    {
                        ln.ProductCode = existing.Code;
                        ln.ProductName = existing.Name;
                        ln.Unit = string.IsNullOrWhiteSpace(existing.Unit) ? "Each" : existing.Unit;

                        if (ln.UnitPrice <= 0)
                        {
                            ln.UnitPrice = existing.CurrentUnitCost;
                        }
                    }
                }

                var allMovements = _service.GetMovements();
                foreach (var ln in Lines)
                {
                    if (ln.UnitPrice > 0) continue;

                    var code = (ln.ProductCode ?? "").Trim();
                    if (string.IsNullOrEmpty(code)) continue;

                    var lastReceived = allMovements
                        .Where(m => string.Equals(m.ProductCode, code, StringComparison.OrdinalIgnoreCase) && m.Type == TicketType.Received && m.UnitPrice > 0)
                        .OrderByDescending(m => m.Date)
                        .FirstOrDefault();

                    if (lastReceived != null && lastReceived.UnitPrice > 0)
                    {
                        ln.UnitPrice = lastReceived.UnitPrice;
                        continue;
                    }

                    var prodFromCatalog = currentProducts.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
                    if (prodFromCatalog != null && prodFromCatalog.CurrentUnitCost > 0)
                    {
                        ln.UnitPrice = prodFromCatalog.CurrentUnitCost;
                    }
                }

                var ticket = new Ticket
                {
                    Type = TicketType.Returned,
                    Date = DateTime.Now,
                    User = User,
                    Observations = Observations,
                    Requisition = Requisition ?? string.Empty,
                    Origin = Destination,
                    SupplierName = SupplierName,
                    ShipmentMethod = ShipmentMethod,
                    Remision = ShipmentReference,
                    Lines = Lines.ToList()
                };

                if (!string.IsNullOrWhiteSpace(EditingTicketId))
                {
                    _service.DeleteMovementsForTicket(EditingTicketId, removeLinkedByRequisition: false);
                    ticket.TicketId = EditingTicketId;
                }
                else if (!string.IsNullOrWhiteSpace(EditingRequisition))
                {
                    _service.DeleteMovementsByRequisition(EditingRequisition);
                    ticket.Requisition = EditingRequisition;
                }

                _service.CreateTicketReturned(ticket);

                Lines.Clear();
                Error = "Ticket de devolución guardado correctamente.";
                EditingTicketId = null;
                EditingRequisition = null;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        private string GenerateCodeFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();

            var cleaned = new string(name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
            var parts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var baseCode = string.Join("_", parts).Replace("__", "_");
            if (baseCode.Length > 20) baseCode = baseCode.Substring(0, 20);
            var suffix = DateTime.Now.Ticks % 10000;
            return $"{baseCode}_{suffix}";
        }

        // Helper method to validate and update error message for return quantity
        private void ValidateReturnQuantity(TicketLine line)
        {
            if (line == null) return;

            line.ValidationError = "";

            if (line.Quantity > 0 && line.QuantityReceived > 0 && line.Quantity > line.QuantityReceived)
            {
                line.ValidationError = $"⚠️ Cannot return {line.Quantity} - Only {line.QuantityReceived} received";
            }
            else if (line.Quantity > 0 && line.QuantityReceived == 0)
            {
                line.ValidationError = "⚠️ No quantity received for this product";
            }
        }
    }
}
