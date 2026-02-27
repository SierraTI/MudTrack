using System;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Inventory
{
    public class TicketHistoryItem
    {
        public string TicketId    { get; set; } = "";
        public string Requisition { get; set; } = "";
        public DateTime Date      { get; set; }
        public string Type        { get; set; } = "";
        public string Origin      { get; set; } = "";
        public string User        { get; set; } = "";
        public int    LineCount   { get; set; }
        public double TotalValue  { get; set; }
        public string SupplierName  { get; set; } = "";
        public string Observations  { get; set; } = "";
        public string Remision      { get; set; } = "";

        // Formatted helpers for the UI
        public string TicketLabel   => string.IsNullOrWhiteSpace(Requisition) ? "(no #)" : $"#{Requisition}";
        public string TotalValueFmt => TotalValue > 0 ? $"${TotalValue:N2}" : "â€”";
    }

    public class TicketLineItem
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double Quantity    { get; set; }
        public string Unit        { get; set; } = "";
        public double UnitPrice   { get; set; }
        public double Total       => Quantity * UnitPrice;
        public string TotalFmt    => Total > 0 ? $"${Total:N2}" : "â€”";
        public string UnitPriceFmt => UnitPrice > 0 ? $"${UnitPrice:N2}" : "â€”";
        public string OriginOrUse { get; set; } = "";
    }

    public class InventoryHistoryViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<TicketHistoryItem> Tickets { get; } = new();
        public ObservableCollection<TicketLineItem>    TicketLines { get; } = new();

        // â”€â”€â”€ Selected ticket â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private TicketHistoryItem? _selectedTicket;
        public TicketHistoryItem? SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                if (SetProperty(ref _selectedTicket, value))
                    LoadLines(value);
            }
        }

        public bool HasSelection => _selectedTicket != null;

        // â”€â”€â”€ Commands â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public RelayCommand RefreshCommand { get; }
        public RelayCommand ClearOldCommand { get; }

        public InventoryHistoryViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            RefreshCommand = new RelayCommand(_ => LoadMovements());
            ClearOldCommand = new RelayCommand(_ => ClearOldTickets());

            _service.InventoryUpdated += OnInventoryUpdated;
            LoadMovements();
        }

        private void OnInventoryUpdated() => LoadMovements();

        // â”€â”€â”€ Load tickets (one per TicketId) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void LoadMovements()
        {
            var previousId = _selectedTicket?.TicketId;

            Tickets.Clear();
            TicketLines.Clear();

            var movements = _service.GetMovements();

            var grouped = movements
                .GroupBy(m => m.TicketId)
                .Select(g =>
                {
                    var items = g.OrderBy(m => m.Date).ToList();
                    var first = items.First();
                    return new TicketHistoryItem
                    {
                        TicketId     = g.Key,
                        Requisition  = first.Requisition,
                        Date         = first.Date,
                        Type         = first.Type.ToString(),
                        Origin       = first.OriginOrUse,
                        User         = first.User,
                        LineCount    = items.Count,
                        TotalValue   = items.Sum(m => m.Quantity * m.UnitPrice),
                        SupplierName = first.SupplierName,
                        Observations = first.Observations,
                        Remision     = first.Remision
                    };
                })
                .OrderByDescending(t => t.Date)
                .ToList();

            foreach (var t in grouped)
                Tickets.Add(t);

            // Re-select previously selected ticket if it still exists
            if (previousId != null)
            {
                var toReselect = Tickets.FirstOrDefault(t => t.TicketId == previousId);
                if (toReselect != null)
                    SelectedTicket = toReselect;
            }
        }

        // â”€â”€â”€ Load lines for the selected ticket â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void LoadLines(TicketHistoryItem? ticket)
        {
            TicketLines.Clear();
            OnPropertyChanged(nameof(HasSelection));

            if (ticket == null) return;

            var movements = _service.GetMovements()
                .Where(m => string.Equals(m.TicketId, ticket.TicketId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.ProductName)
                .ToList();

            foreach (var m in movements)
            {
                TicketLines.Add(new TicketLineItem
                {
                    ProductCode = m.ProductCode,
                    ProductName = m.ProductName,
                    Quantity    = m.Quantity,
                    UnitPrice   = m.UnitPrice,
                    OriginOrUse = m.OriginOrUse
                });
            }
        }

        // â”€â”€â”€ Clear tickets older than 30 days â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void ClearOldTickets()
        {
            var allMovements = _service.GetMovements().ToList();
            if (allMovements.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[InventoryHistory] No tickets to clear");
                return;
            }

            var ticketIds = allMovements.Select(m => m.TicketId).Distinct().ToList();

            foreach (var ticketId in ticketIds)
            {
                _service.DeleteMovementsForTicket(ticketId, removeLinkedByRequisition: false);
            }

            System.Diagnostics.Debug.WriteLine($"[InventoryHistory] Cleared {ticketIds.Count} tickets");
            LoadMovements();
        }

        public void Dispose()
        {
            try { _service.InventoryUpdated -= OnInventoryUpdated; } catch { }
        }
    }
}

