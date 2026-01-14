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

        private string _origin = "";
        public string Origin
        {
            get => _origin;
            set => SetProperty(ref _origin, value);
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
        public RelayCommand LoadFromReceivedCommand { get; }

        public event Action? RequestClose;

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
            LoadFromReceivedCommand = new RelayCommand(_ => LoadFromInventory());

            LoadProducts();

            // precargar productos disponibles para ADD (basado en pozo actual)
            LoadProductsForAdd();

            // NO cargar automáticamente las líneas al abrir la vista:
            // LoadFromInventory();  <-- eliminado intencionadamente

            // refrescar listas cuando cambie inventario
            _service.InventoryUpdated += () =>
            {
                LoadProducts();
                LoadProductsForAdd();
                // NO volver a llamar a LoadFromInventory() para evitar repoblar Lines automáticamente
            };
        }

        private void LoadProducts()
        {
            Products.Clear();
            var list = _service.GetProducts().Where(p => p.Status == ProductStatus.Active).OrderBy(p => p.Name).ToList();
            foreach (var p in list) Products.Add(p);
        }

        // Rellena AvailableProductsForAdd usando movimientos Received del pozo actual,
        // agrupando por ProductCode para evitar duplicados y excluyendo productos con stock == 0.
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
                else
                {
                    // si no hay pozo seleccionado, considerar todos los movimientos Received
                }

                // Catálogo para consultar StockQty y CurrentUnitCost
                var catalog = _service.GetProducts()
                    .Where(p => p.Status == ProductStatus.Active)
                    .ToDictionary(p => (p.Code ?? "").ToUpperInvariant(), p => p, StringComparer.OrdinalIgnoreCase);

                foreach (var g in movements.GroupBy(m => (m.ProductCode ?? "").ToUpperInvariant()))
                {
                    var code = g.Key;
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    // Preferir información del catálogo y requerir stock > 0
                    if (catalog.TryGetValue(code, out var prodCatalog))
                    {
                        if (prodCatalog.StockQty <= 0) continue; // excluir si stock 0
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
                        // Si no está en catálogo, sumar cantidad recibida en movimientos agrupados;
                        // incluir solo si suma > 0
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
            // Añadir línea vacía; Combo en celda usará AvailableProductsForAdd que ya está precargada
            Lines.Add(new TicketLine
            {
                ProductCode = string.Empty,
                ProductName = string.Empty,
                Quantity = 0,   // el usuario indicará cuánto devolver
                UnitPrice = 0,
                Context = Origin ?? string.Empty,
                Observations = string.Empty
            });

            Error = $"Línea agregada. Total líneas: {Lines.Count}";
        }

        private void RemoveLine(TicketLine? line)
        {
            if (line == null) return;
            Lines.Remove(line);
        }

        // Carga desde la lista principal de productos (inventario) productos con stock>0.
        private void LoadFromInventory()
        {
            try
            {
                Lines.Clear();

                var products = _service.GetProducts()
                    .Where(p => p.Status == ProductStatus.Active && p.StockQty > 0)
                    .OrderBy(p => p.Name)
                    .ToList();

                foreach (var p in products)
                {
                    Lines.Add(new TicketLine
                    {
                        ProductCode = p.Code ?? string.Empty,
                        ProductName = p.Name ?? string.Empty,
                        Quantity = 0, // el usuario especificará cuánto devolver
                        UnitPrice = p.CurrentUnitCost,
                        Context = Origin ?? string.Empty,
                        Observations = string.Empty
                    });
                }

                Error = $"Cargadas {Lines.Count} línea(s) desde inventario.";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        // Nuevo: cargar ticket existente (Returned) para editar
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
            Origin = first.OriginOrUse ?? string.Empty;
            Observations = first.Observations ?? string.Empty;
            User = first.User ?? Environment.UserName;

            Lines.Clear();

            // Cargar movimientos de tipo Returned como líneas editables
            foreach (var mv in movements.Where(m => m.Type == TicketType.Returned))
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

        // Nuevo: cargar por Requisition (agrupa movimientos Returned)
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

        // Save() se mantiene (usa Lines para crear Ticket Returned) + soporte edición
        private void Save()
        {
            Error = "";

            if (Lines.Count == 0)
            {
                Error = "No hay líneas para guardar.";
                return;
            }

            // Validaciones
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
            }

            try
            {
                // Ensure products exist and normalize codes/names
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
                            Unit = "Each",
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

                        // Si en la línea no se indicó UnitPrice, tomar el precio actual del producto
                        if (ln.UnitPrice <= 0)
                        {
                            ln.UnitPrice = existing.CurrentUnitCost;
                        }
                    }
                }

                // === NUEVA LÓGICA: si aún hay líneas sin precio, intentar tomar del último Received ===
                var allMovements = _service.GetMovements();
                foreach (var ln in Lines)
                {
                    if (ln.UnitPrice > 0) continue;

                    var code = (ln.ProductCode ?? "").Trim();
                    if (string.IsNullOrEmpty(code)) continue;

                    // Buscar último Received con precio > 0
                    var lastReceived = allMovements
                        .Where(m => string.Equals(m.ProductCode, code, StringComparison.OrdinalIgnoreCase) && m.Type == TicketType.Received && m.UnitPrice > 0)
                        .OrderByDescending(m => m.Date)
                        .FirstOrDefault();

                    if (lastReceived != null && lastReceived.UnitPrice > 0)
                    {
                        ln.UnitPrice = lastReceived.UnitPrice;
                        continue;
                    }

                    // Fallback: usar precio actual del producto del catálogo
                    var prodFromCatalog = currentProducts.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
                    if (prodFromCatalog != null && prodFromCatalog.CurrentUnitCost > 0)
                    {
                        ln.UnitPrice = prodFromCatalog.CurrentUnitCost;
                    }
                }
                // ========================================================================

                var ticket = new Ticket
                {
                    Type = TicketType.Returned,
                    Date = DateTime.Now,
                    User = User,
                    Observations = Observations,
                    Requisition = Requisition ?? string.Empty,
                    Lines = Lines.ToList()
                };

                // Si estamos editando un ticket existente, eliminar movimientos previos de ese ticket
                if (!string.IsNullOrWhiteSpace(EditingTicketId))
                {
                    _service.DeleteMovementsForTicket(EditingTicketId, removeLinkedByRequisition: false);
                    ticket.TicketId = EditingTicketId;
                }
                // Si estamos editando por Requisition, eliminar movimientos previos asociados a esa requisicion
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
    }
}
