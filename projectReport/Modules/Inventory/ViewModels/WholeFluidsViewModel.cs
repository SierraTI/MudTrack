using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using System.Collections.Specialized;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.Services;

namespace ProjectReport.ViewModels.Inventory
{
    public class WholeFluidsViewModel : INotifyPropertyChanged
    {
        private readonly string _dataFile;
        private readonly InventoryService _inventoryService;

        public ObservableCollection<WholeFluidItem> WholeFluids { get; } = new ObservableCollection<WholeFluidItem>();
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand RemoveCommand { get; }

        private string _error = "";
        public string Error
        {
            get => _error;
            set { if (_error != value) { _error = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error))); } }
        }

        private double _dailyTotalCost;
        public double DailyTotalCost
        {
            get => _dailyTotalCost;
            private set
            {
                if (Math.Abs(_dailyTotalCost - value) > 0.0001)
                {
                    _dailyTotalCost = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DailyTotalCost)));
                }
            }
        }

        public WholeFluidsViewModel()
        {
            _inventoryService = ServiceLocator.InventoryService;
            _dataFile = Path.Combine(AppContext.BaseDirectory, "Data", "wholefluids.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_dataFile) ?? AppContext.BaseDirectory);

            AddCommand = new RelayCommand(_ => Add());
            SaveCommand = new RelayCommand(_ => Save());
            RemoveCommand = new RelayCommand(param => Remove(param as WholeFluidItem));

            // subscribe collection changes to recalc totals and item handlers
            WholeFluids.CollectionChanged += WholeFluids_CollectionChanged;

            LoadProducts();
            LoadFromFile();

            // attach handlers for any preloaded items and calculate initial total
            foreach (var it in WholeFluids) AttachItemHandler(it);
            RecalcDailyTotalCost();
        }

        private void WholeFluids_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (WholeFluidItem oldItem in e.OldItems)
                    DetachItemHandler(oldItem);
            }

            if (e.NewItems != null)
            {
                foreach (WholeFluidItem newItem in e.NewItems)
                    AttachItemHandler(newItem);
            }

            RecalcDailyTotalCost();
        }

        private void AttachItemHandler(WholeFluidItem item)
        {
            if (item != null)
                item.PropertyChanged += WholeFluidItem_PropertyChanged;
        }

        private void DetachItemHandler(WholeFluidItem item)
        {
            if (item != null)
                item.PropertyChanged -= WholeFluidItem_PropertyChanged;
        }

        private void WholeFluidItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // cualquier cambio relevante (Quantity o UnitPrice o MovementType) recalcula
            if (e.PropertyName == nameof(WholeFluidItem.Quantity) ||
                e.PropertyName == nameof(WholeFluidItem.UnitPrice) ||
                e.PropertyName == nameof(WholeFluidItem.MovementType))
            {
                RecalcDailyTotalCost();
            }
        }

        private void RecalcDailyTotalCost()
        {
            // Cost = sum(quantity * unitPrice) para todas las líneas
            // (si quieres excluir "Ingreso" o "Salida" puedes ajustar aquí)
            try
            {
                var total = WholeFluids.Sum(i => (i.Quantity) * (i.UnitPrice));
                DailyTotalCost = Math.Round(total, 2);
            }
            catch
            {
                DailyTotalCost = 0;
            }
        }

        private void LoadProducts()
        {
            Products.Clear();

            try
            {
                // Buscar fichero de lista de fluidos en Data (prioridad a "listaFluidos.xlsx")
                var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
                var listaPath = Path.Combine(dataDir, "listaFluidos.xlsx");
                if (!File.Exists(listaPath))
                {
                    // fallback a nombres comunes
                    var alt = Path.Combine(dataDir, "Lista.xlsx");
                    if (File.Exists(alt)) listaPath = alt;
                }

                if (File.Exists(listaPath))
                {
                    // Usar el importador existente que ya parsea excel a UniversalProduct
                    var importer = new ProjectReport.Services.Inventory.InventoryExcelImportService();
                    var uni = importer.LoadUniversalProducts(listaPath);

                    // Mapear a Product para que el ComboBox (DisplayMemberPath="Name", SelectedValuePath="Code") funcione
                    foreach (var u in uni)
                    {
                        var code = string.IsNullOrWhiteSpace(u.Codigo) ? (u.Nombre ?? Guid.NewGuid().ToString()) : u.Codigo;
                        var name = string.IsNullOrWhiteSpace(u.Nombre) ? code : u.Nombre;
                        Products.Add(new Product
                        {
                            Code = code,
                            Name = name,
                            Description = string.IsNullOrWhiteSpace(u.Categoria) ? string.Empty : u.Categoria,
                            Category = string.IsNullOrWhiteSpace(u.Categoria) ? string.Empty : u.Categoria,
                            Unit = string.IsNullOrWhiteSpace(u.Unidad) ? "Each" : u.Unidad,
                            StockQty = 0,
                            CurrentUnitCost = 0,
                            Status = ProductStatus.Active
                        });
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                // No bloquear: mostrar error y seguir con repositorio si hay fallo de lectura
                Error = "Error cargando lista de fluidos desde Excel: " + ex.Message;
            }

            // Fallback: cargar productos desde InventoryService (comportamiento anterior)
            var list = _inventoryService.GetProducts();
            foreach (var p in list) Products.Add(p);
        }

        public void Add()
        {
            WholeFluids.Add(new WholeFluidItem
            {
                Requisition = string.Empty,
                MovementType = "Ingreso",
                ProductCode = string.Empty,
                ProductName = string.Empty,
                Quantity = 1,
                UnitPrice = 0,
                Context = string.Empty,
                Date = DateTime.Now,
                Observations = string.Empty
            });

            Error = $"Línea agregada en borrador. Total líneas: {WholeFluids.Count}";
            RecalcDailyTotalCost();
        }

        public void Remove(WholeFluidItem? item)
        {
            if (item == null) return;
            WholeFluids.Remove(item);
            RecalcDailyTotalCost();
        }

        public void Save()
        {
            try
            {
                var list = WholeFluids.ToList();
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFile, json);
                Error = "Whole Fluids guardado correctamente.";
            }
            catch (Exception ex)
            {
                Error = "Error guardando: " + ex.Message;
            }
        }

        private void LoadFromFile()
        {
            try
            {
                if (!File.Exists(_dataFile)) return;
                var json = File.ReadAllText(_dataFile);
                var list = JsonSerializer.Deserialize<WholeFluidItem[]>(json);
                if (list == null) return;
                WholeFluids.Clear();
                foreach (var i in list) WholeFluids.Add(i);
                RecalcDailyTotalCost();
            }
            catch (Exception ex)
            {
                Error = "Error cargando datos: " + ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}