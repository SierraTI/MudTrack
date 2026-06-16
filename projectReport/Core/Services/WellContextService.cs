using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Models.Inventory;
using ProjectReport.Core.Data;

namespace ProjectReport.Services
{
    /// <summary>
    /// Singleton service to hold the shared state of the application (Current Well, Project Context).
    /// Acts as the 'Thread' connecting all modules.
    /// </summary>
    public class WellContextService
    {
        private static WellContextService? _instance;
        public static WellContextService Instance => _instance ??= new WellContextService();

        private readonly WellRepository _wellRepo;
        private readonly ReportRepository _reportRepo;
        private readonly CatalogRepository _catalogRepo;
        private readonly WellboreGeometryRepository _geometryRepo;
        private readonly DrillStringRepository _drillStringRepo;
        private readonly SurveyRepository _surveyRepo;
        private readonly EngineeringRepository _engineeringRepo;
        private readonly RigProfileRepository _rigProfileRepo;
        private readonly DatabaseService _db;

        public ObservableCollection<string> FluidCatalog { get; } = new();

        private WellContextService() 
        { 
            _db = new DatabaseService();
            _wellRepo = new WellRepository(_db);
            _reportRepo = new ReportRepository(_db);
            _catalogRepo = new CatalogRepository(_db);
            _geometryRepo = new WellboreGeometryRepository(_db);
            _drillStringRepo = new DrillStringRepository(_db);
            _surveyRepo = new SurveyRepository(_db);
            _engineeringRepo = new EngineeringRepository(_db);
            _rigProfileRepo = new RigProfileRepository(_db);

            LoadCatalog();
        }

        private void LoadCatalog()
        {
            try
            {
                var fluids = _catalogRepo.GetFluidNames();
                FluidCatalog.Clear();
                foreach (var f in fluids) FluidCatalog.Add(f);
            }
            catch { /* Handle DB connection issues gracefully */ }
        }

        private Project? _currentProject;
        private Well? _currentWell;
        private Report? _currentReport; // Assuming CurrentReport is a new field based on the SaveCurrentWell logic
        private double _currentDepth;
        private double _currentFlowRate;
        private readonly Dictionary<string, bool> _stepCompletionStatus = new();
        private List<ChemicalItem> _currentSelectedChemicals = new();
        private IEnumerable<ProjectReport.Models.Geometry.Wellbore.WellboreComponent>? _lastGeometry;

        public event EventHandler<Well>? WellChanged;
        public event EventHandler<double>? DepthUpdated;
        public event EventHandler<double>? MudDensityUpdated;
        public event EventHandler<double>? FlowRateUpdated;
        public event EventHandler<ReportThermalDataEventArgs>? ReportThermalDataUpdated;
        public event EventHandler<GeometryDataUpdatedEventArgs>? GeometryDataUpdated;
        public event EventHandler<IEnumerable<ProjectReport.Models.Geometry.Wellbore.WellboreComponent>>? WellboreComponentsUpdated;
        public event Action<IEnumerable<ProjectReport.Models.Geometry.DrillString.DrillStringComponent>>? DrillStringUpdated;
        public event Action<IEnumerable<ProjectReport.Models.Geometry.Survey.SurveyPoint>>? SurveyUpdated;
        public event Action<IEnumerable<ProjectReport.Models.Geometry.ThermalGradient.ThermalGradientPoint>>? ThermalUpdated;
        public event Action<IEnumerable<ProjectReport.Models.Geometry.WellTest.WellTest>>? WellTestsUpdated;
        public event EventHandler<RigProfileUpdatedEventArgs>? RigProfileUpdated;
        public event EventHandler<ChemicalSelectionUpdatedEventArgs>? ChemicalSelectionUpdated;

        public IReadOnlyList<ChemicalItem> CurrentSelectedChemicals => _currentSelectedChemicals;

        public Project? CurrentProject
        {
            get => _currentProject;
            set => _currentProject = value;
        }

        public Well? CurrentWell
        {
            get => _currentWell;
            set
            {
                if (_currentWell != value)
                {
                    _currentWell = value;
                    if (_currentWell != null)
                    {
                        try
                        {
                            // Ensure well is persisted if it's new
                            if (_currentWell.Id <= 0) _wellRepo.SaveWell(_currentWell);
                            
                            // Load associated reports for this well
                            var reports = _reportRepo.GetReportsByWellId(_currentWell.Id);
                            _currentWell.Reports.Clear();
                            foreach (var r in reports) _currentWell.Reports.Add(r);

                            // Load engineering components
                            LoadEngineering(_currentWell);
                        }
                        catch (Exception ex)
                        {
                            // Handle DB connection issues gracefully (e.g., in unit tests or when DB is offline)
                            System.Diagnostics.Debug.WriteLine($"Database access failed in CurrentWell setter: {ex.Message}");
                        }
                    }
                    WellChanged?.Invoke(this, _currentWell!);
                }
            }
        }

        // Assuming CurrentReport is a new property based on the SaveCurrentWell logic
        public Report? CurrentReport
        {
            get => _currentReport;
            set
            {
                if (_currentReport != value)
                {
                    _currentReport = value;
                    if (_currentReport != null)
                    {
                        try
                        {
                            // Load technical details from SQL
                            _lastGeometry = _geometryRepo.LoadGeometry(_currentReport.Id);
                            
                            // Notify observers if necessary (ViewModels should re-sync when report changes)
                            if (_lastGeometry != null) WellboreComponentsUpdated?.Invoke(this, _lastGeometry);
                        }
                        catch (Exception ex)
                        {
                            // Handle DB connection issues gracefully (e.g., in unit tests or when DB is offline)
                            System.Diagnostics.Debug.WriteLine($"Database access failed in CurrentReport setter: {ex.Message}");
                        }
                    }
                }
            }
        }
    
        public IEnumerable<ProjectReport.Models.Geometry.Wellbore.WellboreComponent>? GetLoadedGeometry() => _lastGeometry;

        public async Task SaveCurrentWell()
        {
            if (CurrentWell != null)
            {
                _wellRepo.SaveWell(CurrentWell);

                // Engineering components (Well level)
                _drillStringRepo.SaveDrillString(CurrentWell.Id, CurrentWell.DrillStringComponents);
                _surveyRepo.SaveSurvey(CurrentWell.Id, CurrentWell.SurveyPoints);
                _engineeringRepo.SaveThermalGradient(CurrentWell.Id, CurrentWell.ThermalGradientPoints);
                _engineeringRepo.SaveWellTests(CurrentWell.Id, CurrentWell.WellTests);

                // Rig Profile
                if (CurrentWell.RigProfile != null)
                    _rigProfileRepo.SaveRigProfile(CurrentWell.Id, CurrentWell.RigProfile);
            }

            if (CurrentReport != null)
            {
                _reportRepo.SaveReport(CurrentWell!.Id, CurrentReport);

                if (_lastGeometry != null)
                    _geometryRepo.SaveGeometry(CurrentReport.Id, _lastGeometry);
            }
            await Task.Yield();
        }

        public double CurrentDepth
        {
            get => _currentDepth;
            set => _currentDepth = value;
        }

        public double CurrentFlowRate
        {
            get => _currentFlowRate;
            set => _currentFlowRate = value;
        }

        public void UpdateSystemDepth(double newMD)
        {
            if (CurrentWell != null)
            {
                CurrentWell.TotalMD = newMD;
                CurrentDepth = newMD;
                DepthUpdated?.Invoke(this, newMD);
            }
        }

        public List<Well> GetAllWells()
        {
            return _wellRepo.GetAllWells();
        }

        public void DeleteWell(int id)
        {
            _wellRepo.DeleteWell(id);
        }

        public void DeleteReport(int reportId)
        {
            _reportRepo.DeleteReport(reportId);
        }

        public void UpdateMudDensity(double density)
        {
            MudDensityUpdated?.Invoke(this, density);
        }

        public void UpdateFlowRate(double gpm)
        {
            CurrentFlowRate = gpm;
            FlowRateUpdated?.Invoke(this, gpm);
        }

        public void MarkStepComplete(string stepName)
        {
            _stepCompletionStatus[stepName] = true;
        }

        public bool IsStepComplete(string stepName)
        {
            return _stepCompletionStatus.ContainsKey(stepName) && _stepCompletionStatus[stepName];
        }

        public List<string> GetMissingSteps()
        {
            var requiredSteps = new[] { "Dashboard", "DailyReport", "WellboreGeometry", "DrillString", "Survey", "ThermalGradient", "WellTest" };
            return requiredSteps.Where(step => !IsStepComplete(step)).ToList();
        }

        public string? ValidateDepthConsistency(double wellboreBottomMD)
        {
            if (CurrentDepth > 0 && wellboreBottomMD > CurrentDepth)
            {
                return $"Error: Wellbore cannot be deeper than current drilling depth ({CurrentDepth:F0} ft)";
            }
            return null;
        }

        public void NotifyReportThermalDataUpdated(double? reportTVD, double? reportMaxBHT)
        {
            ReportThermalDataUpdated?.Invoke(this, new ReportThermalDataEventArgs(reportTVD, reportMaxBHT));
        }

        public void PublishGeometryData(double holeCapacity, double stringDisplacement, double stringInternalVolume, double annularVolume, double theoreticalWellbore)
        {
            GeometryDataUpdated?.Invoke(this, new GeometryDataUpdatedEventArgs(holeCapacity, stringDisplacement, stringInternalVolume, annularVolume, theoreticalWellbore));
        }

        public void PublishWellboreComponents(IEnumerable<ProjectReport.Models.Geometry.Wellbore.WellboreComponent> components)
        {
            _lastGeometry = components;
            WellboreComponentsUpdated?.Invoke(this, components);
        }

        public void PublishRigProfilePits(IList<RigPit> activePits)
        {
            RigProfileUpdated?.Invoke(this, new RigProfileUpdatedEventArgs(activePits));
        }

        public void PublishChemicalSelection(IList<ChemicalItem> selectedItems)
        {
            _currentSelectedChemicals = (selectedItems ?? new List<ChemicalItem>())
                .Where(i => i != null)
                .Select(i => new ChemicalItem
                {
                    Code = i.Code ?? string.Empty,
                    Name = i.Name ?? string.Empty,
                    Description = i.Description ?? string.Empty,
                    PhysicalState = i.PhysicalState ?? string.Empty,
                    Presentation = i.Presentation ?? string.Empty,
                    Quantity = i.Quantity,
                    Unit = i.Unit ?? string.Empty,
                    SG = i.SG,
                    Category = i.Category ?? string.Empty,
                    UnitPrice = i.UnitPrice,
                    IsSelected = i.IsSelected
                })
                .ToList();

            ChemicalSelectionUpdated?.Invoke(this, new ChemicalSelectionUpdatedEventArgs(_currentSelectedChemicals));
        }

        private void LoadEngineering(Well well)
        {
            // Drill String
            var drillString = _drillStringRepo.LoadDrillString(well.Id);
            well.DrillStringComponents.Clear();
            foreach (var c in drillString) well.DrillStringComponents.Add(c);
            DrillStringUpdated?.Invoke(well.DrillStringComponents);

            // Survey
            var surveys = _surveyRepo.LoadSurvey(well.Id);
            well.SurveyPoints.Clear();
            foreach (var s in surveys) well.SurveyPoints.Add(s);
            SurveyUpdated?.Invoke(well.SurveyPoints);

            // Thermal
            var thermals = _engineeringRepo.LoadThermalGradient(well.Id);
            well.ThermalGradientPoints.Clear();
            foreach (var t in thermals) well.ThermalGradientPoints.Add(t);
            ThermalUpdated?.Invoke(well.ThermalGradientPoints);

            // Tests
            var tests = _engineeringRepo.LoadWellTests(well.Id);
            well.WellTests.Clear();
            foreach (var t in tests) well.WellTests.Add(t);
            WellTestsUpdated?.Invoke(well.WellTests);

            // Rig Profile
            var rigProfile = _rigProfileRepo.LoadRigProfile(well.Id);
            if (rigProfile != null)
                well.RigProfile = rigProfile;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT ARGS
    // ─────────────────────────────────────────────────────────────

    public class ReportThermalDataEventArgs : EventArgs
    {
        public double? ReportTVD { get; }
        public double? ReportMaxBHT { get; }

        public ReportThermalDataEventArgs(double? reportTVD, double? reportMaxBHT)
        {
            ReportTVD = reportTVD;
            ReportMaxBHT = reportMaxBHT;
        }
    }

    public class GeometryDataUpdatedEventArgs : EventArgs
    {
        public double HoleCapacity { get; }
        public double StringDisplacement { get; }
        public double StringInternalVolume { get; }
        public double AnnularVolume { get; }
        public double TheoreticalWellbore { get; }

        public GeometryDataUpdatedEventArgs(double holeCapacity, double stringDisplacement, double stringInternalVolume, double annularVolume, double theoreticalWellbore)
        {
            HoleCapacity = holeCapacity;
            StringDisplacement = stringDisplacement;
            StringInternalVolume = stringInternalVolume;
            AnnularVolume = annularVolume;
            TheoreticalWellbore = theoreticalWellbore;
        }
    }

    public class RigProfileUpdatedEventArgs : EventArgs
    {
        public IList<RigPit> ActivePits { get; }
        public RigProfileUpdatedEventArgs(IList<RigPit> activePits) => ActivePits = activePits;
    }

    public class ChemicalSelectionUpdatedEventArgs : EventArgs
    {
        public IList<ChemicalItem> SelectedItems { get; }
        public ChemicalSelectionUpdatedEventArgs(IList<ChemicalItem> selectedItems) => SelectedItems = selectedItems;
    }
}
