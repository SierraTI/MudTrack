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

        public ObservableCollection<Product> AvailableProducts { get; } = new();

        public ObservableCollection<ConsumoItem> ConsumoItems { get; } = new ObservableCollection<ConsumoItem>();

        /// <summary>
        /// Grouped by category for UI display (Weighting Materials, Viscosifiers, Thinners, etc.)
        /// </summary>
        public ObservableCollection<ConsumoItemGroup> GroupedConsumos { get; } = new ObservableCollection<ConsumoItemGroup>();

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

        public UsageViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _saveCommand = new RelayCommand(_ => SaveUsage());

            LoadProducts();
            LoadUsage();
        }

        private void LoadProducts()
        {
            AvailableProducts.Clear();
            var selected = _service.GetSelectedProducts();
            foreach (var p in selected)
            {
                AvailableProducts.Add(p);
            }
        }

        /// <summary>
        /// Add a new usage entry for a specific product
        /// </summary>
        public void AddNewUsageItem(string productCode = "", string productName = "", string category = "", double currentStock = 0)
        {
            var newUsageItem = new ConsumoItem
            {
                ProductCode = productCode,
                ProductName = productName ?? "New Item",
                Category = category ?? "General",
                CurrentStock = currentStock,
                Quantity = 0,
                Unit = "Units",
                Concentration = 0,
                UnitCost = 0,
                Date = DateTime.Now,
                Notes = ""
            };
            ConsumoItems.Add(newUsageItem);
            GroupByCategory();
            UpdateTotalCost();
        }

        /// <summary>
        /// Group consumos by category for better UI scannability
        /// </summary>
        private void GroupByCategory()
        {
            GroupedConsumos.Clear();
            var grouped = ConsumoItems
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var groupItem = new ConsumoItemGroup { Category = group.Key };
                foreach (var item in group)
                {
                    groupItem.Items.Add(item);
                }
                GroupedConsumos.Add(groupItem);
            }
        }

        /// <summary>
        /// Validate all entries before saving (stock availability, etc.)
        /// </summary>
        private bool ValidateUsage()
        {
            foreach (var item in ConsumoItems)
            {
                if (item.IsInsufficientStock)
                {
                    item.ValidationError = $"Error: Insufficient stock (Only {item.CurrentStock} {item.Unit} available)";
                    return false;
                }
                item.ValidationError = "";
            }
            return true;
        }

        private void LoadUsage()
        {
            // Load existing usage items from service or database
            // For now, this can be expanded with actual persistence logic
            UpdateTotalCost();
        }

        private void SaveUsage()
        {
            if (!ValidateUsage())
            {
                // Show validation errors to user
                OnPropertyChanged(nameof(ConsumoItems));
                return;
            }

            // Implement save logic to persist consumos
            // This should trigger the Stock table to recalculate the "Used" column
            OnPropertyChanged(nameof(ConsumoItems));
            UpdateTotalCost();
        }

        private void UpdateTotalCost()
        {
            TotalProductsCost = ConsumoItems.Sum(c => c.DailyCost);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a grouped collection of consumo items by category
    /// </summary>
    public class ConsumoItemGroup
    {
        public string Category { get; set; } = "";
        public ObservableCollection<ConsumoItem> Items { get; } = new ObservableCollection<ConsumoItem>();
    }
}
