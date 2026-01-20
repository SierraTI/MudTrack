using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ClosedXML.Excel;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Services;
using ProjectReport.ViewModels;

// Alias para evitar colisiones entre namespace y tipo
using RigProfileClass = ProjectReport.Models.Rig.RigProfile;

namespace ProjectReport.Modules.RigProfile.ViewModels
{
    public class RigProfileViewModel : BaseViewModel
    {
        private RigProfileClass _currentRigProfile;
        private readonly WellContextService _contextService;
        private readonly HydraulicsCalculationService _hydraulicsService;
        private Well? _currentWell;
        private readonly List<CatalogItem> _catalog = new();

        // =========================
        // LISTA FIJA PARA MODEL
        // =========================
        private static readonly string[] FixedModelOptions =
        {
            "FLC 500 Scalper",
            "VSM 300 Scalper",
            "GNZS703 Series Scalper",
            "Hyperpool",
            "FLC 500 Series",
            "FLC 2000 Series",
            "King Cobra",
            "VSM 300 Series",
            "Mongoose PT",
            "MD-3",
            "GNZS703 Series",
            "GNZS594 Series",
            "FLC 503/504",
            "Mud King Combo",
            "Mongoose Combo",
            "GNZJ703 Series",
            "FLC Series (10\" cones)",
            "10\" Hydrocyclone",
            "DC-10",
            "GN Hydrocyclone Desander",
            "FLC Series (4\" cones)",
            "4\" Hydrocyclone",
            "DC-4",
            "GN Hydrocyclone Desilter",
            "DE-1000",
            "DE-7200",
            "HS-3400",
            "VSM Decanter",
            "CD-500",
            "CD-600",
            "GNLW363",
            "GNLW452"
        };

        public RigProfileViewModel()
        {
            _contextService = WellContextService.Instance;
            _hydraulicsService = new HydraulicsCalculationService();
            _contextService.WellChanged += OnWellChanged;

            _currentRigProfile = new RigProfileClass();

            if (_contextService.CurrentWell != null)
                LoadRigProfile(_contextService.CurrentWell);

            // ?? CAMBIO: AvailableModels debe poder setearse y arrancar con la lista fija
            AvailableModels = new ObservableCollection<string>(FixedModelOptions);

            AvailableTypes = new ObservableCollection<string>();
            AvailableManufacturers = new ObservableCollection<string>();
            AvailablePitShapes = new ObservableCollection<string> { "Rectangular", "Cylindrical", "Oval", "Other" };

            // Lista de estilos para la columna "Style" (tal como pediste)
            AvailableStyles = new ObservableCollection<string>
            {
                "Scalper",
                "Shaker",
                "Mud cleaner",
                "Deilter",
                "Desander",
                "Centrifuge"
            };

            _testDensity = 10.0;
            _testGpm = 500.0;

            LoadCatalogFromExcel();
            InitializeCatalogCollections();

            // Sobrescribimos la lista de fabricantes con la lista fija solicitada
            AvailableManufacturers = new ObservableCollection<string>
            {
                "GN Solids Control",
                "KEMTRON Technologies",
                "Sistemas integrados de control de sólidos.",
                "Elgin Separation Solutions",
                "H-Screening Separation",
                "FLC (Fluid Systems Inc.)",
                "PetroSolids (México)",
                "MAS OPCIONES"
            };

            SelectedSurfaceType = AvailableTypes.FirstOrDefault() ?? string.Empty;

            EnsureSurfaceDefaults();
            EnsureServiceLineDefaults();

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            SaveAndReturnCommand = new RelayCommand(async _ => await SaveAndReturnAsync());
            ResetToDefaultCommand = new RelayCommand(_ => ResetToDefault());
        }

        private void OnWellChanged(object? sender, Well well) => LoadRigProfile(well);

        private void LoadRigProfile(Well well)
        {
            _currentWell = well;
            CurrentRigProfile = well.RigProfile ?? new RigProfileClass();
            EnsureSurfaceDefaults();
            EnsureServiceLineDefaults();
        }

        public RigProfileClass CurrentRigProfile
        {
            get => _currentRigProfile;
            set
            {
                if (SetProperty(ref _currentRigProfile, value))
                {
                    OnPropertyChanged(nameof(SurfaceEquipment));
                    OnPropertyChanged(nameof(ServiceLine));
                    OnPropertyChanged(nameof(Pumps));
                    OnPropertyChanged(nameof(SolidsControl));
                    OnPropertyChanged(nameof(Pits));
                    OnPropertyChanged(nameof(TotalSurfaceLoss));
                }
            }
        }

        // Collection wrappers
        public ObservableCollection<RigSurfaceEquipment> SurfaceEquipment => CurrentRigProfile?.SurfaceEquipment ?? new ObservableCollection<RigSurfaceEquipment>();
        public ObservableCollection<RigSurfaceEquipment> ServiceLine => CurrentRigProfile?.ServiceLine ?? new ObservableCollection<RigSurfaceEquipment>();
        public ObservableCollection<RigPump> Pumps => CurrentRigProfile?.Pumps ?? new ObservableCollection<RigPump>();
        public ObservableCollection<RigSolidsControl> SolidsControl => CurrentRigProfile?.SolidsControl ?? new ObservableCollection<RigSolidsControl>();
        public ObservableCollection<RigPit> Pits => CurrentRigProfile?.Pits ?? new ObservableCollection<RigPit>();

        // Catalog collections
        public ObservableCollection<string> AvailableTypes { get; private set; }
        public ObservableCollection<string> AvailableManufacturers { get; private set; }

        // ?? CAMBIO: permitir set porque la inicializamos con lista fija
        public ObservableCollection<string> AvailableModels { get; private set; }

        public ObservableCollection<string> AvailablePitShapes { get; }

        // Lista de estilos para la columna "Style"
        public ObservableCollection<string> AvailableStyles { get; private set; }

        private string _selectedSurfaceType = string.Empty;
        public string SelectedSurfaceType
        {
            get => _selectedSurfaceType;
            set => SetProperty(ref _selectedSurfaceType, value);
        }

        public void FilterManufacturers(string type)
        {
            var manufacturers = _catalog.Where(c => c.Type == type)
                                       .Select(c => c.Manufacturer)
                                       .Distinct()
                                       .OrderBy(x => x)
                                       .ToList();

            AvailableManufacturers.Clear();
            foreach (var m in manufacturers) AvailableManufacturers.Add(m);
        }

        private double _testDensity;
        public double TestDensity
        {
            get => _testDensity;
            set { if (SetProperty(ref _testDensity, value)) OnPropertyChanged(nameof(TotalSurfaceLoss)); }
        }

        private double _testGpm;
        public double TestGpm
        {
            get => _testGpm;
            set
            {
                if (SetProperty(ref _testGpm, value))
                {
                    OnPropertyChanged(nameof(TotalSurfaceLoss));
                    _contextService.UpdateFlowRate(value);
                }
            }
        }

        public double TotalSurfaceLoss
        {
            get
            {
                if (_currentWell?.RigProfile == null) return 0;
                return _hydraulicsService.CalculateTotalSurfacePressureLoss(_currentWell.RigProfile, TestDensity, TestGpm);
            }
        }

        // Catalog loading
        private void LoadCatalogFromExcel()
        {
            try
            {
                var excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Lista.xlsx");
                if (!File.Exists(excelPath)) { LoadDefaultCatalog(); return; }

                using var wb = new XLWorkbook(excelPath);
                var ws = wb.Worksheets.FirstOrDefault();
                if (ws == null) { LoadDefaultCatalog(); return; }

                var firstRow = ws.FirstRowUsed();
                if (firstRow == null) { LoadDefaultCatalog(); return; }

                var headerRow = firstRow;
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int c = 1; c <= (headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0); c++)
                {
                    var headerText = headerRow.Cell(c).GetString().Trim();
                    if (!string.IsNullOrEmpty(headerText) && !headerMap.ContainsKey(headerText))
                        headerMap[headerText] = c;
                }

                int GetCol(params string[] names)
                {
                    foreach (var n in names)
                    {
                        if (string.IsNullOrWhiteSpace(n)) continue;
                        if (headerMap.TryGetValue(n, out var idx)) return idx;
                        var found = headerMap.Keys.FirstOrDefault(k => k.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (found != null) return headerMap[found];
                    }
                    return -1;
                }

                var colType = GetCol("Type", "Tipo", "Equipment Type");
                var colManufacturer = GetCol("Manufacturer", "Fabricante", "Maker");
                var colModel = GetCol("Model", "Modelo");
                var colGpm = GetCol("GPM", "Capacity", "Cap Flow", "Flow Capacity", "Capacidad");

                var startRow = headerRow.RowNumber() + 1;
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? ws.Rows().Count();

                for (int r = startRow; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    if (row.IsEmpty()) continue;

                    var item = new CatalogItem();

                    if (colType > 0) item.Type = row.Cell(colType).GetString().Trim();
                    if (colManufacturer > 0) item.Manufacturer = row.Cell(colManufacturer).GetString().Trim();
                    if (colModel > 0) item.Model = row.Cell(colModel).GetString().Trim();

                    if (colGpm > 0)
                    {
                        if (row.Cell(colGpm).TryGetValue<double>(out var gpm))
                            item.Gpm = gpm;
                        else
                        {
                            var gpmText = row.Cell(colGpm).GetString().Trim();
                            if (double.TryParse(gpmText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedGpm))
                                item.Gpm = parsedGpm;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(item.Type) && !string.IsNullOrWhiteSpace(item.Manufacturer))
                        _catalog.Add(item);
                }
            }
            catch
            {
                LoadDefaultCatalog();
            }
        }

        private void LoadDefaultCatalog()
        {
            _catalog.Clear();
            _catalog.AddRange(new[]
            {
                new CatalogItem { Type = "Shaker", Manufacturer = "Derrick", Model = "Flo-Line Cleaner 503", Gpm = 500 },
                new CatalogItem { Type = "Shaker", Manufacturer = "Derrick", Model = "Hyperpool", Gpm = 600 },
                new CatalogItem { Type = "Shaker", Manufacturer = "NOV Brandt", Model = "Cobra", Gpm = 550 },
                new CatalogItem { Type = "Shaker", Manufacturer = "NOV Brandt", Model = "King Cobra", Gpm = 700 },
                new CatalogItem { Type = "Shaker", Manufacturer = "MI-SWACO", Model = "Mongoose PRO", Gpm = 650 },
                new CatalogItem { Type = "Centrifuge", Manufacturer = "Derrick", Model = "DS-2", Gpm = 1000 },
                new CatalogItem { Type = "Centrifuge", Manufacturer = "Derrick", Model = "S-10", Gpm = 800 }
            });
        }

        private void InitializeCatalogCollections()
        {
            AvailableTypes = new ObservableCollection<string>(_catalog.Select(x => x.Type).Distinct().OrderBy(x => x));
            AvailableManufacturers = new ObservableCollection<string>(_catalog.Select(x => x.Manufacturer).Distinct().OrderBy(x => x));

            // ? por si alguien quiere "re-inicializar", lo blindamos
            if (AvailableModels == null || AvailableModels.Count == 0)
                AvailableModels = new ObservableCollection<string>(FixedModelOptions);
        }

        // Defaults
        private void EnsureSurfaceDefaults()
        {
            if (SurfaceEquipment == null) return;
            if (SurfaceEquipment.Count > 0) return;

            SurfaceEquipment.Add(new RigSurfaceEquipment { No = 1, Component = "Stand Pipe", InternalDiameter = 0.0, Length = 0.0 });
            SurfaceEquipment.Add(new RigSurfaceEquipment { No = 2, Component = "Drilling Hose", InternalDiameter = 0.0, Length = 0.0 });
            SurfaceEquipment.Add(new RigSurfaceEquipment { No = 3, Component = "Swivel / Top Drive", InternalDiameter = 0.0, Length = 0.0 });
            SurfaceEquipment.Add(new RigSurfaceEquipment { No = 4, Component = "Kelly", InternalDiameter = 0.0, Length = 0.0 });
        }

        private void EnsureServiceLineDefaults()
        {
            if (ServiceLine == null) return;
            if (ServiceLine.Count > 0) return;

            ServiceLine.Add(new RigSurfaceEquipment { No = 1, Component = "Choke Line", InternalDiameter = 0.0, Length = 0.0 });
            ServiceLine.Add(new RigSurfaceEquipment { No = 2, Component = "Kill Line", InternalDiameter = 0.0, Length = 0.0 });
            ServiceLine.Add(new RigSurfaceEquipment { No = 3, Component = "Booster Line", InternalDiameter = 0.0, Length = 0.0 });
        }

        // Commands
        public ICommand AddSurfaceEquipmentCommand => new RelayCommand(_ => AddSurfaceItem());
        public ICommand RemoveSurfaceEquipmentCommand => new RelayCommand(p => RemoveSurfaceItem(p as RigSurfaceEquipment));
        public ICommand RemoveServiceLineCommand => new RelayCommand(p => RemoveServiceLineItem(p as RigSurfaceEquipment));
        public ICommand AddPumpCommand => new RelayCommand(_ => AddPump());
        public ICommand RemovePumpCommand => new RelayCommand(p => RemovePump(p as RigPump));
        public ICommand AddSolidsControlCommand => new RelayCommand(_ => AddSolidsControl());
        public ICommand RemoveSolidsControlCommand => new RelayCommand(p => RemoveSolidsControl(p as RigSolidsControl));
        public ICommand AddPitCommand => new RelayCommand(_ => AddPit());
        public ICommand RemovePitCommand => new RelayCommand(p => RemovePit(p as RigPit));
        public ICommand SaveCommand { get; }
        public ICommand SaveAndReturnCommand { get; }
        public ICommand ResetToDefaultCommand { get; }

        // Logic
        private void AddSurfaceItem()
        {
            int nextNo = (SurfaceEquipment?.Count ?? 0) + 1;
            var name = string.IsNullOrWhiteSpace(SelectedSurfaceType) ? $"Component {nextNo}" : SelectedSurfaceType;
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = nextNo, Component = name });
        }

        private void RemoveSurfaceItem(RigSurfaceEquipment? item)
        {
            if (item != null && SurfaceEquipment != null)
            {
                SurfaceEquipment.Remove(item);
                Renumber(SurfaceEquipment);
            }
        }

        private void RemoveServiceLineItem(RigSurfaceEquipment? item)
        {
            if (item != null && ServiceLine != null)
            {
                ServiceLine.Remove(item);
                Renumber(ServiceLine);
            }
        }

        private void AddPump()
        {
            int nextNo = (Pumps?.Count ?? 0) + 1;
            Pumps?.Add(new RigPump { No = nextNo });
        }

        private void RemovePump(RigPump? item)
        {
            if (item != null && Pumps != null)
            {
                Pumps.Remove(item);
                Renumber(Pumps);
            }
        }

        private void AddSolidsControl()
        {
            int nextNo = (SolidsControl?.Count ?? 0) + 1;
            SolidsControl?.Add(new RigSolidsControl { No = nextNo });
        }

        private void RemoveSolidsControl(RigSolidsControl? item)
        {
            if (item != null && SolidsControl != null)
            {
                SolidsControl.Remove(item);
                Renumber(SolidsControl);
            }
        }

        private void AddPit()
        {
            int nextNo = (Pits?.Count ?? 0) + 1;
            Pits?.Add(new RigPit { No = nextNo });
        }

        private void RemovePit(RigPit? item)
        {
            if (item != null && Pits != null)
            {
                Pits.Remove(item);
                Renumber(Pits);
            }
        }

        private void Renumber<T>(ObservableCollection<T> collection)
        {
            if (collection == null) return;
            int i = 1;
            foreach (var item in collection)
            {
                if (item == null) continue;
                var prop = item.GetType().GetProperty("No");
                prop?.SetValue(item, i++);
            }
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            try
            {
                if (_currentWell == null) return;

                var pumpsWithoutEfficiency = Pumps.Where(p => p.Efficiency <= 0).ToList();
                if (pumpsWithoutEfficiency.Any())
                {
                    ToastNotificationService.Instance.ShowWarning("Some pumps are missing efficiency values. Please complete all pump data.");
                }

                _currentWell.RigProfile = CurrentRigProfile;

                _currentWell.RigName = CurrentRigProfile.RigName;
                _currentWell.Contractor = CurrentRigProfile.Contractor;
                _currentWell.RigType = CurrentRigProfile.RigType;

                var projectFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project_data.json");
                var project = _contextService.CurrentProject;
                if (project != null)
                {
                    await DataPersistenceService.SaveProjectAsync(projectFilePath, project);
                    ToastNotificationService.Instance.ShowSuccess("Rig Profile saved successfully");
                }
                else
                {
                    ToastNotificationService.Instance.ShowWarning("No project context available. Changes may not be persisted.");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error saving Rig Profile: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SaveAndReturnAsync()
        {
            await SaveAsync();
            if (_currentWell != null) NavigationService.Instance.NavigateToWellDashboard(_currentWell.Id);
        }

        private void ResetToDefault()
        {
            SurfaceEquipment?.Clear();
            ServiceLine?.Clear();
            Pumps?.Clear();
            SolidsControl?.Clear();
            Pits?.Clear();

            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 1, Component = "Stand Pipe", InternalDiameter = 0.0, Length = 0.0 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 2, Component = "Drilling Hose", InternalDiameter = 0.0, Length = 0.0 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 3, Component = "Swivel / Top Drive", InternalDiameter = 0.0, Length = 0.0 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 4, Component = "Kelly", InternalDiameter = 0.0, Length = 0.0 });

            ServiceLine?.Add(new RigSurfaceEquipment { No = 1, Component = "Choke Line", InternalDiameter = 0.0, Length = 0.0 });
            ServiceLine?.Add(new RigSurfaceEquipment { No = 2, Component = "Kill Line", InternalDiameter = 0.0, Length = 0.0 });
            ServiceLine?.Add(new RigSurfaceEquipment { No = 3, Component = "Booster Line", InternalDiameter = 0.0, Length = 0.0 });

            Pumps?.Add(new RigPump { No = 1, PumpName = "Pump 1", LinerSize = 6.5, StrokeLength = 12, Efficiency = 95 });

            // Ejemplo por defecto para SolidsControl (incluye las columnas solicitadas)
            SolidsControl?.Add(new RigSolidsControl
            {
                No = 1,
                Type = "Shaker",
                Manufacturer = "Derrick",
                Model = "Flo-Line Cleaner 503",
                GpmCapacity = 500,
                CapFlowGpm = 500,
                NumberOfScreens = 3,
                ScreenType = "API",
                Style = "Shaker",
                DesilterNumberOfCones = 0,
                DesilterConeSize = 0.0,
                DesanderNumberOfCones = 0,
                DesanderConeSize = 0.0
            });

            CurrentRigProfile.RigName = string.Empty;
            CurrentRigProfile.Contractor = string.Empty;
            CurrentRigProfile.RigType = string.Empty;
            CurrentRigProfile.RkbElevation = 0;
            CurrentRigProfile.CasingHeadElevation = 0;

            SelectedSurfaceType = AvailableTypes.FirstOrDefault() ?? string.Empty;

            // ? Asegura que la lista fija de Models siga disponible después del reset
            AvailableModels.Clear();
            foreach (var m in FixedModelOptions)
                AvailableModels.Add(m);

            ToastNotificationService.Instance.ShowInfo("Rig Profile reset to defaults");
        }

        // New methods
        public IEnumerable<string> GetModels(string type, string manufacturer)
        {
            return _catalog
                .Where(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(c.Manufacturer, manufacturer, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Model)
                .Distinct()
                .OrderBy(x => x);
        }

        // ? CAMBIO CRÍTICO: ya NO filtra por Excel / manufacturer. Siempre lista fija.
        public void UpdateAvailableModels(string type, string manufacturer)
        {
            if (AvailableModels == null) return;
            AvailableModels.Clear();
            foreach (var model in FixedModelOptions)
                AvailableModels.Add(model);
        }

        public void UpdateSolidsControlSpecs(RigSolidsControl item)
        {
            if (item == null) return;

            var match = _catalog.FirstOrDefault(c =>
                string.Equals(c.Type, item.Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Manufacturer, item.Manufacturer, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Model, item.Model, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                item.GpmCapacity = match.Gpm;
                // también asignar al campo explícito de "Cap flow (gpm)"
                item.CapFlowGpm = match.Gpm;
            }
        }

        // Internal Helper
        public class CatalogItem
        {
            public string Type { get; set; } = "";
            public string Manufacturer { get; set; } = "";
            public string Model { get; set; } = "";
            public double Gpm { get; set; }
        }
    }
}
