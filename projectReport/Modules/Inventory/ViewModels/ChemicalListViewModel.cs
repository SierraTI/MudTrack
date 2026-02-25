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

        private ICollectionView _chemicalItemsView;

        public ObservableCollection<ChemicalItem> ChemicalItems { get; } = new ObservableCollection<ChemicalItem>();
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string> { "All Categories" };

        public ICollectionView ChemicalItemsView => _chemicalItemsView;

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

        public ChemicalListViewModel(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            
            _chemicalItemsView = CollectionViewSource.GetDefaultView(ChemicalItems);
            _chemicalItemsView.Filter = FilterChemicals;

            SaveCommand = new RelayCommand(_ => SaveSelection());
            ClearSelectionCommand = new RelayCommand(_ => ClearSelection());
            SelectAllCommand = new RelayCommand(_ => SelectAll());

            LoadChemicals();
        }

        private bool FilterChemicals(object obj)
        {
            if (obj is not ChemicalItem item) return false;

            // Category Filter
            bool categoryMatch = string.IsNullOrEmpty(SelectedCategory) || 
                                SelectedCategory == "All Categories" || 
                                 string.Equals(item.Categoria, SelectedCategory, StringComparison.OrdinalIgnoreCase);

            if (!categoryMatch) return false;

            // Project/Report Filter
            if (IsFilterBySelected && !item.IsSelected) return false;

            // Search Filter
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            string search = SearchText.Trim().ToLowerInvariant();
            return (item.Nombre?.ToLowerInvariant().Contains(search) == true) ||
                   (item.Code?.ToLowerInvariant().Contains(search) == true) ||
                   (item.Descripcion?.ToLowerInvariant().Contains(search) == true);
        }

        private void LoadChemicals()
        {
            try
            {
                ChemicalItems.Clear();
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
                        Nombre = product.Name,
                        Descripcion = product.Description,
                        EstadoFisico = product.PhysicalState,
                        Presentacion = product.Presentation,
                        Cantidad = product.Quantity > 0 ? product.Quantity : product.StockQty,
                        Unidad = product.Unit,
                        SG = product.SG,
                        Categoria = product.Category,
                        IsSelected = product.IsSelectedForReport
                    };
                    
                    // Hook into property changed to update selected count
                    chemical.PropertyChanged += (s, e) => {
                        if (e.PropertyName == nameof(ChemicalItem.IsSelected))
                            UpdateSelectedCount();
                    };

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

        public void ClearSelection()
        {
            foreach (var item in ChemicalItems)
                item.IsSelected = false;
        }

        public void SelectAll()
        {
            // Only select visible items? Usually better for user experience
            foreach (var item in ChemicalItemsView.Cast<ChemicalItem>())
                item.IsSelected = true;
        }

        private void SaveSelection()
        {
            var selectedChemicals = ChemicalItems.Where(c => c.IsSelected).ToList();
            System.Diagnostics.Debug.WriteLine($"Saved {selectedChemicals.Count} selected chemicals");
            
            // Persist to service
            var codes = selectedChemicals.Select(c => c.Code ?? string.Empty).Where(c => !string.IsNullOrEmpty(c)).ToList();
            _inventoryService.UpdateProductSelection(codes);

            // Broadcast selection to other modules (like Volume Balance)
            WellContextService.Instance.PublishChemicalSelection(selectedChemicals);
        }

        private void UpdateTotalItems()
        {
            TotalItems = ChemicalItems.Count;
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = ChemicalItems.Count(c => c.IsSelected);
        }
    }
}
