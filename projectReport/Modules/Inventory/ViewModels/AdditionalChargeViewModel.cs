using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using System.Collections.Specialized;
using System.Timers;
using ProjectReport.Models.Inventory;
using ProjectReport.Services; // ServiceLocator
using ProjectReport.Services.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class AdditionalChargeViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly string _dataFile;
        private readonly string _wholeFluidsFile;
        private readonly InventoryService? _inventoryService;

        private FileSystemWatcher? _watcherAdditional;
        private FileSystemWatcher? _watcherWhole;
        private readonly System.Timers.Timer _debounceTimer;

        // handler almacenado para poder desuscribir correctamente
        private Action? _inventoryUpdatedHandler;

        public ObservableCollection<AdditionalChargeItem> Charges { get; } = new ObservableCollection<AdditionalChargeItem>();
        public ObservableCollection<string> DefaultChargeNames { get; } = new ObservableCollection<string>();

        // Opciones visibles en la lista desplegable por fila
        public ObservableCollection<string> CurrencyOptions { get; } = new ObservableCollection<string> { "USD", "COP" };
        // Opciones para la Unit (lista desplegable Unit)
        public ObservableCollection<string> UnitOptions { get; } = new ObservableCollection<string> { "Each", "Day" };

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

        // Nuevo: total combinado (Products + WholeFluids + AdditionalCharge)
        private double _combinedDailyTotal;
        public double CombinedDailyTotal
        {
            get => _combinedDailyTotal;
            private set
            {
                if (Math.Abs(_combinedDailyTotal - value) > 0.0001)
                {
                    _combinedDailyTotal = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CombinedDailyTotal)));
                }
            }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ReloadCommand { get; }

        public AdditionalChargeViewModel()
        {
            Debug.WriteLine("[AdditionalChargeViewModel] ctor start");
            _dataFile = Path.Combine(AppContext.BaseDirectory, "Data", "additional_charges.json");
            _wholeFluidsFile = Path.Combine(AppContext.BaseDirectory, "Data", "wholefluids.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_dataFile) ?? AppContext.BaseDirectory);

            // intentar usar InventoryService compartido (si existe)
            try { _inventoryService = ServiceLocator.InventoryService; } catch { _inventoryService = null; }

            AddCommand = new RelayCommand(_ => Add());
            RemoveCommand = new RelayCommand(param => Remove(param as AdditionalChargeItem));
            SaveCommand = new RelayCommand(_ => Save());
            ReloadCommand = new RelayCommand(_ => LoadFromFile());

            // reaccionar a cambios en la colección para recalcular total
            Charges.CollectionChanged += Charges_CollectionChanged;

            SeedDefaults();
            LoadFromFile();

            // DEBUG: añadir fila de ejemplo si no hay cargas (para verificar UI)
            if (Charges.Count == 0)
            {
                Charges.Add(new AdditionalChargeItem
                {
                    Name = DefaultChargeNames.FirstOrDefault() ?? "TRANSPORTE",
                    Unit = UnitOptions.FirstOrDefault() ?? "Each",
                    Quantity = 1,
                    UnitPrice = 0,
                    Currency = CurrencyOptions.FirstOrDefault() ?? "USD"
                });
            }

            // attach handlers y calcular total inicial
            foreach (var c in Charges) AttachItemHandler(c);
            RecalcDailyTotalCost();

            // suscribirse a InventoryUpdated para recalc producto cuando cambie el inventario
            if (_inventoryService != null)
            {
                // almacenar el handler para poder quitarlo luego
                _inventoryUpdatedHandler = () => RecalcCombinedTotal();
                _inventoryService.InventoryUpdated += _inventoryUpdatedHandler;
            }

            // File watchers para detectar cambios hechos por otras vistas (WholeFluids, AdditionalCharge saves)
            _debounceTimer = new System.Timers.Timer(400) { AutoReset = false };
            _debounceTimer.Elapsed += (s, e) => RecalcCombinedTotal();

            try
            {
                _watcherWhole = new FileSystemWatcher(Path.GetDirectoryName(_wholeFluidsFile) ?? AppContext.BaseDirectory)
                {
                    Filter = Path.GetFileName(_wholeFluidsFile),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size
                };
                _watcherWhole.Changed += FileChangedHandler;
                _watcherWhole.Created += FileChangedHandler;
                _watcherWhole.Deleted += FileChangedHandler;
                _watcherWhole.EnableRaisingEvents = true;
            }
            catch { /* no bloquear si FS watcher no está disponible */ }

            try
            {
                _watcherAdditional = new FileSystemWatcher(Path.GetDirectoryName(_dataFile) ?? AppContext.BaseDirectory)
                {
                    Filter = Path.GetFileName(_dataFile),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size
                };
                _watcherAdditional.Changed += FileChangedHandler;
                _watcherAdditional.Created += FileChangedHandler;
                _watcherAdditional.Deleted += FileChangedHandler;
                _watcherAdditional.EnableRaisingEvents = true;
            }
            catch { /* ignore */ }

            Debug.WriteLine($"[AdditionalChargeViewModel] ctor end - Charges.Count = {Charges.Count}");
        }

        private void FileChangedHandler(object sender, FileSystemEventArgs e)
        {
            // reiniciar temporizador debounce para evitar múltiples recálculos seguidos
            try
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
            catch { }
        }

        private void Charges_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (AdditionalChargeItem oldItem in e.OldItems)
                    DetachItemHandler(oldItem);
            }

            if (e.NewItems != null)
            {
                foreach (AdditionalChargeItem newItem in e.NewItems)
                    AttachItemHandler(newItem);
            }

            RecalcDailyTotalCost();
        }

        private void AttachItemHandler(AdditionalChargeItem item)
        {
            if (item != null)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        private void DetachItemHandler(AdditionalChargeItem item)
        {
            if (item != null)
                item.PropertyChanged -= Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // si cambia Quantity, precio o total recalcular
            if (e.PropertyName == nameof(AdditionalChargeItem.Quantity) ||
                e.PropertyName == nameof(AdditionalChargeItem.UnitPrice) ||
                e.PropertyName == nameof(AdditionalChargeItem.Total))
            {
                RecalcDailyTotalCost();
            }
        }

        private void RecalcDailyTotalCost()
        {
            try
            {
                var total = Charges.Sum(c => c.Total);
                DailyTotalCost = Math.Round(total, 2);
            }
            catch
            {
                DailyTotalCost = 0;
            }

            // después de recalcular el propio total, actualizar combinado
            RecalcCombinedTotal();
        }

        // Nuevo: calcula Products + WholeFluids + AdditionalCharge
        private void RecalcCombinedTotal()
        {
            double productsTotal = 0;
            double wholeFluidsTotal = 0;
            double additionalTotal = DailyTotalCost;

            try
            {
                // Filtrar por fecha del día (Daily). Además, solo contar movimientos de tipo Consumed para coste diario,
                // para concordar con InventoryProductsDashboardViewModel (DailyCost).
                if (_inventoryService != null)
                {
                    var today = DateTime.Today;
                    productsTotal = _inventoryService.GetMovements()
                        .Where(m => m.Date.Date == today && m.Type == TicketType.Consumed)
                        .Sum(m => m.UnitPrice * m.Quantity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AdditionalChargeViewModel] error calculando productsTotal: " + ex);
                productsTotal = 0;
            }

            try
            {
                if (File.Exists(_wholeFluidsFile))
                {
                    var json = File.ReadAllText(_wholeFluidsFile);
                    var arr = JsonSerializer.Deserialize<WholeFluidItem[]>(json);
                    if (arr != null)
                    {
                        // Filtrar por la fecha del día (si la entrada tiene Date) para que sea un "daily total"
                        var today = DateTime.Today;
                        wholeFluidsTotal = arr
                            .Where(w => w.Date.Date == today)
                            .Sum(w => w.UnitPrice * w.Quantity);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AdditionalChargeViewModel] error calculando wholeFluidsTotal: " + ex);
                wholeFluidsTotal = 0;
            }

            CombinedDailyTotal = Math.Round(productsTotal + wholeFluidsTotal + additionalTotal, 2);
        }

        private void SeedDefaults()
        {
            // Lista base que solicitaste
            var defaults = new[]
            {
                "CARROTANQUE",
                "CARTUCHOS",
                "COSTO DE EQUIPO MPSA - INCLUYE OPERADOR",
                "FILTROS",
                "INGENIERIA",
                "INGENIERO DE FluidS",
                "INGENIERO DE FluidS JUNIOR",
                "INGENIERO DE FluidS OPERATIVO",
                "INGENIERO DE FluidS SENIOR",
                "MOVILIZACION DE QUIMICOS EN CAMA ALTA",
                "MOVILIZACION DE QUIMICOS EN TURBO",
                "MOVILIZACION INGENIERO DE FluidS",
                "MOVILIZACION Unit DE FILTRADO",
                "MOVILIZACION Unit DE FLOCULACION",
                "MOVILIZACION/DESMOVILIZACION Unit DE FILTRADO",
                "MOVILIZACION/DESMOVILIZACION Unit DE FLOCULACION",
                "MOVILIZACION/DESMOVILIZACION Unit DE MEZCLA",
                "SERVICIO DE ALIMENTACION",
                "SERVICIO DE ALIMENTACION Y HOSPEDAJE",
                "SERVICIO DE HOSPEDAJE",
                "STAND BY Unit DE FILTRADO",
                "TECNICO DE Unit DE FILTRADO OPERATIVO",
                "TECNICO DE Unit DE FLOCULACION",
                "TECNICO DE Unit DE FLOCULACION OPERATIVO",
                "TECNICO Unit DE FILTRADO",
                "TRANSPORTE",
                "TRANSPORTE DE Fluid DE COMPLETAMIENTO",
                "TRANSPORTE DE Fluid DE PERFORACION",
                "Unit DE FILTRADO",
                "Unit DE FLOCULACION",
                "Unit DE MEZCLA EN OPERACI?N",
                "Unit DE MEZCLA OPERATIVA",
                "OPCION ADICIONAL"
            };

            DefaultChargeNames.Clear();
            foreach (var d in defaults) DefaultChargeNames.Add(d);

            // Si no hay fichero persistido, añadir una línea de ejemplo (opcional)
            if (Charges.Count == 0 && !File.Exists(_dataFile))
            {
                Charges.Add(new AdditionalChargeItem
                {
                    Name = defaults.First(),
                    Unit = UnitOptions.FirstOrDefault() ?? "Each",
                    Quantity = 1,
                    UnitPrice = 0.0,
                    Observations = "",
                    Currency = CurrencyOptions.FirstOrDefault() ?? "USD"
                });
            }
        }

        public void Add()
        {
            Charges.Add(new AdditionalChargeItem
            {
                Name = DefaultChargeNames.FirstOrDefault() ?? string.Empty,
                Unit = UnitOptions.FirstOrDefault() ?? "Each",
                Quantity = 1,
                UnitPrice = 0,
                Observations = "",
                Currency = CurrencyOptions.FirstOrDefault() ?? "USD"
            });
            Error = $"Line added. Total charges: {Charges.Count}";
            RecalcDailyTotalCost();
        }

        public void Remove(AdditionalChargeItem? item)
        {
            if (item == null) return;
            Charges.Remove(item);
            RecalcDailyTotalCost();
        }

        public void Save()
        {
            try
            {
                var arr = Charges.ToArray();
                var json = JsonSerializer.Serialize(arr, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFile, json);
                Error = "Additional charges saved successfully.";
            }
            catch (Exception ex)
            {
                Error = "Error saving: " + ex.Message;
            }
        }

        public void LoadFromFile()
        {
            try
            {
                if (!File.Exists(_dataFile)) return;
                var json = File.ReadAllText(_dataFile);
                var arr = JsonSerializer.Deserialize<AdditionalChargeItem[]>(json);
                if (arr == null) return;
                Charges.Clear();
                foreach (var c in arr) Charges.Add(c);
                Error = $"Additional charges loaded ({Charges.Count}).";
                RecalcDailyTotalCost();
            }
            catch (Exception ex)
            {
                Error = "Error loading: " + ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            try
            {
                _watcherWhole?.Dispose();
                _watcherAdditional?.Dispose();
                _debounceTimer?.Stop();
                _debounceTimer?.Dispose();
                if (_inventoryService != null && _inventoryUpdatedHandler != null)
                {
                    _inventoryService.InventoryUpdated -= _inventoryUpdatedHandler;
                    _inventoryUpdatedHandler = null;
                }
            }
            catch { }
        }
    }

}

