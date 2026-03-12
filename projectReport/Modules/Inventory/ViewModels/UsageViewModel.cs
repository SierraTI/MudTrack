using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.Services;

namespace ProjectReport.ViewModels.Inventory
{
    public class UsageViewModel : INotifyPropertyChanged
    {
        private readonly InventoryService _service;
        private readonly RelayCommand _saveCommand;
        private double _totalProductsCost;

        public ObservableCollection<UsageBalanceItem> BalanceItems { get; } = new();

        public Action<UsageBalanceItem>? RequestUsageSpecification;

        public double TotalProductsCost
        {
            get => _totalProductsCost;
            private set
            {
                if (Math.Abs(_totalProductsCost - value) > 0.0001)
                {
                    _totalProductsCost = value;
                    OnPropertyChanged(nameof(TotalProductsCost));
                }
            }
        }

        public ICommand SaveCommand => _saveCommand;
        public ICommand OpenUseSpecificationCommand { get; }

        public UsageViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _saveCommand = new RelayCommand(_ => SaveUsage());
            OpenUseSpecificationCommand = new RelayCommand(param => OpenUseSpecification(param as UsageBalanceItem));

            _service.InventoryUpdated += OnInventoryUpdated;

            LoadUsage();
        }

        private void OnInventoryUpdated()
        {
            LoadUsage();
        }

        private void LoadUsage()
        {
            BalanceItems.Clear();

            var allProducts = _service.GetProducts();
            var movements = _service.GetMovements();
            
            // Assume report date is today for this view instance
            var reportDate = DateTime.Today;

            // Products to show: those explicitly selected OR those with movements today
            var productsToShow = allProducts.Where(p => 
                p.IsSelectedForReport || 
                movements.Any(m => string.Equals(m.ProductCode, p.Code, StringComparison.OrdinalIgnoreCase) && m.Date.Date == reportDate)
            ).ToList();

            foreach (var product in productsToShow)
            {
                var prodMovements = movements.Where(m => string.Equals(m.ProductCode, product.Code, StringComparison.OrdinalIgnoreCase)).ToList();
                
                // Movements before today
                var historical = prodMovements.Where(m => m.Date.Date < reportDate).ToList();
                double initial = historical.Where(m => m.Type == TicketType.Received).Sum(m => m.Quantity)
                                - historical.Where(m => m.Type == TicketType.Returned).Sum(m => m.Quantity)
                                - historical.Where(m => m.Type == TicketType.Consumed).Sum(m => m.Quantity);

                // Movements today
                var today = prodMovements.Where(m => m.Date.Date == reportDate).ToList();
                double received = today.Where(m => m.Type == TicketType.Received).Sum(m => m.Quantity);
                double returned = today.Where(m => m.Type == TicketType.Returned).Sum(m => m.Quantity);
                double used = today.Where(m => m.Type == TicketType.Consumed).Sum(m => m.Quantity);

                BalanceItems.Add(new UsageBalanceItem
                {
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    Unit = product.Unit ?? "Units",
                    InitialQuantity = initial,
                    ReceivedQuantity = received,
                    ReturnQuantity = returned,
                    TotalUsedQuantity = used,
                    UnitCost = product.CurrentUnitCost
                });
            }

            UpdateTotalCost();
        }

        private void OpenUseSpecification(UsageBalanceItem? item)
        {
            if (item == null) return;
            RequestUsageSpecification?.Invoke(item);
        }

        private void SaveUsage()
        {
            LoadUsage();
            UpdateTotalCost();
        }

        private void UpdateTotalCost()
        {
            TotalProductsCost = BalanceItems.Sum(b => b.DailyCost);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
