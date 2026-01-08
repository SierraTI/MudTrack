using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using RigProfileClass = ProjectReport.Models.Rig.RigProfile;
using ProjectReport.Services;
using ProjectReport.ViewModels;
using ClosedXML.Excel;
using System.Globalization;

namespace ProjectReport.Modules.RigProfile.ViewModels
{
    public class RigProfileViewModel : BaseViewModel
    {
        private RigProfileClass _currentRigProfile;
        
        // Catalog Data Source - Loaded from Excel
        private readonly WellContextService _contextService;
        private readonly HydraulicsCalculationService _hydraulicsService;
        private Well? _currentWell;
        private readonly List<CatalogItem> _catalog = new();

        public RigProfileViewModel()
        {
            // Initial setup
            _contextService = WellContextService.Instance;
            _hydraulicsService = new HydraulicsCalculationService();
            _contextService.WellChanged += OnWellChanged;
            
            // Initialize with a default profile
            _currentRigProfile = new RigProfileClass();

            // Load current if exists
            if (_contextService.CurrentWell != null)
            {
                LoadRigProfile(_contextService.CurrentWell);
            }

            AvailableModels = new ObservableCollection<string>();
            AvailableTypes = new ObservableCollection<string>();
            AvailableManufacturers = new ObservableCollection<string>();
            AvailablePitShapes = new ObservableCollection<string> { "Rectangular", "Cylindrical", "Oval", "Other" };

            // Test parameters for preview
            _testDensity = 10.0;
            _testGpm = 500.0;

            // Load catalog from Excel
            LoadCatalogFromExcel();

            InitializeCatalogCollections();

            // Initialize commands
            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            SaveAndReturnCommand = new RelayCommand(async _ => await SaveAndReturnAsync());
            ResetToDefaultCommand = new RelayCommand(_ => ResetToDefault());
        }

        private void OnWellChanged(object? sender, Well well)
        {
            if (well != null)
            {
                LoadRigProfile(well);
            }
        }

        private void LoadRigProfile(Well well)
        {
            _currentWell = well;
            CurrentRigProfile = well.RigProfile ?? new RigProfileClass();
        }

        public RigProfileClass CurrentRigProfile
        {
            get => _currentRigProfile;
            set
            {
                if (SetProperty(ref _currentRigProfile, value))
                {
                    OnPropertyChanged(nameof(SurfaceEquipment));
                    OnPropertyChanged(nameof(Pumps));
                    OnPropertyChanged(nameof(SolidsControl));
                    OnPropertyChanged(nameof(Pits));
                    OnPropertyChanged(nameof(TotalSurfaceLoss));
                }
            }
        }

        // Collections wrappers for binding
        public ObservableCollection<RigSurfaceEquipment> SurfaceEquipment => CurrentRigProfile?.SurfaceEquipment ?? new ObservableCollection<RigSurfaceEquipment>();
        public ObservableCollection<RigPump> Pumps => CurrentRigProfile?.Pumps ?? new ObservableCollection<RigPump>();
        public ObservableCollection<RigSolidsControl> SolidsControl => CurrentRigProfile?.SolidsControl ?? new ObservableCollection<RigSolidsControl>();
        public ObservableCollection<RigPit> Pits => CurrentRigProfile?.Pits ?? new ObservableCollection<RigPit>();

        // Catalog Collections
        public ObservableCollection<string> AvailableTypes { get; private set; }
        public ObservableCollection<string> AvailableManufacturers { get; private set; }
        public ObservableCollection<string> AvailableModels { get; }
        public ObservableCollection<string> AvailablePitShapes { get; }

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
            set { if (SetProperty(ref _testGpm, value)) OnPropertyChanged(nameof(TotalSurfaceLoss)); }
        }

        public double TotalSurfaceLoss
        {
            get
            {
                if (_currentWell?.RigProfile == null) return 0;
                return _hydraulicsService.CalculateTotalSurfacePressureLoss(_currentWell.RigProfile, TestDensity, TestGpm);
            }
        }

        private void LoadCatalogFromExcel()
        {
            try
            {
                var excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Lista.xlsx");
                if (!File.Exists(excelPath))
                {
                    // Fallback to hardcoded catalog if Excel not found
                    LoadDefaultCatalog();
                    return;
                }

                using var wb = new XLWorkbook(excelPath);
                var ws = wb.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    LoadDefaultCatalog();
                    return;
                }

                var firstRow = ws.FirstRowUsed();
                if (firstRow == null)
                {
                    LoadDefaultCatalog();
                    return;
                }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading catalog from Excel: {ex.Message}");
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
        }

        // Commands
        public ICommand AddSurfaceEquipmentCommand => new RelayCommand(_ => AddSurfaceItem());
        public ICommand RemoveSurfaceEquipmentCommand => new RelayCommand(p => RemoveSurfaceItem(p as RigSurfaceEquipment));
        
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
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = nextNo });
        }

        private void RemoveSurfaceItem(RigSurfaceEquipment? item)
        {
            if (item != null && SurfaceEquipment != null)
            {
                SurfaceEquipment.Remove(item);
                Renumber(SurfaceEquipment);
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

        public void UpdateSolidsControlSpecs(RigSolidsControl item)
        {
            var match = _catalog.FirstOrDefault(c => 
                c.Type == item.Type && 
                c.Manufacturer == item.Manufacturer && 
                c.Model == item.Model);

            if (match != null)
            {
                item.GpmCapacity = match.Gpm;
            }
        }

        public IEnumerable<string> GetModels(string type, string manufacturer)
        {
            return _catalog.Where(c => c.Type == type && c.Manufacturer == manufacturer)
                           .Select(c => c.Model)
                           .Distinct()
                           .OrderBy(x => x);
        }

        public void UpdateAvailableModels(string type, string manufacturer)
        {
            AvailableModels.Clear();
            foreach (var model in GetModels(type, manufacturer))
            {
                AvailableModels.Add(model);
            }
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            try
            {
                if (_currentWell == null) return;

                // Validate pumps have efficiency
                var pumpsWithoutEfficiency = Pumps.Where(p => p.Efficiency <= 0).ToList();
                if (pumpsWithoutEfficiency.Any())
                {
                    ToastNotificationService.Instance.ShowWarning("Some pumps are missing efficiency values. Please complete all pump data.");
                }

                _currentWell.RigProfile = CurrentRigProfile;

                // Sync back to Well properties
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
            
            // Navigate back to Well Dashboard
            if (_currentWell != null)
            {
                NavigationService.Instance.NavigateToWellDashboard(_currentWell.Id);
            }
        }

        private void ResetToDefault()
        {
            // Clear all collections
            SurfaceEquipment?.Clear();
            Pumps?.Clear();
            SolidsControl?.Clear();
            Pits?.Clear();

            // Populate Table A: Surface & Service Equipment
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 1, Component = "Stand Pipe", InternalDiameter = 4.0, Length = 40 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 2, Component = "Drilling Hose", InternalDiameter = 3.5, Length = 60 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 3, Component = "Swivel / Top Drive", InternalDiameter = 3.0, Length = 20 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 4, Component = "Kelly", InternalDiameter = 3.0, Length = 40 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 5, Component = "Choke Line", InternalDiameter = 3.0, Length = 150 });
            SurfaceEquipment?.Add(new RigSurfaceEquipment { No = 6, Component = "Kill Line", InternalDiameter = 3.0, Length = 150 });

            // Default Pump
            Pumps?.Add(new RigPump { No = 1, PumpName = "Pump 1", LinerSize = 6.5, StrokeLength = 12, Efficiency = 95 });

            // Reset general properties
            CurrentRigProfile.RigName = string.Empty;
            CurrentRigProfile.Contractor = string.Empty;
            CurrentRigProfile.RigType = string.Empty;
            CurrentRigProfile.RkbElevation = 0;
            CurrentRigProfile.CasingHeadElevation = 0;

            ToastNotificationService.Instance.ShowInfo("Rig Profile reset to defaults");
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
