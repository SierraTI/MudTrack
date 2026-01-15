using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using ProjectReport.Models.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class AdditionalChargeViewModel : INotifyPropertyChanged
    {
        private readonly string _dataFile;

        public ObservableCollection<AdditionalChargeItem> Charges { get; } = new ObservableCollection<AdditionalChargeItem>();
        public ObservableCollection<string> DefaultChargeNames { get; } = new ObservableCollection<string>();

        // Opciones visibles en la lista desplegable por fila
        public ObservableCollection<string> CurrencyOptions { get; } = new ObservableCollection<string> { "USD", "COP" };
        // Opciones para la unidad (lista desplegable Unit)
        public ObservableCollection<string> UnitOptions { get; } = new ObservableCollection<string> { "Each", "Day" };

        private string _error = "";
        public string Error
        {
            get => _error;
            set { if (_error != value) { _error = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error))); } }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ReloadCommand { get; }

        public AdditionalChargeViewModel()
        {
            Debug.WriteLine("[AdditionalChargeViewModel] ctor start");
            _dataFile = Path.Combine(AppContext.BaseDirectory, "Data", "additional_charges.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_dataFile) ?? AppContext.BaseDirectory);

            AddCommand = new RelayCommand(_ => Add());
            RemoveCommand = new RelayCommand(param => Remove(param as AdditionalChargeItem));
            SaveCommand = new RelayCommand(_ => Save());
            ReloadCommand = new RelayCommand(_ => LoadFromFile());

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
            Debug.WriteLine($"[AdditionalChargeViewModel] ctor end - Charges.Count = {Charges.Count}");
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
                "INGENIERO DE FLUIDOS",
                "INGENIERO DE FLUIDOS JUNIOR",
                "INGENIERO DE FLUIDOS OPERATIVO",
                "INGENIERO DE FLUIDOS SENIOR",
                "MOVILIZACION DE QUIMICOS EN CAMA ALTA",
                "MOVILIZACION DE QUIMICOS EN TURBO",
                "MOVILIZACION INGENIERO DE FLUIDOS",
                "MOVILIZACION UNIDAD DE FILTRADO",
                "MOVILIZACION UNIDAD DE FLOCULACION",
                "MOVILIZACION/DESMOVILIZACION UNIDAD DE FILTRADO",
                "MOVILIZACION/DESMOVILIZACION UNIDAD DE FLOCULACION",
                "MOVILIZACION/DESMOVILIZACION UNIDAD DE MEZCLA",
                "SERVICIO DE ALIMENTACION",
                "SERVICIO DE ALIMENTACION Y HOSPEDAJE",
                "SERVICIO DE HOSPEDAJE",
                "STAND BY UNIDAD DE FILTRADO",
                "TECNICO DE UNIDAD DE FILTRADO OPERATIVO",
                "TECNICO DE UNIDAD DE FLOCULACION",
                "TECNICO DE UNIDAD DE FLOCULACION OPERATIVO",
                "TECNICO UNIDAD DE FILTRADO",
                "TRANSPORTE",
                "TRANSPORTE DE FLUIDO DE COMPLETAMIENTO",
                "TRANSPORTE DE FLUIDO DE PERFORACION",
                "UNIDAD DE FILTRADO",
                "UNIDAD DE FLOCULACION",
                "UNIDAD DE MEZCLA EN OPERACIÓN",
                "UNIDAD DE MEZCLA OPERATIVA",
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
            Error = $"Línea agregada. Total cargos: {Charges.Count}";
        }

        public void Remove(AdditionalChargeItem? item)
        {
            if (item == null) return;
            Charges.Remove(item);
        }

        public void Save()
        {
            try
            {
                var arr = Charges.ToArray();
                var json = JsonSerializer.Serialize(arr, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFile, json);
                Error = "Cargos adicionales guardados correctamente.";
            }
            catch (Exception ex)
            {
                Error = "Error guardando: " + ex.Message;
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
                Error = $"Cargos cargados ({Charges.Count}).";
            }
            catch (Exception ex)
            {
                Error = "Error cargando: " + ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}