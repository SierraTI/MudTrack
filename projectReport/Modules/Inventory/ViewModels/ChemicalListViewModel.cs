using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services;
using ProjectReport.Services.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class ChemicalListViewModel : INotifyPropertyChanged
    {
        private readonly InventoryService _inventoryService;
        private readonly RelayCommand _saveCommand;
        private readonly RelayCommand _clearSelectionCommand;
        private readonly RelayCommand _selectAllCommand;
        private int _totalItems;
        private int _selectedCount;

        public ObservableCollection<ChemicalItem> ChemicalItems { get; } = new ObservableCollection<ChemicalItem>();

        public int TotalItems
        {
            get => _totalItems;
            private set
            {
                if (_totalItems != value)
                {
                    _totalItems = value;
                    OnPropertyChanged(nameof(TotalItems));
                }
            }
        }

        public int SelectedCount
        {
            get => _selectedCount;
            private set
            {
                if (_selectedCount != value)
                {
                    _selectedCount = value;
                    OnPropertyChanged(nameof(SelectedCount));
                }
            }
        }

        public ICommand SaveCommand => _saveCommand;
        public ICommand ClearSelectionCommand => _clearSelectionCommand;
        public ICommand SelectAllCommand => _selectAllCommand;

        public ChemicalListViewModel(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            _saveCommand = new RelayCommand(_ => SaveSelection());
            _clearSelectionCommand = new RelayCommand(_ => ClearSelection());
            _selectAllCommand = new RelayCommand(_ => SelectAll());
            LoadChemicals();
        }

        private void LoadChemicals()
        {
            try
            {
                ChemicalItems.Clear();
                var products = _inventoryService.GetProducts();

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
                        IsSelected = false
                    };
                    ChemicalItems.Add(chemical);
                }

                UpdateTotalItems();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading chemicals: {ex.Message}");
            }
        }

        public void ClearSelection()
        {
            foreach (var item in ChemicalItems)
            {
                item.IsSelected = false;
            }
            UpdateSelectedCount();
        }

        public void SelectAll()
        {
            foreach (var item in ChemicalItems)
            {
                item.IsSelected = true;
            }
            UpdateSelectedCount();
        }

        private void SaveSelection()
        {
            var selectedChemicals = ChemicalItems.Where(c => c.IsSelected).ToList();
            System.Diagnostics.Debug.WriteLine($"Saved {selectedChemicals.Count} selected chemicals");
            // Store selected items globally or emit event for other screens to consume
            OnPropertyChanged(nameof(ChemicalItems));
        }

        private void UpdateTotalItems()
        {
            TotalItems = ChemicalItems.Count;
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = ChemicalItems.Count(c => c.IsSelected);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // Update selected count when items change selection
            if (propertyName != nameof(SelectedCount))
            {
                UpdateSelectedCount();
            }
        }
    }
}
