using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ClosedXML.Excel;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Services;
using ProjectReport.ViewModels;

namespace ProjectReport.Modules.RigProfile.ViewModels
{
    /// <summary>
    /// ViewModel for the Rig Profile module, managing data and operations for rig configuration.
    /// </summary>
    public class RigProfileViewModel : BaseViewModel
    {
        private ProjectReport.Models.Rig.RigProfile _currentRigProfile;
        private readonly WellContextService _contextService;
        private readonly HydraulicsCalculationService _hydraulicsService;
        private Well? _currentWell;
        private readonly List<CatalogItem> _catalog = new();
        private readonly List<SolidControlCatalogItem> _solidControlCatalog = new()
        {
            new() { Style = "Scalper", Manufacturer = "Derrick", Model = "FLC 500 Scalper" },
            new() { Style = "Scalper", Manufacturer = "NOV Brandt", Model = "VSM 300 Scalper" },
            new() { Style = "Scalper", Manufacturer = "GN Solids Control", Model = "GNZS703 Series Scalper" },

            new() { Style = "Shaker", Manufacturer = "Derrick", Model = "Hyperpool" },
            new() { Style = "Shaker", Manufacturer = "Derrick", Model = "FLC 500 Series" },
            new() { Style = "Shaker", Manufacturer = "Derrick", Model = "FLC 2000 Series" },
            new() { Style = "Shaker", Manufacturer = "NOV Brandt", Model = "King Cobra" },
            new() { Style = "Shaker", Manufacturer = "NOV Brandt", Model = "VSM 300 Series" },
            new() { Style = "Shaker", Manufacturer = "MI-SWACO", Model = "Mongoose PT" },
            new() { Style = "Shaker", Manufacturer = "MI-SWACO", Model = "MD-3" },
            new() { Style = "Shaker", Manufacturer = "GN Solids Control", Model = "GNZS703 Series" },
            new() { Style = "Shaker", Manufacturer = "GN Solids Control", Model = "GNZS594 Series" },

            new() { Style = "Mud Cleaner", Manufacturer = "Derrick", Model = "FLC 503/504" },
            new() { Style = "Mud Cleaner", Manufacturer = "NOV Brandt", Model = "Mud King Combo" },
            new() { Style = "Mud Cleaner", Manufacturer = "MI-SWACO", Model = "Mongoose Combo" },
            new() { Style = "Mud Cleaner", Manufacturer = "GN Solids Control", Model = "GNZJ703 Series" },

            new() { Style = "Desander", Manufacturer = "Derrick", Model = "FLC Series (10\" cones)" },
            new() { Style = "Desander", Manufacturer = "NOV Brandt", Model = "10\" Hydrocyclone" },
            new() { Style = "Desander", Manufacturer = "MI-SWACO", Model = "DC-10" },
            new() { Style = "Desander", Manufacturer = "GN Solids Control", Model = "GN Hydrocyclone Desander" },

            new() { Style = "Desilter", Manufacturer = "Derrick", Model = "FLC Series (4\" cones)" },
            new() { Style = "Desilter", Manufacturer = "NOV Brandt", Model = "4\" Hydrocyclone" },
            new() { Style = "Desilter", Manufacturer = "MI-SWACO", Model = "DC-4" },
            new() { Style = "Desilter", Manufacturer = "GN Solids Control", Model = "GN Hydrocyclone Desilter" },

            new() { Style = "Centrifuge", Manufacturer = "Derrick", Model = "DE-1000" },
            new() { Style = "Centrifuge", Manufacturer = "Derrick", Model = "DE-7200" },
            new() { Style = "Centrifuge", Manufacturer = "NOV Brandt", Model = "HS-3400" },
            new() { Style = "Centrifuge", Manufacturer = "NOV Brandt", Model = "VSM Decanter" },
            new() { Style = "Centrifuge", Manufacturer = "MI-SWACO", Model = "CD-500" },
            new() { Style = "Centrifuge", Manufacturer = "MI-SWACO", Model = "CD-600" },
            new() { Style = "Centrifuge", Manufacturer = "GN Solids Control", Model = "GNLW363" },
            new() { Style = "Centrifuge", Manufacturer = "GN Solids Control", Model = "GNLW452" }
        };
        private readonly List<RigPump> _pumpCatalog = new()
        {
            new() { Model = "BOMCO F-1300", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "BOMCO F-1600", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "BOMCO F-1600HL", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "BOMCO F-2200", PumpType = "Triplex", StrokeLength = 14, MaxStrokeRate = 105 },
            new() { Model = "BOMCO F-2200HL", PumpType = "Triplex", StrokeLength = 14, MaxStrokeRate = 105 },
            new() { Model = "BOSS P-275/310", PumpType = "Triplex", StrokeLength = 8, RodSize = 5, MaxStrokeRate = 175 },
            new() { Model = "BOSS P-550", PumpType = "Triplex" },
            new() { Model = "Drillmec 12T-1600", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "Drillmec 14T-2200", PumpType = "Triplex", StrokeLength = 14, MaxStrokeRate = 105 },
            new() { Model = "Emsco F-1000", PumpType = "Triplex", StrokeLength = 10, MaxStrokeRate = 140 },
            new() { Model = "Emsco F-1300", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "Emsco F-1600", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "Emsco F-2200", PumpType = "Triplex", StrokeLength = 14, MaxStrokeRate = 105 },
            new() { Model = "Emsco F-500", PumpType = "Triplex", StrokeLength = 7.5, MaxStrokeRate = 165 },
            new() { Model = "Emsco F-800", PumpType = "Triplex", StrokeLength = 9, MaxStrokeRate = 150 },
            new() { Model = "Gardner Denver PZ-10", PumpType = "Triplex", StrokeLength = 10, RodSize = 7, MaxStrokeRate = 115 },
            new() { Model = "Gardner Denver PZ-11", PumpType = "Triplex", StrokeLength = 11, RodSize = 7, MaxStrokeRate = 115 },
            new() { Model = "Gardner Denver PZ-11 Hi-Flow", PumpType = "Triplex", StrokeLength = 11, RodSize = 8, MaxStrokeRate = 115 },
            new() { Model = "Gardner Denver PZ-1600", PumpType = "Triplex", StrokeLength = 11, MaxStrokeRate = 115 },
            new() { Model = "Gardner Denver PZ-2000", PumpType = "Triplex", StrokeLength = 11, MaxStrokeRate = 115 },
            new() { Model = "Gardner Denver PZ-2400", PumpType = "Triplex", StrokeLength = 14, MaxStrokeRate = 105 },
            new() { Model = "Gardner Denver PZ-7", PumpType = "Triplex", StrokeLength = 7, RodSize = 7, MaxStrokeRate = 145 },
            new() { Model = "Gardner Denver PZ-8", PumpType = "Triplex", StrokeLength = 8, RodSize = 7, MaxStrokeRate = 145 },
            new() { Model = "Gardner Denver PZ-9", PumpType = "Triplex", StrokeLength = 9, RodSize = 7, MaxStrokeRate = 130 },
            new() { Model = "Halliburton HQ-2000", PumpType = "Triplex" },
            new() { Model = "Halliburton HT-400", PumpType = "Triplex", StrokeLength = 8 },
            new() { Model = "Honghua HHF-1600", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "Honghua HHF-2200HL", PumpType = "Triplex", StrokeLength = 14, MaxStrokeRate = 105 },
            new() { Model = "Loadmaster LSF-1300", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "Loadmaster LSF-1600", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "Nabors Rig Pump 1600HP", PumpType = "Triplex", StrokeLength = 12, RodSize = 7, MaxStrokeRate = 120 },
            new() { Model = "Nabors Rig Pump 2200HP", PumpType = "Triplex", StrokeLength = 14, RodSize = 9, MaxStrokeRate = 105 },
            new() { Model = "National JWS-165-L", PumpType = "Triplex", MaxStrokeRate = 165 },
            new() { Model = "National JWS-340", PumpType = "Triplex", MaxStrokeRate = 340 },
            new() { Model = "National JWS-400", PumpType = "Triplex", MaxStrokeRate = 400 },
            new() { Model = "NOV 10-P-130", PumpType = "Triplex", StrokeLength = 10, RodSize = 6.75, MaxStrokeRate = 140 },
            new() { Model = "NOV 12-P-160", PumpType = "Triplex", StrokeLength = 12, RodSize = 7.25, MaxStrokeRate = 120 },
            new() { Model = "NOV 14-P-220", PumpType = "Triplex", StrokeLength = 14, RodSize = 9, MaxStrokeRate = 105 },
            new() { Model = "NOV 7-P-50", PumpType = "Triplex", MaxStrokeRate = 180 },
            new() { Model = "NOV 8-P-80", PumpType = "Triplex", MaxStrokeRate = 160 },
            new() { Model = "NOV 9-P-100", PumpType = "Triplex", MaxStrokeRate = 154 },
            new() { Model = "NOV F-1000 / FD-1000", PumpType = "Triplex", StrokeLength = 10, MaxStrokeRate = 140 },
            new() { Model = "Oilwell A-1100-PT", PumpType = "Triplex", StrokeLength = 12 },
            new() { Model = "Oilwell A-1400-PT", PumpType = "Triplex", StrokeLength = 12 },
            new() { Model = "Oilwell A-1700-PT", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 150 },
            new() { Model = "Oilwell A-650-PT", PumpType = "Triplex", StrokeLength = 12 },
            new() { Model = "Oilwell A-850-PT", PumpType = "Triplex", StrokeLength = 12 },
            new() { Model = "Pioneer Rig Pump 2200HP", PumpType = "Triplex", StrokeLength = 14, RodSize = 9, MaxStrokeRate = 105 },
            new() { Model = "Weatherford MP10", PumpType = "Triplex", StrokeLength = 10, MaxStrokeRate = 140 },
            new() { Model = "Weatherford MP13", PumpType = "Triplex", StrokeLength = 12, MaxStrokeRate = 120 },
            new() { Model = "Weatherford MP16", PumpType = "Triplex", StrokeLength = 12, RodSize = 7, MaxStrokeRate = 120 },
            new() { Model = "Weatherford MP250", PumpType = "Triplex", StrokeLength = 5, MaxStrokeRate = 310 },
            new() { Model = "Wheatley 7024", PumpType = "Duplex", StrokeLength = 6.125, MaxStrokeRate = 90 }
        };

        public RigProfileViewModel()
        {
            _contextService = WellContextService.Instance;
            _hydraulicsService = new HydraulicsCalculationService();
            _contextService.WellChanged += OnWellChanged;

            _currentRigProfile = new ProjectReport.Models.Rig.RigProfile();

            if (_contextService.CurrentWell != null)
                LoadRigProfile(_contextService.CurrentWell);

            AvailableTypes = new ObservableCollection<string>();
            AvailableManufacturers = new ObservableCollection<string>();
            AvailableModels = new ObservableCollection<string>();
            AvailablePitShapes = new ObservableCollection<string> { "Rectangular", "Cylindrical", "Oval", "Other" };

            // Lista de estilos para la columna "Style" (tal como pediste)
            AvailableStyles = new ObservableCollection<string>
            {
                "Scalper",
                "Shaker",
                "Mud Cleaner",
                "Desilter",
                "Desander",
                "Centrifuge"
            };

            _testDensity = 10.0;
            _testGpm = 500.0;

            LoadCatalogFromExcel();
            InitializeCatalogCollections();
            InitializePumpCatalog();

            SelectedSurfaceType = AvailableTypes.FirstOrDefault() ?? string.Empty;

            EnsureSurfaceDefaults();
            EnsureServiceLineDefaults();

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            SaveAndReturnCommand = new RelayCommand(async _ => await SaveAndReturnAsync());
            ResetToDefaultCommand = new RelayCommand(_ => ResetToDefault());
            ClearPumpFiltersCommand = new RelayCommand(_ => ClearPumpFilters());
            EditSelectedPumpCommand = new RelayCommand(p => EditSelectedPump(p as RigPump));
            RemoveSelectedPumpCommand = new RelayCommand(p => RemoveSelectedPump(p as RigPump));
            AddPumpCommand = new RelayCommand(_ => AddSelectedPump(), _ => CanAddPump);

            // Listen for pit changes
            Pits.CollectionChanged += (s, e) => PublishPits();
        }

        private void OnWellChanged(object? sender, Well well) => LoadRigProfile(well);

        private void LoadRigProfile(Well well)
        {
            _currentWell = well;
            _currentRigProfile = well.RigProfile ?? new ProjectReport.Models.Rig.RigProfile();
            InitializeSolidsControlRows();
            EnsureSurfaceDefaults();
            EnsureServiceLineDefaults();
            PublishPits();
        }

        public ProjectReport.Models.Rig.RigProfile CurrentRigProfile
        {
            get => _currentRigProfile;
            set
            {
                if (SetProperty(ref _currentRigProfile, value))
                {
                    OnPropertyChanged(nameof(SurfaceEquipment));
                    OnPropertyChanged(nameof(ServiceLine));
                    OnPropertyChanged(nameof(Pumps));
                    OnPropertyChanged(nameof(SelectedPumps));
                    OnPropertyChanged(nameof(SolidsControl));
                    OnPropertyChanged(nameof(Pits));
                    OnPropertyChanged(nameof(TotalSurfaceLoss));

                    if (Pits != null)
                    {
                        Pits.CollectionChanged -= (s, e) => PublishPits();
                        Pits.CollectionChanged += (s, e) => PublishPits();
                        PublishPits();
                    }
                }
            }
        }

        private void PublishPits()
        {
            if (Pits != null)
            {
                _contextService.PublishProjectReport.Models.Rig.RigProfilePits(Pits.Where(p => p.IsActive).ToList());
            }
        }

        // Collection wrappers
        public ObservableCollection<RigSurfaceEquipment> SurfaceEquipment => CurrentProjectReport.Models.Rig.RigProfile?.SurfaceEquipment ?? new ObservableCollection<RigSurfaceEquipment>();
        public ObservableCollection<RigSurfaceEquipment> ServiceLine => CurrentProjectReport.Models.Rig.RigProfile?.ServiceLine ?? new ObservableCollection<RigSurfaceEquipment>();
        public ObservableCollection<RigPump> Pumps => CurrentProjectReport.Models.Rig.RigProfile?.Pumps ?? new ObservableCollection<RigPump>();
        public ObservableCollection<RigSolidsControl> SolidsControl => CurrentProjectReport.Models.Rig.RigProfile?.SolidsControl ?? new ObservableCollection<RigSolidsControl>();
        public ObservableCollection<RigPit> Pits => CurrentProjectReport.Models.Rig.RigProfile?.Pits ?? new ObservableCollection<RigPit>();
        public ObservableCollection<RigPump> SelectedPumps => Pumps;

        private ObservableCollection<RigPump> _filteredPumpCatalog = new();
        public ObservableCollection<RigPump> FilteredPumpCatalog
        {
            get => _filteredPumpCatalog;
            private set => SetProperty(ref _filteredPumpCatalog, value);
        }

        private RigPump? _selectedCatalogPump;
        public RigPump? SelectedCatalogPump
        {
            get => _selectedCatalogPump;
            set
            {
                if (SetProperty(ref _selectedCatalogPump, value) && value != null)
                    PopulateEditPumpFromCatalog(value);
            }
        }

        private RigPump? _selectedPump;
        public RigPump? SelectedPump
        {
            get => _selectedPump;
            set => SetProperty(ref _selectedPump, value);
        }

        private RigPump _editPump = new();
        public RigPump EditPump
        {
            get => _editPump;
            set
            {
                if (SetProperty(ref _editPump, value))
                    HookEditPump();
            }
        }

        private string _pumpSearchText = string.Empty;
        public string PumpSearchText
        {
            get => _pumpSearchText;
            set
            {
                if (SetProperty(ref _pumpSearchText, value))
                    ApplyPumpFilters();
            }
        }

        public ObservableCollection<string> PumpTypes { get; } = new() { "All", "Triplex", "Duplex" };

        private string _selectedPumpTypeFilter = "All";
        public string SelectedPumpTypeFilter
        {
            get => _selectedPumpTypeFilter;
            set
            {
                if (SetProperty(ref _selectedPumpTypeFilter, value))
                    ApplyPumpFilters();
            }
        }

        private string _pumpValidationMessage = string.Empty;
        public string PumpValidationMessage
        {
            get => _pumpValidationMessage;
            private set => SetProperty(ref _pumpValidationMessage, value);
        }

        public bool CanAddPump =>
            !string.IsNullOrWhiteSpace(EditPump.Model) &&
            EditPump.MaxLinerSize > 0;

        private bool _isPumpsCatalogExpanded = true;
        public bool IsPumpsCatalogExpanded
        {
            get => _isPumpsCatalogExpanded;
            set => SetProperty(ref _isPumpsCatalogExpanded, value);
        }

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
            set
            {
                if (SetProperty(ref _selectedSurfaceType, value))
                {
                    // Auto-populate surface equipment when a type is selected
                    PopulateSurfaceEquipmentByType(value);
                }
            }
        }

        // Mapping data for Surface Equipment Types (from reference table)
        private static readonly Dictionary<string, (double StandPipeID, double StandPipeLen,
                                                    double DrillingHoseID, double DrillingHoseLen,
                                                    double SwivelID, double SwivelLen,
                                                    double KellyID, double KellyLen)> SurfaceTypeMapping =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Type 1: Stand Pipe(ID=3", Len=40"), Drilling Hose(ID=2", Len=45"), Swivel(ID=4", Len=2"), Kelly(ID=40", Len=2.25")
                { "Type 1", (3, 40, 2, 45, 4, 2, 40, 2.25) },

                // Type 2: Stand Pipe(ID=3.5", Len=40"), Drilling Hose(ID=2.5", Len=55"), Swivel(ID=5", Len=2.25"), Kelly(ID=40", Len=3.25")
                { "Type 2", (3.5, 40, 2.5, 55, 5, 2.25, 40, 3.25) },

                // Type 3: Stand Pipe(ID=4", Len=45"), Drilling Hose(ID=3", Len=55"), Swivel(ID=5", Len=2.25"), Kelly(ID=40", Len=3.25")
                { "Type 3", (4, 45, 3, 55, 5, 2.25, 5, 2.25, 40, 3.25) },

                // Type 4: Stand Pipe(ID=4", Len=45"), Drilling Hose(ID=3", Len=65"), Swivel(ID=6", Len=3"), Kelly(ID=40", Len=4")
                { "Type 4", (4, 45, 3, 65, 6, 3, 40, 4) }
            };

        private void PopulateSurfaceEquipmentByType(string selectedType)
        {
            if (string.IsNullOrWhiteSpace(selectedType) || !SurfaceTypeMapping.ContainsKey(selectedType))
                return;

            if (SurfaceEquipment == null || SurfaceEquipment.Count != 4)
                return;

            var (standPipeID, standPipeLen, drillingHoseID, drillingHoseLen,
                 swivelID, swivelLen, kellyID, kellyLen) = SurfaceTypeMapping[selectedType];

            // Stand Pipe - convert from inches to feet (divide by 12)
            if (SurfaceEquipment.Count > 0)
            {
                SurfaceEquipment[0].InternalDiameter = standPipeID;
                SurfaceEquipment[0].Length = standPipeLen / 12.0;
            }

            // Drilling Hose - convert from inches to feet
            if (SurfaceEquipment.Count > 1)
            {
                SurfaceEquipment[1].InternalDiameter = drillingHoseID;
                SurfaceEquipment[1].Length = drillingHoseLen / 12.0;
            }

            // Swivel / Top Drive - convert from inches to feet
            if (SurfaceEquipment.Count > 2)
            {
                SurfaceEquipment[2].InternalDiameter = swivelID;
                SurfaceEquipment[2].Length = swivelLen / 12.0;
            }

            // Kelly - Special handling: ID is likely correct, but Length should use raw value
            // Based on the user's note, Kelly appears to have swapped data
            // ID = 40 should be the length, but keeping as-is per specification
            if (SurfaceEquipment.Count > 3)
            {
                SurfaceEquipment[3].InternalDiameter = kellyID;
                // Kelly length conversion: convert to feet
                SurfaceEquipment[3].Length = kellyLen / 12.0;
            }
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
                if (_currentWell?.ProjectReport.Models.Rig.RigProfile == null) return 0;
                return _hydraulicsService.CalculateTotalSurfacePressureLoss(_currentWell.ProjectReport.Models.Rig.RigProfile, TestDensity, TestGpm);
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
            // Initialize Surface Equipment types with predefined Type 1-4 mapping
            AvailableTypes = new ObservableCollection<string> { "Type 1", "Type 2", "Type 3", "Type 4" };
            AvailableManufacturers = new ObservableCollection<string>(_catalog.Select(x => x.Manufacturer).Distinct().OrderBy(x => x));

            if (AvailableModels == null)
                AvailableModels = new ObservableCollection<string>();
        }

        private void InitializePumpCatalog()
        {
            EditPump = new RigPump();
            HookEditPump();
            ApplyPumpFilters();
        }

        private void HookEditPump()
        {
            if (EditPump == null) return;
            EditPump.PropertyChanged -= EditPump_PropertyChanged;
            EditPump.PropertyChanged += EditPump_PropertyChanged;
            OnPropertyChanged(nameof(CanAddPump));
        }

        private void EditPump_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PumpValidationMessage = string.Empty;
            OnPropertyChanged(nameof(CanAddPump));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private void ApplyPumpFilters()
        {
            IEnumerable<RigPump> query = _pumpCatalog;

            if (!string.IsNullOrWhiteSpace(PumpSearchText))
            {
                query = query.Where(p =>
                    !string.IsNullOrWhiteSpace(p.Model) &&
                    p.Model.Contains(PumpSearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedPumpTypeFilter) &&
                !string.Equals(SelectedPumpTypeFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => string.Equals(p.PumpType, SelectedPumpTypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            FilteredPumpCatalog = new ObservableCollection<RigPump>(
                query.OrderBy(p => p.Model).Select(ClonePump));
        }

        private void PopulateEditPumpFromCatalog(RigPump selected)
        {
            var maxLiner = EditPump?.MaxLinerSize ?? 0;
            EditPump = ClonePump(selected);
            EditPump.MaxLinerSize = maxLiner;
        }

        private static RigPump ClonePump(RigPump source)
        {
            return new RigPump
            {
                No = source.No,
                Model = source.Model,
                PumpType = source.PumpType,
                StrokeLength = source.StrokeLength,
                RodSize = source.RodSize,
                MaxLinerSize = source.MaxLinerSize,
                MaxStrokeRate = source.MaxStrokeRate,
                PumpName = source.PumpName,
                LinerSize = source.LinerSize,
                Efficiency = source.Efficiency
            };
        }

        private void ClearPumpFilters()
        {
            PumpSearchText = string.Empty;
            SelectedPumpTypeFilter = "All";
            ApplyPumpFilters();
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
        public ICommand AddPumpCommand { get; }
        public ICommand RemovePumpCommand => new RelayCommand(p => RemovePump(p as RigPump));
        public ICommand EditSelectedPumpCommand { get; }
        public ICommand RemoveSelectedPumpCommand { get; }
        public ICommand ClearPumpFiltersCommand { get; }
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

        private void AddSelectedPump()
        {
            if (!CanAddPump)
            {
                PumpValidationMessage = "Max Liner Size is required.";
                return;
            }

            int nextNo = (SelectedPumps?.Count ?? 0) + 1;
            var toAdd = ClonePump(EditPump);
            toAdd.No = nextNo;
            SelectedPumps?.Add(toAdd);

            PumpValidationMessage = string.Empty;
            EditPump = ClonePump(toAdd);
            SelectedPump = toAdd;
        }

        private void RemovePump(RigPump? item)
        {
            if (item != null && Pumps != null)
            {
                Pumps.Remove(item);
                Renumber(Pumps);
            }
        }

        private void EditSelectedPump(RigPump? item)
        {
            if (item == null) return;
            EditPump = ClonePump(item);
            SelectedPump = item;
        }

        private void RemoveSelectedPump(RigPump? item)
        {
            if (item == null || SelectedPumps == null) return;
            SelectedPumps.Remove(item);
            Renumber(SelectedPumps);
            if (ReferenceEquals(SelectedPump, item))
                SelectedPump = null;
        }

        private void AddSolidsControl()
        {
            int nextNo = (SolidsControl?.Count ?? 0) + 1;
            SolidsControl?.Add(new RigSolidsControl
            {
                No = nextNo,
                ManufacturerOptions = new ObservableCollection<string>(),
                ModelOptions = new ObservableCollection<string>()
            });
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

                _currentWell.ProjectReport.Models.Rig.RigProfile = CurrentProjectReport.Models.Rig.RigProfile;

                _currentWell.RigName = CurrentProjectReport.Models.Rig.RigProfile.RigName;
                _currentWell.Contractor = CurrentProjectReport.Models.Rig.RigProfile.Contractor;
                _currentWell.RigType = CurrentProjectReport.Models.Rig.RigProfile.RigType;

                // Persist rig profile to the database via WellContextService (SQLite)
                if (_currentWell != null)
                {
                    // Ensure the current well contains the updated ProjectReport.Models.Rig.RigProfile (already set above)
                    _currentWell.ProjectReport.Models.Rig.RigProfile = CurrentProjectReport.Models.Rig.RigProfile;
                    _currentWell.ContextService = WellContextService.Instance;
                    WellContextService.Instance.CurrentWell = _currentWell;

                    await WellContextService.Instance.SaveCurrentWell();
                    ToastNotificationService.Instance.ShowSuccess("Rig Profile saved to database successfully");
                    PublishPits();
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
                ManufacturerOptions = new ObservableCollection<string>(),
                ModelOptions = new ObservableCollection<string>(),
                DesilterNumberOfCones = 0,
                DesilterConeSize = 0.0,
                DesanderNumberOfCones = 0,
                DesanderConeSize = 0.0
            });
            var defaultItem = SolidsControl?.FirstOrDefault();
            if (defaultItem != null)
            {
                ApplySolidControlStyleSelection(defaultItem);
                ApplySolidControlManufacturerSelection(defaultItem);
                ApplySolidControlModelSelection(defaultItem);
            }

            CurrentProjectReport.Models.Rig.RigProfile.RigName = string.Empty;
            CurrentProjectReport.Models.Rig.RigProfile.Contractor = string.Empty;
            CurrentProjectReport.Models.Rig.RigProfile.RigType = string.Empty;
            CurrentProjectReport.Models.Rig.RigProfile.RkbElevation = 0;
            CurrentProjectReport.Models.Rig.RigProfile.CasingHeadElevation = 0;

            SelectedSurfaceType = AvailableTypes.FirstOrDefault() ?? string.Empty;

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

        public void UpdateAvailableModels(string type, string manufacturer)
        {
            if (AvailableModels == null) return;
            AvailableModels.Clear();
            foreach (var model in GetSolidControlModels(type, manufacturer))
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
                // tambi�n asignar al campo expl�cito de "Cap flow (gpm)"
                item.CapFlowGpm = match.Gpm;
            }
        }

        public IEnumerable<string> GetSolidControlManufacturers(string style)
        {
            if (string.IsNullOrWhiteSpace(style)) return Enumerable.Empty<string>();
            return _solidControlCatalog
                .Where(x => string.Equals(x.Style, style, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Manufacturer)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x);
        }

        public IEnumerable<string> GetSolidControlModels(string style, string manufacturer)
        {
            if (string.IsNullOrWhiteSpace(style) || string.IsNullOrWhiteSpace(manufacturer))
                return Enumerable.Empty<string>();

            return _solidControlCatalog
                .Where(x =>
                    string.Equals(x.Style, style, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Manufacturer, manufacturer, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Model)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x);
        }

        public void ApplySolidControlStyleSelection(RigSolidsControl item)
        {
            if (item == null) return;

            item.Type = item.Style;

            var manufacturers = GetSolidControlManufacturers(item.Style).ToList();
            item.ManufacturerOptions = new ObservableCollection<string>(manufacturers);

            if (!manufacturers.Any(m => string.Equals(m, item.Manufacturer, StringComparison.OrdinalIgnoreCase)))
                item.Manufacturer = string.Empty;

            item.Model = string.Empty;
            item.ModelOptions = new ObservableCollection<string>();
        }

        public void ApplySolidControlManufacturerSelection(RigSolidsControl item)
        {
            if (item == null) return;

            var models = GetSolidControlModels(item.Style, item.Manufacturer).ToList();
            item.ModelOptions = new ObservableCollection<string>(models);

            if (!models.Any(m => string.Equals(m, item.Model, StringComparison.OrdinalIgnoreCase)))
                item.Model = string.Empty;
        }

        public void ApplySolidControlModelSelection(RigSolidsControl item)
        {
            if (item == null) return;
            UpdateSolidsControlSpecs(item);
        }

        private void InitializeSolidsControlRows()
        {
            if (SolidsControl == null) return;

            foreach (var item in SolidsControl)
            {
                if (item == null) continue;
                item.ManufacturerOptions ??= new ObservableCollection<string>();
                item.ModelOptions ??= new ObservableCollection<string>();
                ApplySolidControlStyleSelection(item);
                if (!string.IsNullOrWhiteSpace(item.Manufacturer))
                {
                    ApplySolidControlManufacturerSelection(item);
                    if (!string.IsNullOrWhiteSpace(item.Model))
                        ApplySolidControlModelSelection(item);
                }
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

        public class SolidControlCatalogItem
        {
            public string Style { get; set; } = "";
            public string Manufacturer { get; set; } = "";
            public string Model { get; set; } = "";
        }
    }
}