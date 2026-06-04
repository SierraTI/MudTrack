using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services;
using ProjectReport.Services.Inventory;
using ProjectReport.Views.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class ChemicalListViewModel : BaseViewModel
    {
        private readonly InventoryService _inventoryService;
        private int _totalItems;
        private int _selectedCount;
        private string _searchText = string.Empty;
        private string _selectedCategory = "All Categories";
        private bool _isFilterBySelected;

        public bool IsFilterBySelected
        {
            get => _isFilterBySelected;
            set
            {
                if (SetProperty(ref _isFilterBySelected, value))
                    _chemicalItemsView.Refresh();
            }
        }

        private List<string>? _allowedProductCodes;
        public List<string>? AllowedProductCodes
        {
            get => _allowedProductCodes;
            set
            {
                if (SetProperty(ref _allowedProductCodes, value))
                    _chemicalItemsView.Refresh();
            }
        }

        private ICollectionView _chemicalItemsView;
        private ICollectionView _selectedChemicalItemsView;
        private readonly ObservableCollection<ChemicalItem> _customSelectedItems = new();
        private readonly ObservableCollection<ChemicalItem> _selectedItems = new();
        private bool _isBatchSelectionChange;

        public ObservableCollection<ChemicalItem> ChemicalItems { get; } = new ObservableCollection<ChemicalItem>();
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string> { "All Categories" };

        public ICollectionView ChemicalItemsView => _chemicalItemsView;
        public ICollectionView SelectedChemicalItemsView => _selectedChemicalItemsView;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    _chemicalItemsView.Refresh();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    _chemicalItemsView.Refresh();
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            private set => SetProperty(ref _totalItems, value);
        }

        public int SelectedCount
        {
            get => _selectedCount;
            private set => SetProperty(ref _selectedCount, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand AddCustomProductCommand { get; }

        public ChemicalListViewModel(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            
            _chemicalItemsView = CollectionViewSource.GetDefaultView(ChemicalItems);
            _chemicalItemsView.Filter = FilterChemicals;

            _selectedChemicalItemsView = new ListCollectionView(_selectedItems);
            _selectedChemicalItemsView.Filter = obj => obj is ChemicalItem item && item.IsSelected;

            SaveCommand = new RelayCommand(_ => SaveSelection());
            ClearSelectionCommand = new RelayCommand(_ => ClearSelection());
            SelectAllCommand = new RelayCommand(_ => SelectAll());
            AddCustomProductCommand = new RelayCommand(_ => AddCustomProduct());

            LoadChemicals();
        }

        private bool FilterChemicals(object obj)
        {
            if (obj is not ChemicalItem item) return false;

            // Category Filter
            bool categoryMatch = string.IsNullOrEmpty(SelectedCategory) || 
                                SelectedCategory == "All Categories" || 
                                 string.Equals(item.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase);

            if (!categoryMatch) return false;

            // Project/Report Filter
            if (IsFilterBySelected && !item.IsSelected) return false;

            // Context-specific filter (e.g., only show received products for return tickets)
            if (AllowedProductCodes != null && !AllowedProductCodes.Contains(item.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                return false;
 
            // Search Filter
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            string search = SearchText.Trim().ToLowerInvariant();
            return (item.Name?.ToLowerInvariant().Contains(search) == true) ||
                   (item.Code?.ToLowerInvariant().Contains(search) == true) ||
                   (item.Description?.ToLowerInvariant().Contains(search) == true);
        }

        private void LoadChemicals()
        {
            try
            {
                ChemicalItems.Clear();
                _customSelectedItems.Clear();
                _selectedItems.Clear();
                Categories.Clear();
                Categories.Add("All Categories");

                var products = _inventoryService.GetProducts();
                var distinctCategories = products.Select(p => p.Category)
                                                 .Where(c => !string.IsNullOrEmpty(c))
                                                 .Distinct()
                                                 .OrderBy(c => c);

                foreach (var cat in distinctCategories)
                {
                    Categories.Add(cat);
                }

                foreach (var product in products)
                {
                    var chemical = new ChemicalItem
                    {
                        Code = product.Code,
                        Name = product.Name,
                        Description = product.Description,
                        PhysicalState = product.PhysicalState,
                        Presentation = product.Presentation,
                        Quantity = product.Quantity > 0 ? product.Quantity : product.StockQty,
                        Unit = product.Unit,
                        SG = product.SG,
                        Category = product.Category,
                        UnitPrice = product.CurrentUnitCost,
                        IsSelected = product.IsSelectedForReport
                    };
                    
                    // Hook into property changed to update selected count
                    chemical.PropertyChanged += OnChemicalItemPropertyChanged;

                    ChemicalItems.Add(chemical);
                }

                UpdateTotalItems();
                UpdateSelectedCount();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading chemicals: {ex.Message}");
            }
        }

        private void AddCustomProduct()
        {
            var dialog = new AddCustomProductDialog
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                var newItem = dialog.Result;

                // Check if a product with this code already exists
                var existing = ChemicalItems.FirstOrDefault(c =>
                    string.Equals(c.Code, newItem.Code, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Just select it if it already exists
                    existing.IsSelected = true;
                    return;
                }

                var existingCustom = _customSelectedItems.FirstOrDefault(c =>
                    string.Equals(c.Code, newItem.Code, StringComparison.OrdinalIgnoreCase));

                if (existingCustom != null)
                {
                    existingCustom.IsSelected = true;
                    UpdateSelectedCount();
                    return;
                }

                // Custom products are variable/session items:
                // keep them only on the Selected Products side, not in Available Products.
                newItem.IsSelected = true;
                newItem.PropertyChanged += OnChemicalItemPropertyChanged;
                _customSelectedItems.Add(newItem);

                UpdateSelectedCount();
            }
        }

        public void ClearSelection()
        {
            _isBatchSelectionChange = true;
            foreach (var item in ChemicalItems)
                item.IsSelected = false;
            foreach (var item in _customSelectedItems)
                item.IsSelected = false;
            _isBatchSelectionChange = false;
            UpdateSelectedCount();
            PersistAndPublishSelection();
        }

        public void SelectAll()
        {
            // Only select visible items? Usually better for user experience
            _isBatchSelectionChange = true;
            foreach (var item in ChemicalItemsView.Cast<ChemicalItem>())
                item.IsSelected = true;
            _isBatchSelectionChange = false;
            UpdateSelectedCount();
            PersistAndPublishSelection();
        }

        private void SaveSelection()
        {
            PersistAndPublishSelection();
        }

        private void UpdateTotalItems()
        {
            TotalItems = ChemicalItems.Count;
        }

        private void UpdateSelectedCount()
        {
            var selectedStatic = ChemicalItems.Where(c => c.IsSelected).ToList();
            var selectedCustom = _customSelectedItems.Where(c => c.IsSelected).ToList();

            _selectedItems.Clear();
            foreach (var item in selectedStatic)
                _selectedItems.Add(item);
            foreach (var item in selectedCustom)
                _selectedItems.Add(item);

            SelectedCount = _selectedItems.Count;
            _selectedChemicalItemsView?.Refresh();
        }

        private void OnChemicalItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChemicalItem.IsSelected))
            {
                UpdateSelectedCount();
                if (!_isBatchSelectionChange)
                {
                    PersistAndPublishSelection();
                }
            }
        }

        private void PersistAndPublishSelection()
        {
            var selectedChemicals = ChemicalItems
                .Where(c => c.IsSelected)
                .Concat(_customSelectedItems.Where(c => c.IsSelected))
                .ToList();
            System.Diagnostics.Debug.WriteLine($"Saved {selectedChemicals.Count} selected chemicals");

            // Persist selection only for static catalog products
            var staticCodes = ChemicalItems.Select(c => c.Code ?? string.Empty)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var codes = selectedChemicals
                .Select(c => c.Code ?? string.Empty)
                .Where(c => !string.IsNullOrWhiteSpace(c) && staticCodes.Contains(c))
                .ToList();
            _inventoryService.UpdateProductSelection(codes);

            // Persist unit price set in Selected Products for static catalog items.
            var unitCostsByCode = ChemicalItems
                .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                .ToDictionary(c => c.Code!, c => c.UnitPrice, StringComparer.OrdinalIgnoreCase);
            _inventoryService.UpdateProductUnitCosts(unitCostsByCode);

            // Broadcast selection to other modules (like Volume Balance)
            WellContextService.Instance.PublishChemicalSelection(selectedChemicals);
        }
    }
}

