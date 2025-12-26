using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class InventoryProductsDashboardViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        // ======= NAV EVENTS (MainWindow listens) =======
        public event Action? RequestOpenReceived;
        public event Action? RequestOpenReturned;
        public event Action? RequestOpenHistory;

        // ======= COMMANDS (buttons in dashboard) =======
        public RelayCommand OpenTicketReceivedCommand { get; }
        public RelayCommand OpenTicketReturnedCommand { get; }
        public RelayCommand OpenHistoryCommand { get; }
        public RelayCommand RefreshCommand { get; }

        // ======= TABLE DATA =======
        public ObservableCollection<ProductSummaryRow> Rows { get; } = new();

        private ProductSummaryRow? _selectedRow;
        public ProductSummaryRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (SetProperty(ref _selectedRow, value))
                {
                    // Hace que WPF re-evalúe CanExecute de los comandos
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

        public void LoadForDate(DateTime date)
        {
            Rows.Clear();
            SelectedRow = null;

            var products = _service.GetProducts();
            var movements = _service.GetMovements()
                .Where(m => m.Date.Date == date.Date)
                .OrderBy(m => m.Date)
                .ToList();

            // Build a lookup of movements by product code for the selected date
            var byProduct = movements.GroupBy(m => m.ProductCode)
                                     .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var p in products.OrderBy(pp => pp.Name))
            {
                byProduct.TryGetValue(p.Code, out var list);
                list ??= new System.Collections.Generic.List<InventoryMovement>();

                double received = list.Where(x => x.Type == TicketType.Received).Sum(x => x.Quantity);
                double used = 0; // consumed removed
                double returned = list.Where(x => x.Type == TicketType.Returned).Sum(x => x.Quantity);

                double dailyCost = 0; // no consumed cost

                double unitAvg = 0; // no consumed data

                // Assume current product.StockQty reflects stock AFTER today's movements.
                // Compute initial as current stock minus today's net change (received - used + returned)
                double netChangeToday = received - used + returned;
                double initialQty = p.StockQty - netChangeToday;

                Rows.Add(new ProductSummaryRow
                {
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    Unit = p.Unit ?? "",
                    InitialQty = initialQty,
                    Received = received,
                    Used = used,
                    Returned = returned,
                    RemainingStock = p.StockQty,
                    UnitCostAvg = unitAvg,
                    DailyCost = dailyCost
                });
            }

            OnPropertyChanged(nameof(TotalProductsCost));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
