using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.Services;

namespace ProjectReport.ViewModels.Inventory
{
    public class InventoryProductsDashboardViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        // NAV EVENTS
        public event Action? RequestOpenReceived;
        public event Action? RequestOpenReturned;
        public event Action? RequestOpenHistory;

        // COMMANDS
        public RelayCommand OpenTicketReceivedCommand { get; }
        public RelayCommand OpenTicketReturnedCommand { get; }
        public RelayCommand OpenHistoryCommand { get; }
        public RelayCommand RefreshCommand { get; }

        // Delete command per ticket
        public RelayCommand DeleteRowCommand { get; }

        // TABLE DATA
        public ObservableCollection<ProductSummaryRow> Rows { get; } = new();

        private ProductSummaryRow? _selectedRow;
        public ProductSummaryRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (SetProperty(ref _selectedRow, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                    LoadForDate(_selectedDate);
            }
        }

        public double TotalProductsCost => Rows.Sum(r => r.DailyCost);

        public InventoryProductsDashboardViewModel(InventoryService service)
        {
            _service = service;

            OpenTicketReceivedCommand = new RelayCommand(_ => RequestOpenReceived?.Invoke());
            OpenTicketReturnedCommand = new RelayCommand(_ => RequestOpenReturned?.Invoke());

            OpenHistoryCommand = new RelayCommand(_ => RequestOpenHistory?.Invoke());
            RefreshCommand = new RelayCommand(_ => LoadForDate(SelectedDate));

            DeleteRowCommand = new RelayCommand(param =>
            {
                if (param is string ticketId && !string.IsNullOrWhiteSpace(ticketId))
                {
                    var confirm = System.Windows.MessageBox.Show("¿Eliminar este ticket y sus movimientos? Esta acción no se puede deshacer.", "Confirmar eliminación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    if (confirm == System.Windows.MessageBoxResult.Yes)
                    {
                        _service.DeleteMovementsForTicket(ticketId);
                        LoadForDate(SelectedDate);
                    }
                }
            });

            // Subscribe to inventory updates to refresh dashboard in real time
            _service.InventoryUpdated += () =>
            {
                // Ensure update runs on UI thread
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() => LoadForDate(SelectedDate));
                }
                else
                {
                    LoadForDate(SelectedDate);
                }
            };

            LoadForDate(SelectedDate);
        }

        public InventoryProductsDashboardViewModel() : this(ServiceLocator.InventoryService) { }

        public void LoadForDate(DateTime date)
        {
            Rows.Clear();
            SelectedRow = null;

            var products = _service.GetProducts();
            var movements = _service.GetMovements()
                .Where(m => m.Date.Date == date.Date)
                .OrderBy(m => m.Date)
                .ToList();

            // net change por producto (case-insensitive)
            var netByProduct = movements
                .GroupBy(m => (m.ProductCode ?? "").ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => (x.Type == TicketType.Received || x.Type == TicketType.Returned) ? x.Quantity : 0.0));

            // Agrupar por ProductCode normalizado + TicketId (clave anónima con ProductCode en mayúscula)
            var groups = movements.GroupBy(m => new { ProductCode = (m.ProductCode ?? "").ToUpperInvariant(), TicketId = m.TicketId });

            foreach (var g in groups.OrderBy(g => g.Key.ProductCode))
            {
                // Buscar producto por código (case-insensitive)
                var prod = products.FirstOrDefault(p => string.Equals(p.Code, g.Key.ProductCode, StringComparison.OrdinalIgnoreCase));
                var productName = prod?.Name ?? g.Key.ProductCode;
                var productUnit = prod?.Unit ?? "";

                double received = g.Where(x => x.Type == TicketType.Received).Sum(x => x.Quantity);
                double returned = g.Where(x => x.Type == TicketType.Returned).Sum(x => x.Quantity);
                double used = g.Where(x => x.Type != TicketType.Received && x.Type != TicketType.Returned).Sum(x => x.Quantity);

                double netChangeToday = 0;
                netByProduct.TryGetValue(g.Key.ProductCode, out netChangeToday);
                double initialQty = (prod?.StockQty ?? 0) - netChangeToday;

                var requisition = g.Select(x => x.Requisition).FirstOrDefault() ?? "";
                var ticketId = g.Key.TicketId ?? "";

                Rows.Add(new ProductSummaryRow
                {
                    ProductCode = g.Key.ProductCode,
                    ProductName = productName,
                    Unit = productUnit,
                    InitialQty = initialQty,
                    Received = received,
                    Used = used,
                    Returned = returned,
                    RemainingStock = prod?.StockQty ?? 0,
                    UnitCostAvg = 0,
                    DailyCost = 0,
                    TicketId = ticketId,
                    Requisition = requisition
                });
            }

            OnPropertyChanged(nameof(TotalProductsCost));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
