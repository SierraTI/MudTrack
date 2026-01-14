using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;
using ProjectReport.Models;
using ProjectReport.Services;
using System.Globalization;

namespace ProjectReport.ViewModels.Inventory
{
    public class WholeFluidsViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<string> Locations { get; } = new();
        private string? _selectedLocation;
        public string? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (SetProperty(ref _selectedLocation, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<FluidLine> Lines { get; } = new();

        private string _movementType = "Ingreso";
        public string MovementType
        {
            get => _movementType;
            set
            {
                if (SetProperty(ref _movementType, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public DateTime Date { get; } = DateTime.Now;

        private string _requisition = "";
        public string Requisition
        {
            get => _requisition;
            set
            {
                if (SetProperty(ref _requisition, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _origin = "";
        public string Origin
        {
            get => _origin;
            set
            {
                if (SetProperty(ref _origin, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _reference = "";
        public string Reference
        {
            get => _reference;
            set => SetProperty(ref _reference, value);
        }

        private string _error = "";
        public string Error
        {
            get => _error;
            set => SetProperty(ref _error, value);
        }

        // Renombrada para evitar colisiones con tipos llamados User en el proyecto
        private string _currentUser = Environment.UserName;
        public string CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public ObservableCollection<ProjectReport.Models.Inventory.Product> Products { get; } = new();

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand AddAllFromCatalogCommand { get; }
        public RelayCommand LoadLastMovementCommand { get; }

        public RelayCommand AddLineCommand { get; }
        public RelayCommand RemoveLineCommand { get; }

        public event Action? RequestClose;

        public WholeFluidsViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
            AddAllFromCatalogCommand = new RelayCommand(_ => LoadFluidCatalog());
            LoadLastMovementCommand = new RelayCommand(_ => LoadLastMovement());

            // Habilita/inhabilita ADD según campos superiores
            AddLineCommand = new RelayCommand(_ => AddLine(), _ => CanAddLine());
            RemoveLineCommand = new RelayCommand(param => RemoveLine(param as FluidLine));

            LoadLocations();
            LoadProducts();

            // NOTE: no cargamos el catálogo en Lines por defecto.
            // El usuario debe pulsar ADD para empezar a añadir filas,
            // o usar "Cargar catálogo" para pre-poblar muchas líneas.

            // refrescar catálogo/listas si cambia inventario
            _service.InventoryUpdated += () =>
            {
                LoadLocations();
                LoadProducts();
            };
        }

        private void LoadProducts()
        {
            Products.Clear();
            var list = _service.GetProducts().Where(p => p.Status == ProductStatus.Active).OrderBy(p => p.Name).ToList();
            foreach (var p in list) Products.Add(p);
        }

        private void LoadLocations()
        {
            Locations.Clear();
            var current = WellContextService.Instance.CurrentWell;
            if (current != null && !string.IsNullOrWhiteSpace(current.WellName))
            {
                Locations.Add(current.WellName);
                SelectedLocation = current.WellName;
            }
            else
            {
                Locations.Add("Default");
                if (SelectedLocation == null) SelectedLocation = "Default";
            }
        }

        // Carga catálogo de fluidos en Lines (uso opcional, deja habilitadas=false)
        private void LoadFluidCatalog()
        {
            Lines.Clear();

            var products = _service.GetProducts()
                .Where(p => p.Status == ProductStatus.Active)
                .Where(p =>
                    (p.Unit ?? "").IndexOf("gal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (p.Category ?? "").IndexOf("fluid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (p.Name ?? "").IndexOf("fluid", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.Name)
                .ToList();

            if (products.Count == 0)
            {
                products = _service.GetProducts()
                    .Where(p => p.Status == ProductStatus.Active)
                    .OrderBy(p => p.Name)
                    .ToList();
            }

            foreach (var p in products)
            {
                Lines.Add(new FluidLine
                {
                    ProductCode = p.Code ?? "",
                    ProductName = p.Name ?? p.Code ?? "",
                    Enabled = false,
                    Barrels = 0,
                    Price = p.CurrentUnitCost,
                    AvailableStock = p.StockQty,
                    Observations = string.Empty
                });
            }
        }

        // ADD línea vacía para que el usuario la complete (solo si campos superiores están rellenos)
        private void AddLine()
        {
            // limpiar posible error previo
            Error = "";

            var line = new FluidLine
            {
                ProductCode = "",
                ProductName = "",
                Enabled = true,
                Barrels = 0,
                Price = 0,
                AvailableStock = 0,
                Observations = Reference ?? ""
            };

            Lines.Add(line);

            // Forzar reevaluación de comandos por si hace falta
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveLine(FluidLine? line)
        {
            if (line == null) return;
            Lines.Remove(line);
        }

        // Carga último movimiento de fluidos (si existe) y marca líneas correspondientes
        private void LoadLastMovement()
        {
            try
            {
                var lastReceived = _service.GetMovements()
                    .Where(m => m.Type == TicketType.Received && (m.ProductName ?? "").Length > 0)
                    .OrderByDescending(m => m.Date)
                    .Take(200)
                    .ToList();

                if (lastReceived.Count == 0)
                {
                    MessageBox.Show("No hay movimientos recientes.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var mv in lastReceived)
                {
                    var line = Lines.FirstOrDefault(l => string.Equals((l.ProductCode ?? ""), (mv.ProductCode ?? ""), StringComparison.OrdinalIgnoreCase));
                    if (line != null)
                    {
                        line.Enabled = true;
                        line.Barrels = mv.Quantity;
                        line.Price = mv.UnitPrice > 0 ? mv.UnitPrice : line.Price;
                        line.Observations = mv.Observations ?? Reference ?? "";
                    }
                }

                Error = "Último movimiento cargado (sugerencias).";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        private bool CanAddLine()
        {
            // Requisición y Origen obligatorios antes de añadir una nueva fila
            return !string.IsNullOrWhiteSpace(Requisition) && !string.IsNullOrWhiteSpace(Origin);
        }

        private bool ValidateAll()
        {
            Error = "";

            if (!Lines.Any(l => l.Enabled))
            {
                Error = "Marca al menos un fluido para mover.";
                return false;
            }

            foreach (var l in Lines.Where(x => x.Enabled))
            {
                if (l.Barrels <= 0)
                {
                    Error = $"Barriles debe ser > 0 para {l.ProductName}.";
                    return false;
                }

                if (MovementType == "Ingreso" && l.Price <= 0)
                {
                    Error = $"Precio es obligatorio en Ingreso para {l.ProductName}.";
                    return false;
                }

                if (MovementType == "Salida")
                {
                    var prod = _service.GetProducts().FirstOrDefault(p => string.Equals(p.Code, l.ProductCode, StringComparison.OrdinalIgnoreCase));
                    if (prod != null)
                    {
                        if (l.Barrels > prod.StockQty)
                        {
                            Error = $"Salida inválida para {l.ProductName}: disponible {prod.StockQty}.";
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void Save()
        {
            if (!ValidateAll()) return;

            try
            {
                var ticket = new Ticket
                {
                    Type = MovementType == "Ingreso" ? TicketType.Received : TicketType.Returned,
                    Date = DateTime.Now,
                    User = CurrentUser,
                    Observations = Reference ?? "",
                    Requisition = Requisition ?? ""
                };

                ticket.Lines = Lines
                    .Where(l => l.Enabled)
                    .Select(l =>
                    {
                        // Normalizar nombre/código desde catálogo si es posible
                        var prod = _service.GetProducts().FirstOrDefault(p => string.Equals(p.Code, l.ProductCode, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, l.ProductName, StringComparison.OrdinalIgnoreCase));
                        var code = l.ProductCode;
                        var name = l.ProductName;
                        if (prod != null)
                        {
                            code = prod.Code;
                            name = prod.Name;
                        }

                        return new TicketLine
                        {
                            ProductCode = code,
                            ProductName = name,
                            Quantity = l.Barrels,
                            UnitPrice = l.Price,
                            Context = Origin ?? ""
                        };
                    })
                    .ToList();

                if (ticket.Type == TicketType.Received)
                    _service.CreateTicketReceived(ticket);
                else
                    _service.CreateTicketReturned(ticket);

                MessageBox.Show("Movimiento guardado correctamente.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);

                // limpiar:
                Lines.Clear();
                Requisition = "";
                Origin = "";
                Reference = "";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}