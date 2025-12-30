using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Services;
using ProjectReport.Services.Wellbore;
using ProjectReport.Services.Survey;
using ProjectReport.Views.Geometry;
using ProjectReport.Services.DrillString;
using ProjectReport.ViewModels.Geometry.ThermalGradient;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Windows.Media; // added for Brushes

namespace ProjectReport.ViewModels.Geometry
{
    public class GeometryViewModel : BaseViewModel
    {
        private readonly GeometryCalculationService _geometryService;
        private readonly GeometryValidationService _validationService; // validation service
        private readonly DataPersistenceService _dataService;
        private readonly ThermalGradientService _thermalService;
        private readonly SurveyCalculationService _surveyCalculationService; // Survey trajectory calculations
        private const double DepthTolerance = 0.01;
        private SeriesCollection _surveySeriesCollection = new();
        private SeriesCollection _safetySeriesCollection = new();
        private SeriesCollection _lotSeriesCollection = new();
        private Well? _currentWell; // Reference to the current well being edited
        private string _wellName = string.Empty;
        private string _reportNumber = string.Empty;
        private string _operator = string.Empty;
        private string _location = string.Empty;
        private string _rigName = string.Empty;
        private int _selectedTabIndex;
        private bool _depthOverrunToastShown;
        private string _drillStringDepthErrorMessage = string.Empty;
        private string _bhaWarningMessage = string.Empty;
        private string _bhaInsertPosition = "Bottom";
        private readonly List<string> _bhaInsertPositions = new() { "Top", "Bottom" };
        
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public GeometryViewModel(GeometryCalculationService geometryService, DataPersistenceService dataService, ThermalGradientService thermalService)
        {
            _geometryService = geometryService ?? throw new ArgumentNullException(nameof(geometryService));
            _validationService = new GeometryValidationService(); // new instance
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _thermalService = thermalService ?? throw new ArgumentNullException(nameof(thermalService));
            _surveyCalculationService = new SurveyCalculationService(); // Initialize survey calculation service

            // Initialize Sub-ViewModels
            ThermalGradientViewModel = new ThermalGradientViewModel(_thermalService);
            
            // Connect to Global Context
            WellContextService.Instance.WellChanged += OnWellContextChanged;
            WellContextService.Instance.DepthUpdated += OnGlobalDepthUpdated;
            
            // Initialize collections
            WellboreComponents = new ObservableCollection<WellboreComponent>();
            DrillStringComponents = new ObservableCollection<DrillStringComponent>();
            SurveyPoints = new ObservableCollection<SurveyPoint>();
            WellTests = new ObservableCollection<WellTest>();
            AnnularVolumeDetails = new ObservableCollection<AnnularVolumeDetail>();

            // Initialize dropdown options
            // Include null for "Select..." state
            var sectionTypes = new List<WellboreSectionType?> { null };
            sectionTypes.AddRange(Enum.GetValues(typeof(WellboreSectionType)).Cast<WellboreSectionType?>());
            WellboreSectionTypes = new ObservableCollection<WellboreSectionType?>(sectionTypes);

            var stages = new List<WellboreStage?> { null };
            stages.AddRange(Enum.GetValues(typeof(WellboreStage)).Cast<WellboreStage?>());
            WellboreStages = new ObservableCollection<WellboreStage?>(stages);
            
            ComponentTypes = new ObservableCollection<ComponentType>(
                Enum.GetValues(typeof(ComponentType)).Cast<ComponentType>());


            WellTestTypes = new ObservableCollection<string> 
            { 
                "Leak Off", "Fracture gradient", "Pore pressure", "Integrity" 
            };

            // Subscribe to collection changes
            WellboreComponents.CollectionChanged += OnWellboreCollectionChanged;
            DrillStringComponents.CollectionChanged += OnDrillStringCollectionChanged;
            SurveyPoints.CollectionChanged += OnSurveyCollectionChanged;
            WellboreComponents.CollectionChanged += (s, e) => OnPropertyChanged(nameof(WellboreSectionNames));

            // Subscribe to property changes in components
            foreach (var component in WellboreComponents)
            {
                component.PropertyChanged += OnWellboreComponentChanged;
            }
            foreach (var component in DrillStringComponents)
            {
                component.PropertyChanged += OnDrillStringComponentChanged;
            }
            foreach (var point in SurveyPoints)
            {
                point.PropertyChanged += OnSurveyPointChanged;
            }

            InitializeSurveyChart();
            
            WellContextService.Instance.MudDensityUpdated += OnMudDensityUpdated;
            _currentMudWeight = 10.0; // Default
            SafetySeriesCollection = new SeriesCollection();

            WellTests.CollectionChanged += OnWellTestsCollectionChanged;
            foreach (var test in WellTests)
            {
                test.PropertyChanged += OnWellTestPropertyChanged;
            }
        }

        private void InitializeSurveyChart()
        {
            SurveySeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Trajectory (Vertical Section)",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Stroke = Brushes.SlateBlue,
                    Fill = Brushes.Transparent,
                    LabelPoint = point => $"VS: {point.X:N1} ft | TVD: {Math.Abs(point.Y):N1} ft"
                }
            };
            UpdateSurveyChart();
        }

        private void OnWellContextChanged(object? sender, Well well)
        {
            if (well != null && well.Id != (_currentWell?.Id ?? 0))
            {
                LoadWell(well);
            }
        }

        private void OnMudDensityUpdated(object? sender, double density)
        {
            CurrentMudWeight = density;
        }

        private void OnGlobalDepthUpdated(object? sender, double newMD)
        {
             // If we want to auto-extend the last wellbore section or just alert?
             // For now, let's just toast
             ToastNotificationService.Instance.ShowInfo($"Global Depth Updated to {newMD} ft");
        }



        // Dropdown options
        public ObservableCollection<WellboreSectionType?> WellboreSectionTypes { get; }
        public ObservableCollection<WellboreStage?> WellboreStages { get; }

        public ObservableCollection<ComponentType> ComponentTypes { get; }
        public ObservableCollection<string> WellTestTypes { get; }

        // Sub-ViewModels
        public ThermalGradientViewModel ThermalGradientViewModel { get; }
        
        // Wellbore section names for Well Test dropdown
        public ObservableCollection<string> WellboreSectionNames => 
            new ObservableCollection<string>(WellboreComponents.Select(w => w.Name).Where(n => !string.IsNullOrEmpty(n)));

        private bool _isProcessingCollectionChange = false;

        private void OnWellboreCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isProcessingCollectionChange) return;

            if (e.NewItems != null)
            {
                foreach (WellboreComponent component in e.NewItems)
                {
                    component.PropertyChanged += OnWellboreComponentChanged;
                    ValidateWellboreComponent(component);
                }
            }
            if (e.OldItems != null)
            {
                foreach (WellboreComponent component in e.OldItems)
                {
                    component.PropertyChanged -= OnWellboreComponentChanged;
                }
                
                // Renumber existing items logic (Rule: Renumber on Delete)
                RenumberWellboreSections();
            }
            
            // Re-validate all components after collection change (order may have changed)
            foreach (var component in WellboreComponents)
            {
                ValidateWellboreComponent(component);
            }
            
            RecalculateTotals();
        }

        private void RenumberWellboreSections()
        {
            _isProcessingCollectionChange = true;
            try
            {
                int idCounter = 1;
                foreach (var component in WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue))
                {
                    component.Id = idCounter++;
                }
                
                // Update next ID counter
                _nextWellboreId = idCounter;
            }
            finally
            {
                _isProcessingCollectionChange = false;
            }
        }

        private void OnDrillStringCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isProcessingCollectionChange) return;

            if (e.NewItems != null)
            {
                foreach (DrillStringComponent component in e.NewItems)
                {
                    component.PropertyChanged += OnDrillStringComponentChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (DrillStringComponent component in e.OldItems)
                {
                    component.PropertyChanged -= OnDrillStringComponentChanged;
                }
            }
            
            RenumberDrillStringSections();
            RecalculateTotals();
        }

        private void RenumberDrillStringSections()
        {
            _isProcessingCollectionChange = true;
            try
            {
                int idCounter = 1;
                // Drill string is typically top-down, so just order by list index effectively
                // But since it's an ObservableCollection, the index is the order.
                // If we want to accept drag-drop reordering, we should rely on the Collection order.
                foreach (var component in DrillStringComponents)
                {
                    component.Id = idCounter++;
                }
            }
            finally
            {
                _isProcessingCollectionChange = false;
            }
        }

        private void OnWellboreComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WellboreComponent.TopMD) || 
                e.PropertyName == nameof(WellboreComponent.BottomMD) ||
                e.PropertyName == nameof(WellboreComponent.ID) ||
                e.PropertyName == nameof(WellboreComponent.OD) ||
                e.PropertyName == nameof(WellboreComponent.SectionType) ||
                e.PropertyName == nameof(WellboreComponent.Washout))
            {
                if (sender is WellboreComponent component)
                {
                    // Determine previous component for context-aware calculation
                    var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
                    int index = sorted.IndexOf(component);
                    var prev = index > 0 ? sorted[index - 1] : null;

                    _geometryService.CalculateWellboreComponentVolume(component, "Imperial", prev);
                    ValidateWellboreComponent(component);

                    // Check for Casing Overwrite Logic (Rule 4.1)
                    // If user edited a casing to match the previous one's start, they might intend to overwrite.
                    if (prev != null && component.SectionType == WellboreSectionType.Casing && prev.SectionType == WellboreSectionType.Casing)
                    {
                        bool topMatches = component.TopMD.HasValue && prev.TopMD.HasValue && Math.Abs(component.TopMD.Value - prev.TopMD.Value) < 0.01;
                        bool odMatches = Math.Abs(component.OD.GetValueOrDefault() - prev.OD.GetValueOrDefault()) < 0.001;
                        bool isExtension = component.BottomMD.GetValueOrDefault() > prev.BottomMD.GetValueOrDefault();

                        if (topMatches && odMatches && isExtension)
                        {
                            // Trigger Overwrite
                            // 1. Update previous casing depth
                            prev.BottomMD = component.BottomMD;
                            prev.Name = component.Name; // Optional update
                            
                            ToastNotificationService.Instance.ShowSuccess($"Casing overwritten/extended: {prev.Name} now to {prev.BottomMD} ft.");

                            // 2. Remove the current "duplicate" component that was just edited
                            // We must do this carefully to avoid re-triggering events in a loop
                            // Dispatch to UI thread to remove after current event
                            Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                WellboreComponents.Remove(component);
                            });
                        }
                    }

                    // DEPTH CHAINING: Update next component's TopMD if BottomMD changed
                    if (e.PropertyName == nameof(WellboreComponent.BottomMD))
                    {
                        var next = index < sorted.Count - 1 ? sorted[index + 1] : null;
                        if (next != null)
                        {
                            next.SetPreviousBottomMD(component.BottomMD);
                        }
                    }

                    // VOLUME CASCADING: If this ID changes, the NEXT component's annular volume might change.
                    if (e.PropertyName == nameof(WellboreComponent.ID) || e.PropertyName == nameof(WellboreComponent.OD)) // OD can also affect next if we ever support complex annulus
                    {
                        var next = index < sorted.Count - 1 ? sorted[index + 1] : null;
                        if (next != null)
                        {
                             // Recalculate next component volume with THIS component as 'previous'
                            _geometryService.CalculateWellboreComponentVolume(next, "Imperial", component);
                        }
                    }
                }
                RecalculateTotals();
            }
        }

        /// <summary>
        /// Validates a wellbore component against all rules including telescoping and casing progression
        /// </summary>
        private void ValidateWellboreComponent(WellboreComponent component)
        {
            if (component == null) return;
            
            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            int index = sorted.IndexOf(component);
            
            if (index < 0) return;
            
            var previousComponent = index > 0 ? sorted[index - 1] : null;

            // Recalculate Volume using Service (Context dependent)
            _geometryService.CalculateWellboreComponentVolume(component, "Imperial", previousComponent);
            
            // Validate telescopic diameter (OD[n] < ID[n-1])
            component.ValidateTelescopicDiameter(previousComponent);
            
            // Validate casing depth progression
            component.ValidateCasingDepthProgression(previousComponent);
            
            // Handle casing override logic
            CheckForCasingOverwrite(component, previousComponent);
        }

        private void CheckForCasingOverwrite(WellboreComponent current, WellboreComponent? previous)
        {
            if (previous != null && 
                (current.SectionType == WellboreSectionType.Casing || current.SectionType == WellboreSectionType.Liner) &&
                (previous.SectionType == WellboreSectionType.Casing || previous.SectionType == WellboreSectionType.Liner))
            {
                // Check for duplicate start (potential overwrite condition)
                // Condition: Type Matches, OD Matches, TopMD Matches, New BottomMD > Old BottomMD
                
                bool isSameType = current.SectionType == previous.SectionType;
                bool isSameOD = Math.Abs((current.OD ?? 0) - (previous.OD ?? 0)) < 0.001;
                bool isSameTop = Math.Abs((current.TopMD ?? 0) - (previous.TopMD ?? 0)) < 0.01;
                bool isExtension = (current.BottomMD ?? 0) > (previous.BottomMD ?? 0);
                
                if (isSameType && isSameOD && isSameTop && isExtension)
                {
                    // This looks like an intended overwrite/extension
                    // We can't modify the collection inside a validation loop triggered by collection change/property change strictly speaking,
                    // but we can queue it or handle it.
                    // Given the request asks for "Smart Add", maybe we handle this at the "Add" command level predominantly,
                    // but if the user edits a row to match, we might offer to merge.
                    
                    // For now, let's just notify. The "Add" command will handle the auto-merge.
                    // ToastNotificationService.Instance.ShowInfo("Identical Casing detected. Use 'Add Section' logic to auto-extend.");
                }
            }
        }

        private void OnDrillStringComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrillStringComponent.Length) || 
                e.PropertyName == nameof(DrillStringComponent.OD) ||
                e.PropertyName == nameof(DrillStringComponent.ID))
            {
                if (sender is DrillStringComponent component)
                {
                    // Volume calculations are now handled automatically in the model
                }
                RecalculateTotals();
            }
        }

        private void OnSurveyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SurveyPoint point in e.NewItems)
                {
                    point.PropertyChanged += OnSurveyPointChanged;
                    ValidateSurveyPoint(point);
                }
            }
            if (e.OldItems != null)
            {
                foreach (SurveyPoint point in e.OldItems)
                {
                    point.PropertyChanged -= OnSurveyPointChanged;
                }
            }
            
            // Re-validate all points after collection change (order may have changed)
            foreach (var point in SurveyPoints)
            {
                ValidateSurveyPoint(point);
            }
            UpdateSurveyChart();
        }

        private void OnSurveyPointChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Only trigger recalculation when input fields change (MD, HoleAngle, Azimuth)
            // TVD, Northing, Easting, VerticalSection are auto-calculated and should not trigger recalc
            if (e.PropertyName == nameof(SurveyPoint.MD) || 
                e.PropertyName == nameof(SurveyPoint.HoleAngle) ||
                e.PropertyName == nameof(SurveyPoint.Azimuth))
            {
                if (sender is SurveyPoint point)
                {
                    // Recalculate trajectory for this point and all subsequent points
                    RecalculateSurveyTrajectory(point);
                    ValidateSurveyPoint(point);
                    UpdateSurveyChart();
                }
            }
        }

        private void UpdateSurveyChart()
        {
            if (SurveySeriesCollection == null || SurveySeriesCollection.Count == 0) return;

            var vsValues = new ChartValues<ObservablePoint>();
            var sorted = SurveyPoints.OrderBy(p => p.MD).ToList();

            foreach (var p in sorted)
            {
                // X = Vertical Section (Horizontal Displacement), Y = TVD (Inverted)
                vsValues.Add(new ObservablePoint(p.VerticalSection, -p.TVD));
            }

            SurveySeriesCollection[0].Values = vsValues;
        }

        /// <summary>
        /// Recalculates trajectory for a survey point and all subsequent points.
        /// Called when MD, HoleAngle, or Azimuth changes.
        /// </summary>
        private void RecalculateSurveyTrajectory(SurveyPoint changedPoint)
        {
            if (changedPoint == null) return;
            
            var sorted = SurveyPoints.OrderBy(p => p.MD).ToList();
            int index = sorted.IndexOf(changedPoint);
            
            if (index < 0) return;
            
            // Recalculate from this point forward
            for (int i = index; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var previous = i > 0 ? sorted[i - 1] : null;
                _surveyCalculationService.CalculateTrajectory(current, previous);
            }
        }

        /// <summary>
        /// Recalculates trajectory for all survey points.
        /// Useful after loading data or bulk changes.
        /// </summary>
        private void RecalculateAllSurveyTrajectories()
        {
            var sorted = SurveyPoints.OrderBy(p => p.MD).ToList();
            _surveyCalculationService.RecalculateAllTrajectories(sorted);
        }

        /// <summary>
        /// Validates a survey point against depth progression rules (S1)
        /// </summary>
        private void ValidateSurveyPoint(SurveyPoint point)
        {
            if (point == null) return;
            
            var sorted = SurveyPoints.OrderBy(p => p.MD).ToList();
            int index = sorted.IndexOf(point);
            
            if (index < 0) return;
            
            var previousPoint = index > 0 ? sorted[index - 1] : null;
            
            // Validate S1: Depth progression
            point.ValidateDepthProgression(previousPoint);
        }

        // Header fields
        public string WellName
        {
            get => _wellName;
            set => SetProperty(ref _wellName, value);
        }

        public string ReportNumber
        {
            get => _reportNumber;
            set => SetProperty(ref _reportNumber, value);
        }

        public string Operator
        {
            get => _operator;
            set => SetProperty(ref _operator, value);
        }

        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public string RigName
        {
            get => _rigName;
            set => SetProperty(ref _rigName, value);
        }

        // Collections
        public ObservableCollection<WellboreComponent> WellboreComponents { get; }
        public ObservableCollection<DrillStringComponent> DrillStringComponents { get; }
        public ObservableCollection<SurveyPoint> SurveyPoints { get; }
        public ObservableCollection<WellTest> WellTests { get; }
        public ObservableCollection<AnnularVolumeDetail> AnnularVolumeDetails { get; }

        public Func<double, string> YAxisLabelFormatter => value => Math.Abs(value).ToString("N0");

        public double AnnularVolumePercent => TotalCirculationVolume > 0 ? (TotalAnnularVolume / TotalCirculationVolume) * 100 : 0;
        public double StringVolumePercent => TotalCirculationVolume > 0 ? (TotalDrillStringVolume / TotalCirculationVolume) * 100 : 0;

        public SeriesCollection SurveySeriesCollection
        {
            get => _surveySeriesCollection;
            set => SetProperty(ref _surveySeriesCollection, value);
        }

        public SeriesCollection SafetySeriesCollection
        {
            get => _safetySeriesCollection;
            set => SetProperty(ref _safetySeriesCollection, value);
        }

        private double _currentMudWeight;
        public double CurrentMudWeight
        {
            get => _currentMudWeight;
            set
            {
                if (SetProperty(ref _currentMudWeight, value))
                {
                    RecalculateSafetyMetrics();
                }
            }
        }

        private double _maasp;
        public double MAASP
        {
            get => _maasp;
            set => SetProperty(ref _maasp, value);
        }

        private double _kickTolerance;
        public double KickTolerance
        {
            get => _kickTolerance;
            set => SetProperty(ref _kickTolerance, value);
        }



        #region Commands

        public ICommand SaveCommand => new RelayCommand(async _ => await SaveProjectAsync());
        public ICommand LoadCommand => new RelayCommand(async _ => await LoadProjectAsync());
        public ICommand ExportToCsvCommand => new RelayCommand(ExportToCsv);
        public ICommand ShowVisualizationCommand => new RelayCommand(ShowVisualization);

        public ICommand ForceToBottomCommand => new RelayCommand(_ => ExecuteForceToBottom(), _ => CanForceToBottom);

        private WellTest? _selectedWellTest;
        public WellTest? SelectedWellTest
        {
            get => _selectedWellTest;
            set
            {
                if (SetProperty(ref _selectedWellTest, value))
                {
                    UpdateLotChart();
                }
            }
        }

        public SeriesCollection LotSeriesCollection
        {
            get => _lotSeriesCollection;
            set => SetProperty(ref _lotSeriesCollection, value);
        }

        public ICommand ImportPumpDataCommand => new RelayCommand(_ => ExecuteImportPumpData(), _ => SelectedWellTest != null && SelectedWellTest.Type == WellTestType.LeakOff);
        
        // Navigation Commands
        public ICommand SaveAndNextCommand => new RelayCommand(async _ => await SaveAndNextAsync());
        public ICommand FinalizeGeometryCommand => new RelayCommand(async _ => await FinalizeGeometryAsync());

        private async Task SaveAndNextAsync()
        {
            // 1. Validation for current tab
            if (!ValidateCurrentTab()) return;

            // 2. Save
            await SaveProjectAsync();

            // 3. Move to next tab
            if (SelectedTabIndex < 5) // Assuming 6 tabs (0-5)
            {
                SelectedTabIndex++;
            }
        }

        private bool ValidateCurrentTab()
        {
            // Simple validation based on current tab index
            switch (SelectedTabIndex)
            {
                case 0: // Wellbore
                    if (WellboreComponents.Any(c => !c.IsValid)) {
                        ToastNotificationService.Instance.ShowError("Fix Wellbore errors first.");
                        return false;
                    }
                    return true;
                case 1: // DrillString
                    if (DrillStringComponents.Any(c => !c.IsValid)) {
                        ToastNotificationService.Instance.ShowError("Fix Drill String errors first.");
                        return false;
                    }
                    return true;
                case 2: // Survey
                     if (SurveyPoints.Any(c => !c.IsValid)) {
                        ToastNotificationService.Instance.ShowError("Fix Survey errors first.");
                        return false;
                    }
                    return true;
                case 3: // Thermal Gradient
                    if (ThermalGradientViewModel.ThermalGradientPoints.Any(c => !c.IsValid)) {
                        ToastNotificationService.Instance.ShowError("Fix Thermal Gradient errors first.");
                        return false;
                    }
                    return true;
                case 4: // Well Test
                    if (WellTests.Any(c => !c.IsValid)) {
                        ToastNotificationService.Instance.ShowError("Fix Well Test errors first.");
                        return false;
                    }
                    return true;
            }
            return true;
        }

        private async Task FinalizeGeometryAsync()
        {
             // 1. Validate All
             if (WellboreComponents.Any(c => !c.IsValid) || DrillStringComponents.Any(c => !c.IsValid))
             {
                 ToastNotificationService.Instance.ShowError("Cannot finalize. Fix validation errors.");
                 return;
             }

             // 2. Save
             await SaveProjectAsync();

             // 3. Navigate to Inventory
             NavigationService.Instance.NavigateToInventory(_currentWell?.Id ?? 0);
        }

        // Wellbore Commands
        public ICommand AddWellboreSectionCommand => new RelayCommand(AddWellboreSection);
        public ICommand DeleteWellboreSectionCommand => new RelayCommand(DeleteWellboreSection);
        
        private void AddWellboreSection(object? parameter)
        {
            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            var lastSection = sorted.FirstOrDefault(c => c.TopMD.HasValue);
            
            // Create completely empty section - user must fill all fields
            var newSection = new WellboreComponent
            {
                Id = GetNextWellboreId(),
                Name = string.Empty,          // Empty name - user must enter
                SectionType = default,        // null - user must select from dropdown
                TopMD = null,                 // Will be auto-set
                BottomMD = null,              // Empty - user must enter
                OD = null,                    // Empty - user must enter
                ID = null,                    // Empty - user must enter
                Washout = null                // Empty - optional for OpenHole
            };

            // Auto-link TopMD logic:
            if (WellboreComponents.Count == 0)
            {
                // First row always starts at TopMD = 0
                newSection.SetAsFirstRow(true);
                newSection.TopMD = 0;
            }
            else if (lastSection != null && lastSection.BottomMD.HasValue)
            {
                // Subsequent rows: TopMD = previous row's BottomMD (auto-linked)
                newSection.SetPreviousBottomMD(lastSection.BottomMD.Value);
            }
            
            WellboreComponents.Add(newSection);
            newSection.PropertyChanged += OnWellboreComponentChanged;
            
            RecalculateTotals();
        }

        private void DeleteWellboreSection(object? parameter)
        {
            if (parameter is WellboreComponent section)
            {
                WellboreComponents.Remove(section);
                // Renumbering handled in CollectionChanged
            }
        }
        
        // Export commands for individual tabs
        public ICommand ExportWellboreCsvCommand => new RelayCommand(ExportWellboreCsv);
        public ICommand ExportDrillStringCsvCommand => new RelayCommand(ExportDrillStringCsv);
        public ICommand ExportSurveyCsvCommand => new RelayCommand(ExportSurveyCsv);
        public ICommand ExportWellTestCsvCommand => new RelayCommand(ExportWellTestCsv);
        public ICommand ExportAnnularDetailsCsvCommand => new RelayCommand(ExportAnnularDetailsCsv);
        
        public ICommand ExportWellboreJsonCommand => new RelayCommand(ExportWellboreJson);
        public ICommand ExportDrillStringJsonCommand => new RelayCommand(ExportDrillStringJson);
        public ICommand ExportSurveyJsonCommand => new RelayCommand(ExportSurveyJson);
        public ICommand ExportWellTestJsonCommand => new RelayCommand(ExportWellTestJson);
        
        // Import commands
        public ICommand ImportWellboreDataCommand => new RelayCommand(ImportWellboreData);
        public ICommand ImportDrillStringDataCommand => new RelayCommand(ImportDrillStringData);
        
        // Survey row action commands
        public ICommand MoveSurveyPointUpCommand => new RelayCommand(MoveSurveyPointUp, CanMoveSurveyPointUp);
        public ICommand MoveSurveyPointDownCommand => new RelayCommand(MoveSurveyPointDown, CanMoveSurveyPointDown);
        public ICommand DeleteSurveyPointCommand => new RelayCommand(DeleteSurveyPoint, CanDeleteSurveyPoint);
        
        private async Task SaveProjectAsync()
        {
            try
            {
                // BR-WG-002: Check for continuity errors before saving
                if (!ShowContinuityErrorModal())
                {
                    // If user cancelled or errors exist, don't save
                    return;
                }

                // BR-WG-003: Check for other validation errors
                // Run detailed Geometry Validation
                var validationResult = _validationService.ValidateWellbore(WellboreComponents, 300.0); // Assuming 300.0 for now, should be derived from context? User prompt said "300.00 ft" in rules.
                
                // Clear existing UI errors and warnings
                foreach (var comp in WellboreComponents) 
                {
                    comp.ClearValidationErrors();
                    comp.ClearValidationWarnings();
                }

                if (!validationResult.IsValid || validationResult.HasWarnings)
                {
                    // Map errors/warnings back to components for UI highlighting if needed
                    foreach (var item in validationResult.Items)
                    {
                        if (int.TryParse(item.ComponentId, out int index) && index >= 0 && index < WellboreComponents.Count)
                        {
                            if (item.Severity == GeometryValidationService.ValidationSeverity.Warning)
                            {
                                WellboreComponents[index].AddValidationWarning(item.Message);
                            }
                            else
                            {
                                WellboreComponents[index].AddValidationError(item.Message);
                            }
                        }
                    }

                    // Show Modal
                    var modal = new ProjectReport.Views.Modals.ValidationResultModal(validationResult);
                    if (Application.Current.MainWindow != null)
                        modal.Owner = Application.Current.MainWindow;
                        
                    modal.ShowDialog();

                    // Logic:
                    // If Critical Errors exist -> Stop (IsValid is false)
                    // If Only Warnings exist -> Check if user clicked "Continue"
                    if (validationResult.HasCriticalErrors)
                    {
                        return; // Block Save
                    }
                    
                    if (validationResult.HasWarnings && !modal.ContinueConfirmed)
                    {
                        return; // User cancelled warning
                    }
                    
                    // If we reach here, it's either Valid or Warnings were Confirmed.
                }

                if (WellboreComponents.Any(c => !c.IsValid))
                {
                    ToastNotificationService.Instance.ShowError("Please fix validation errors in Wellbore Geometry before saving.");
                    return;
                }

                // BR-DS-001: Check for Drill String validation errors
                if (DrillStringComponents.Any(c => !c.IsValid))
                {
                    ToastNotificationService.Instance.ShowError("Please fix validation errors in Drill String Geometry before saving.");
                    return;
                }

                // Check if drill string exceeds well MD (physically impossible)
                if (DrillStringExceedsMD)
                {
                    ShowDepthOverrunError();
                    return;
                }

                // BR-SV-001, BR-SV-002, BR-SV-003: Check for Survey validation errors
                if (SurveyPoints.Any(p => !p.IsValid))
                {
                    ToastNotificationService.Instance.ShowError("Please fix validation errors in Survey module before saving.");
                    return;
                }

                // BR-TG-001, BR-TG-002, BR-TG-003, BR-TG-004: Check for Thermal Gradient validation issues
                if (ThermalGradientViewModel.HasValidationError)
                {
                    // Some thermal gradient issues are warnings (overrideable), some are errors.
                    // We'll ask the user for confirmation.
                    var result = MessageBox.Show(
                        $"Thermal Gradient module has validation issues:\n\n{ThermalGradientViewModel.ValidationMessage}\n\nDo you want to save anyway?",
                        "Thermal Gradient Validation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Project Files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create a new project with the current data
                    var project = new Project
                    {
                        Name = "Wellbore Project",
                        WellName = WellName
                    };
                    
                    // Save the project
                    await DataPersistenceService.SaveProjectAsync(saveFileDialog.FileName, project);
                    
                    // Save the wellbore components
                    var wellboreFilePath = Path.ChangeExtension(saveFileDialog.FileName, ".wellbore.json");
                    await DataPersistenceService.SaveWellboreComponentsAsync(WellboreComponents, wellboreFilePath);
                    
                    // Save the drill string components
                    var drillStringFilePath = Path.ChangeExtension(saveFileDialog.FileName, ".drillstring.json");
                    await DataPersistenceService.SaveDrillStringComponentsAsync(DrillStringComponents, drillStringFilePath);
                    ToastNotificationService.Instance.ShowSuccess("Project saved successfully.");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error saving project: {ex.Message}");
            }
        }
        
        private async Task LoadProjectAsync()
        {
            // Implementation preserved but LoadWell is preferred for app navigation
            await Task.CompletedTask; 
        }

        public void LoadWell(Well well)
        {
            if (well == null) return;

            _currentWell = well; // Store reference to the well
            WellName = well.WellName;
         

            // Load Wellbore Components
            WellboreComponents.Clear();
            foreach (var component in well.WellboreComponents)
            {
                component.PropertyChanged += OnWellboreComponentChanged;
                WellboreComponents.Add(component);
            }
            
            // Validate all components after loading
            foreach (var component in WellboreComponents)
            {
                ValidateWellboreComponent(component);
            }
            
            // Recalculate volumes for all sections on data load
            RecalculateAllWellboreVolumes();

            // Load Drill String Components
            DrillStringComponents.Clear();
            foreach (var component in well.DrillStringComponents)
            {
                component.PropertyChanged += OnDrillStringComponentChanged;
                DrillStringComponents.Add(component);
            }

            // Load Survey Points
            SurveyPoints.Clear();
            foreach (var point in well.SurveyPoints)
            {
                SurveyPoints.Add(point);
            }
            
            // Recalculate all survey trajectories after loading
            RecalculateAllSurveyTrajectories();

            // Load Well Tests
            WellTests.Clear();
            foreach (var test in well.WellTests)
            {
                WellTests.Add(test);
            }

            // Load Thermal Gradient Points
            ThermalGradientViewModel.ThermalGradientPoints.Clear();
            foreach (var point in well.ThermalGradientPoints)
            {
                ThermalGradientViewModel.ThermalGradientPoints.Add(point);
            }

            RecalculateTotals();
            
            // Update MaxWellboreTVD for thermal gradient validation
            if (ThermalGradientViewModel != null && WellboreComponents.Count > 0)
            {
                var maxTVD = WellboreComponents.Max(w => w.BottomMD ?? 0);
                ThermalGradientViewModel.MaxWellboreTVD = maxTVD;
            }
        }

        /// <summary>
        /// Saves all geometry data back to the Well object for persistence
        /// </summary>
        public void SaveToWell()
        {
            if (_currentWell == null) return;

            // Sync Wellbore Components
            _currentWell.WellboreComponents.Clear();
            foreach (var component in WellboreComponents)
            {
                _currentWell.WellboreComponents.Add(component);
            }

            // Sync Drill String Components
            _currentWell.DrillStringComponents.Clear();
            foreach (var component in DrillStringComponents)
            {
                _currentWell.DrillStringComponents.Add(component);
            }

            // Sync Survey Points
            _currentWell.SurveyPoints.Clear();
            foreach (var point in SurveyPoints)
            {
                _currentWell.SurveyPoints.Add(point);
            }

            // Sync Well Tests
            _currentWell.WellTests.Clear();
            foreach (var test in WellTests)
            {
                _currentWell.WellTests.Add(test);
            }

            // Sync Thermal Gradient Points
            _currentWell.ThermalGradientPoints.Clear();
            foreach (var point in ThermalGradientViewModel.ThermalGradientPoints)
            {
                _currentWell.ThermalGradientPoints.Add(point);
            }
        }
        
        private void ExportToCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = ".csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Export wellbore components
                    var wellboreCsv = new StringBuilder();
                    wellboreCsv.AppendLine("Type,Top MD (ft),Bottom MD (ft),ID (in),OD (in),Volume (bbl)");
                    foreach (var component in WellboreComponents)
                    {
                        var top = component.TopMD.HasValue ? component.TopMD.Value.ToString("F2") : string.Empty;
                        var bottom = component.BottomMD.HasValue ? component.BottomMD.Value.ToString("F2") : string.Empty;
                        var id = component.ID.HasValue ? component.ID.Value.ToString("F3") : string.Empty;
                        var od = component.OD.HasValue ? component.OD.Value.ToString("F3") : string.Empty;
                        wellboreCsv.AppendLine($"{component.SectionType},{top},{bottom},{id},{od},{component.Volume:F2}");
                    }
                    
                    // Export drill string components
                    var drillStringCsv = new StringBuilder();
                    drillStringCsv.AppendLine("Type,Length (ft),ID (in),OD (in),Volume (bbl)");
                    foreach (var component in DrillStringComponents)
                    {
                        drillStringCsv.AppendLine($"{component.ComponentType},{component.Length:F2},{component.ID:F3},{component.OD:F3},{component.Volume:F2}");
                    }
                    
                    // Combine and save
                    var combinedCsv = $"=== WELLBORE COMPONENTS ===\n{wellboreCsv}\n\n=== DRILL STRING COMPONENTS ===\n{drillStringCsv}";
                    File.WriteAllText(saveFileDialog.FileName, combinedCsv);
                    
                    ToastNotificationService.Instance.ShowSuccess("Data exported to CSV successfully.");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting to CSV: {ex.Message}");
            }
        }
        
        private void ShowVisualization(object? parameter)
        {
            try
            {
                // This would typically open a visualization window or tab
                ToastNotificationService.Instance.ShowInfo("Visualization feature will be implemented here.");
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error showing visualization: {ex.Message}");
            }
        }

        #region Export Methods

        private void ExportWellboreCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Wellbore_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportWellboreToCsv(WellboreComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Wellbore data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting wellbore data: {ex.Message}");
            }
        }

        private void ExportDrillStringCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"DrillString_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportDrillStringToCsv(DrillStringComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Drill string data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting drill string data: {ex.Message}");
            }
        }

        private void ExportSurveyCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Survey_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportSurveyToCsv(SurveyPoints, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Survey data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting survey data: {ex.Message}");
            }
        }

        private void ExportWellTestCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"WellTest_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportWellTestsToCsv(WellTests, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Well test data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting well test data: {ex.Message}");
            }
        }

        private void ExportAnnularDetailsCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Annular_Volume_Details_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportAnnularVolumeDetailsToCsv(AnnularVolumeDetails, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Annular volume details exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting annular volume details: {ex.Message}");
            }
        }

        private void ExportWellboreJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"Wellbore_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(WellboreComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Wellbore data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting wellbore data: {ex.Message}");
            }
        }

        private void ExportDrillStringJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"DrillString_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(DrillStringComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Drill string data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting drill string data: {ex.Message}");
            }
        }

        private void ExportSurveyJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"Survey_Data_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(SurveyPoints, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Survey data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting survey data: {ex.Message}");
            }
        }

        private void ExportWellTestJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"WellTest_Data_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(WellTests, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Well test data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting well test data: {ex.Message}");
            }
        }

        #endregion

        #region Import Methods

        private void ImportWellboreData(object? parameter)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    Title = "Import Wellbore Data"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var importService = new ProjectReport.Services.Wellbore.WellboreImportService();
                    ProjectReport.Services.Wellbore.WellboreImportService.ImportResult result;

                    if (openFileDialog.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        result = importService.ImportFromExcel(openFileDialog.FileName);
                    }
                    else
                    {
                        result = importService.ImportFromCsv(openFileDialog.FileName);
                    }

                    if (result.Success)
                    {
                        // Clear existing components and add imported ones
                        WellboreComponents.Clear();
                        foreach (var component in result.WellboreComponents)
                        {
                            component.PropertyChanged += OnWellboreComponentChanged;
                            WellboreComponents.Add(component);
                        }

                        var message = $"Imported {result.ImportedCount} wellbore component(s)";
                        if (result.ErrorCount > 0)
                        {
                            message += $" with {result.ErrorCount} error(s)";
                        }
                        ToastNotificationService.Instance.ShowSuccess(message);

                        if (result.DetailedErrors.Count > 0)
                        {
                            var errorSummary = string.Join("\n", result.DetailedErrors.Take(5));
                            if (result.DetailedErrors.Count > 5)
                            {
                                errorSummary += $"\n... and {result.DetailedErrors.Count - 5} more errors";
                            }
                            ToastNotificationService.Instance.ShowWarning($"Import warnings:\n{errorSummary}");
                        }
                    }
                    else
                    {
                        ToastNotificationService.Instance.ShowError($"Import failed: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error importing wellbore data: {ex.Message}");
            }
        }

        private void ImportDrillStringData(object? parameter)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    Title = "Import Drill String Data"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var importService = new DrillStringImportService();
                    DrillStringImportService.ImportResult result;

                    if (openFileDialog.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        result = importService.ImportFromExcel(openFileDialog.FileName);
                    }
                    else
                    {
                        result = importService.ImportFromCsv(openFileDialog.FileName);
                    }

                    if (result.Success)
                    {
                        // Clear existing components and add imported ones
                        DrillStringComponents.Clear();
                        foreach (var component in result.DrillStringComponents)
                        {
                            component.PropertyChanged += OnDrillStringComponentChanged;
                            DrillStringComponents.Add(component);
                        }

                        var message = $"Imported {result.ImportedCount} drill string component(s)";
                        if (result.ErrorCount > 0)
                        {
                            message += $" with {result.ErrorCount} error(s)";
                        }
                        ToastNotificationService.Instance.ShowSuccess(message);

                        if (result.DetailedErrors.Count > 0)
                        {
                            var errorSummary = string.Join("\n", result.DetailedErrors.Take(5));
                            if (result.DetailedErrors.Count > 5)
                            {
                                errorSummary += $"\n... and {result.DetailedErrors.Count - 5} more errors";
                            }
                            ToastNotificationService.Instance.ShowWarning($"Import warnings:\n{errorSummary}");
                        }
                    }
                    else
                    {
                        ToastNotificationService.Instance.ShowError($"Import failed: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error importing drill string data: {ex.Message}");
            }
        }

        #endregion

        #region Survey Row Actions

        private void MoveSurveyPointUp(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                if (index > 0)
                {
                    SurveyPoints.Move(index, index - 1);
                    ToastNotificationService.Instance.ShowSuccess("Survey point moved up");
                }
            }
        }

        private bool CanMoveSurveyPointUp(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                return index > 0;
            }
            return false;
        }

        private void MoveSurveyPointDown(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                if (index >= 0 && index < SurveyPoints.Count - 1)
                {
                    SurveyPoints.Move(index, index + 1);
                    ToastNotificationService.Instance.ShowSuccess("Survey point moved down");
                }
            }
        }

        private bool CanMoveSurveyPointDown(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                return index >= 0 && index < SurveyPoints.Count - 1;
            }
            return false;
        }

        private void DeleteSurveyPoint(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                SurveyPoints.Remove(point);
                ToastNotificationService.Instance.ShowSuccess("Survey point deleted");
            }
        }

        private bool CanDeleteSurveyPoint(object? parameter)
        {
            return parameter is SurveyPoint;
        }

        #endregion


        // Calculated totals
        public double TotalWellboreVolume { get; private set; }
        public double TotalDrillStringVolume { get; private set; }
        public double TotalAnnularVolume { get; private set; }
        public double TotalCirculationVolume { get; private set; }
        public double TotalWellboreMD { get; private set; }
        public string ContinuityError { get; private set; } = string.Empty;

        // Validation error counts for tab indicators
        public int WellboreErrorCount => ValidateWellboreContinuity().Count + WellboreComponents.Count(c => c.HasErrors);
        public int DrillStringErrorCount => DrillStringComponents.Count(c => c.HasErrors);
        public int SurveyErrorCount => SurveyPoints.Count(p => p.HasErrors);
        public int WellTestErrorCount => WellTests.Count(t => t.HasErrors);

        // Auto-increment ID counters
        private int _nextWellboreId = 1;
        private int _nextDrillStringId = 1;
        private int _nextSurveyId = 1;
        private int _nextWellTestId = 1;

        // Drill String Force to Bottom
        private bool _forceDrillStringToBottom = false;
        public bool ForceDrillStringToBottom
        {
            get => _forceDrillStringToBottom;
            set
            {
                SetProperty(ref _forceDrillStringToBottom, value);
                if (value)
                {
                    CalculateDrillStringToBottom();
                }
                OnPropertyChanged(nameof(FeetMissing));
                OnPropertyChanged(nameof(DepthDifferential));
            }
        }

        /// <summary>
        /// Forces the drill string to bottom by extending the last component
        /// </summary>
        private void ExecuteForceToBottom()
        {
            CalculateDrillStringToBottom();
        }

        public double FeetMissing
        {
            get
            {
                if (TotalWellboreMD <= 0) return 0;
                double totalDrillStringLength = DrillStringComponents.Sum(c => c.Length.GetValueOrDefault());
                return Math.Max(0, TotalWellboreMD - totalDrillStringLength);
            }
        }

        public double DepthDifferential
        {
            get
            {
                double totalDrillStringLength = DrillStringComponents.Sum(c => c.Length.GetValueOrDefault());
                return TotalWellboreMD - totalDrillStringLength;
            }
        }

        public bool HasDrillStringDepthError => TotalWellboreMD > 0 && DepthDifferential < -DepthTolerance;

        public string DrillStringDepthErrorMessage
        {
            get => _drillStringDepthErrorMessage;
            private set => SetProperty(ref _drillStringDepthErrorMessage, value);
        }

        public bool CanForceToBottom => !HasDrillStringDepthError && TotalWellboreMD > 0 && DrillStringComponents.Count > 0;

        /// <summary>
        /// Gets the total drill string length (sum of all component lengths)
        /// </summary>
        public double TotalDrillStringLength => DrillStringComponents.Sum(c => c.Length.GetValueOrDefault());

        /// <summary>
        /// Gets the bottom differential (Well_MD - TotalStringLength)
        /// Positive = string is short, Negative = string exceeds TD, Zero = on bottom
        /// </summary>
        public double BottomDifferential => DepthDifferential;

        /// <summary>
        /// Gets the depth differential status for color coding
        /// </summary>
        public string DepthDifferentialStatus
        {
            get
            {
                double diff = DepthDifferential;
                if (Math.Abs(diff) < DepthTolerance) return "OnBottom"; // 0 ft
                if (diff > 0) return "Short"; // Positive - not reaching
                return "Overrun"; // Negative - exceeds TD
            }
        }

        /// <summary>
        /// Gets the color for depth differential indicator
        /// </summary>
        public System.Windows.Media.Brush DepthDifferentialColor
        {
            get
            {
                return DepthDifferentialStatus switch
                {
                    "OnBottom" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green),
                    "Short" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange),
                    "Overrun" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                };
            }
        }

        private double _shoeDepth;
        public double ShoeDepth
        {
            get => _shoeDepth;
            set => SetProperty(ref _shoeDepth, value);
        }

        /// <summary>
        /// Checks if drill string exceeds well MD (should block save)
        /// </summary>
        public bool DrillStringExceedsMD => TotalWellboreMD > 0 && DepthDifferential < -DepthTolerance;

        /// <summary>
        /// Gets BitToBottom calculation when last component is Bit
        /// BitToBottom = FinalStringLength - Well_MD
        /// </summary>
        public double? BitToBottom
        {
            get
            {
                if (DrillStringComponents.Count == 0) return null;
                var lastComponent = DrillStringComponents.LastOrDefault();
                if (lastComponent?.ComponentType != ComponentType.Bit) return null;
                
                return TotalDrillStringLength - TotalWellboreMD;
            }
        }

        public bool IsOnBottom => BitToBottom != null && Math.Abs(BitToBottom.Value) < 0.1;

        /// <summary>
        /// Gets suggested BHA components when last component is DrillPipe
        /// </summary>
        public List<ComponentType> SuggestedBHAComponents
        {
            get
            {
                if (DrillStringComponents.Count == 0) return new List<ComponentType>();
                var lastComponent = DrillStringComponents.LastOrDefault();
                if (lastComponent?.ComponentType != ComponentType.DrillPipe) return new List<ComponentType>();
                
                return new List<ComponentType>
                {
                    ComponentType.DC,        // Drill Collar
                    ComponentType.HWDP,      // Heavy Weight
                    ComponentType.Stabilizer, // Stabilizer
                    ComponentType.Bit        // Bit
                };
            }
        }

        public string BhaWarningMessage
        {
            get => _bhaWarningMessage;
            private set => SetProperty(ref _bhaWarningMessage, value);
        }

        public bool ShowBhaWarning => !string.IsNullOrWhiteSpace(BhaWarningMessage);

        public IEnumerable<string> BhaInsertPositions => _bhaInsertPositions;

        public string BhaInsertPosition
        {
            get => _bhaInsertPosition;
            set => SetProperty(ref _bhaInsertPosition, value);
        }

        public void InsertStandardBhaComponent(ComponentType componentType)
        {
            var component = CreateDefaultBhaComponent(componentType);
            if (component == null) return;

            if (string.Equals(BhaInsertPosition, "Top", StringComparison.OrdinalIgnoreCase))
            {
                DrillStringComponents.Insert(0, component);
            }
            else
            {
                DrillStringComponents.Add(component);
            }

            component.PropertyChanged += OnDrillStringComponentChanged;
            RecalculateTotals();
        }

        private DrillStringComponent? CreateDefaultBhaComponent(ComponentType componentType)
        {
            double holeSize = GetHoleDiameter();
            double defaultHole = holeSize > 0 ? holeSize : 8.5;

            switch (componentType)
            {
                case ComponentType.Bit:
                    return new DrillStringComponent
                    {
                        Id = GetNextDrillStringId(),
                        Name = "Bit",
                        ComponentType = ComponentType.Bit,
                        Length = 1.0,
                        OD = defaultHole,
                        ID = Math.Max(0.5, defaultHole * 0.6)
                    };
                case ComponentType.DC:
                    return new DrillStringComponent
                    {
                        Id = GetNextDrillStringId(),
                        Name = "Drill Collar",
                        ComponentType = ComponentType.DC,
                        Length = 30.0,
                        OD = 7.0,
                        ID = 3.0
                    };
                case ComponentType.HWDP:
                    return new DrillStringComponent
                    {
                        Id = GetNextDrillStringId(),
                        Name = "HWDP",
                        ComponentType = ComponentType.HWDP,
                        Length = 30.0,
                        OD = 5.0,
                        ID = 4.276
                    };
                default:
                    return null;
            }
        }

        private double GetHoleDiameter()
        {
            var openHole = WellboreComponents.LastOrDefault(c => c.SectionType == WellboreSectionType.OpenHole);
            if (openHole != null && openHole.OD.GetValueOrDefault() > 0)
            {
                return openHole.OD.GetValueOrDefault();
            }

            var lastSection = WellboreComponents.LastOrDefault();
            return lastSection?.OD.GetValueOrDefault() ?? 0;
        }

        private void CalculateDrillStringToBottom()
        {
            if (HasDrillStringDepthError)
            {
                ShowDepthOverrunError();
                return;
            }

            if (TotalWellboreMD <= 0) return;
            if (DrillStringComponents.Count == 0) return;

            // Get the LAST component in the drill string (bottom-most)
            var lastComponent = DrillStringComponents.LastOrDefault();
            if (lastComponent == null)
            {
                ToastNotificationService.Instance.ShowWarning("No drill string components found to adjust.");
                return;
            }

            double totalOtherLength = DrillStringComponents
                .Where(c => c != lastComponent)
                .Sum(c => c.Length.GetValueOrDefault());

            double delta = TotalWellboreMD - (totalOtherLength + lastComponent.Length.GetValueOrDefault());

            // If string is shorter than MD, extend last component
            if (delta > DepthTolerance)
            {
                double oldLength = lastComponent.Length.GetValueOrDefault();
                double newLength = lastComponent.Length.GetValueOrDefault() + delta;
                
                // Update the last component
                lastComponent.Length = newLength;
                
                // Highlight the adjusted field
                lastComponent.IsHighlighted = true;
                
                // Remove highlight after 2 seconds
                Task.Delay(2000).ContinueWith(_ => 
                {
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        lastComponent.IsHighlighted = false;
                    });
                });
                
                // Show notification
                ToastNotificationService.Instance.ShowSuccess(
                    $"Drill String forced to bottom. Last component length adjusted from {oldLength:F2} ft to {newLength:F2} ft (+{delta:F2} ft).");
            }
            // If string exceeds MD, show error (but don't auto-adjust)
            else if (delta < -DepthTolerance)
            {
                ShowDepthOverrunError();
            }
        }

        /// <summary>
        /// Recalculates volumes for all wellbore sections after data load.
        /// Ensures sections with complete data (OD, ID, TopMD, BottomMD) show proper volumes.
        /// </summary>
        private void RecalculateAllWellboreVolumes()
        {
            if (WellboreComponents.Count == 0) return;
            
            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            
            for (int i = 0; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var previous = i > 0 ? sorted[i - 1] : null;
                
                // Calculate volume for this section with context of previous section
                _geometryService.CalculateWellboreComponentVolume(current, "Imperial", previous);
            }
        }

        public void RecalculateTotals()
        {
            TotalWellboreVolume = _geometryService.CalculateTotalWellboreVolume(WellboreComponents, "Imperial");
            TotalDrillStringVolume = _geometryService.CalculateTotalDrillStringVolume(DrillStringComponents, false, "Imperial");
            TotalAnnularVolume = _geometryService.CalculateTotalAnnularVolume(TotalWellboreVolume, TotalDrillStringVolume);
            TotalCirculationVolume = TotalAnnularVolume + TotalDrillStringVolume;
            TotalWellboreMD = WellboreComponents.Count > 0 ? WellboreComponents.Max(w => w.BottomMD ?? 0) : 0;
            
            // Calculate Shoe Depth: BottomMD of the deepest Casing or Liner section
            var lastCasing = WellboreComponents
                .Where(c => c.SectionType == WellboreSectionType.Casing || c.SectionType == WellboreSectionType.Liner)
                .OrderByDescending(c => c.BottomMD)
                .FirstOrDefault();
            ShoeDepth = lastCasing?.BottomMD ?? 0;
            
            // Update Thermal Gradient context with survey depth information
            var maxSurveyTvd = SurveyPoints.Count > 0 ? SurveyPoints.Max(p => p.TVD) : 0;
            ThermalGradientViewModel.MaxWellboreTVD = (maxSurveyTvd > 0 ? maxSurveyTvd : TotalWellboreMD);
            ThermalGradientViewModel.HasSurveyData = SurveyPoints.Count > 0;
            if (ForceDrillStringToBottom)
            {
                CalculateDrillStringToBottom();
            }
            // Update continuity error
            var continuityErrors = ValidateWellboreContinuity();
            ContinuityError = continuityErrors.FirstOrDefault() ?? string.Empty;
            // Notify UI
            OnPropertyChanged(nameof(ContinuityError));
            // Raise total property changes
            OnPropertyChanged(nameof(TotalWellboreVolume));
            OnPropertyChanged(nameof(TotalDrillStringVolume));
            OnPropertyChanged(nameof(TotalAnnularVolume));
            OnPropertyChanged(nameof(TotalCirculationVolume));
            OnPropertyChanged(nameof(TotalWellboreMD));
            OnPropertyChanged(nameof(AnnularVolumePercent));
            OnPropertyChanged(nameof(StringVolumePercent));
            UpdateAnnularVolumeDetails();
            
            // Update drill string depth properties
            OnPropertyChanged(nameof(TotalDrillStringLength));
            OnPropertyChanged(nameof(BottomDifferential));
            OnPropertyChanged(nameof(FeetMissing));
            OnPropertyChanged(nameof(DepthDifferential));
            OnPropertyChanged(nameof(DepthDifferentialStatus));
            OnPropertyChanged(nameof(DepthDifferentialColor));
            OnPropertyChanged(nameof(DrillStringExceedsMD));
            OnPropertyChanged(nameof(BitToBottom));
            OnPropertyChanged(nameof(SuggestedBHAComponents));
            UpdateDrillStringDepthState();
            OnPropertyChanged(nameof(DrillStringDepthErrorMessage));
            OnPropertyChanged(nameof(HasDrillStringDepthError));
            OnPropertyChanged(nameof(CanForceToBottom));
            OnPropertyChanged(nameof(BhaWarningMessage));
            OnPropertyChanged(nameof(ShowBhaWarning));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            
            // Update validation error counts
            OnPropertyChanged(nameof(WellboreErrorCount));
            OnPropertyChanged(nameof(DrillStringErrorCount));
            OnPropertyChanged(nameof(SurveyErrorCount));
            OnPropertyChanged(nameof(WellTestErrorCount));

            // Update Safety Metrics (MAASP, Kick Tolerance)
            RecalculateSafetyMetrics();
        }


        private void UpdateAnnularVolumeDetails()
        {
            AnnularVolumeDetails.Clear();
            
            // Use the new service method for detailed calculation
            var details = _geometryService.CalculateAnnularVolumeDetails(
                WellboreComponents, 
                DrillStringComponents, 
                "Imperial"); // Assuming Imperial for now, should be dynamic based on settings
                
            foreach (var detail in details)
            {
                AnnularVolumeDetails.Add(detail);
            }
        }

        private void UpdateDrillStringDepthState()
        {
            if (TotalWellboreMD <= 0)
            {
                DrillStringDepthErrorMessage = string.Empty;
                _depthOverrunToastShown = false;
                return;
            }

            if (HasDrillStringDepthError)
            {
                DrillStringDepthErrorMessage =
                    $"Error D1: La longitud de la sarta de perforación ({TotalDrillStringLength:F2} ft) excede la Profundidad Total del Pozo ({TotalWellboreMD:F2} ft). Ajuste la longitud o la profundidad de la última herramienta.";

                if (!_depthOverrunToastShown)
                {
                    ToastNotificationService.Instance.ShowError(DrillStringDepthErrorMessage);
                    _depthOverrunToastShown = true;
                }
            }
            else
            {
                DrillStringDepthErrorMessage = string.Empty;
                _depthOverrunToastShown = false;
            }
        }

        private void ShowDepthOverrunError()
        {
            ToastNotificationService.Instance.ShowError(
                $"Error D1: La longitud de la sarta de perforación ({TotalDrillStringLength:F2} ft) excede la Profundidad Total del Pozo ({TotalWellboreMD:F2} ft). Ajuste la longitud o la profundidad de la última herramienta.");
        }


        public int GetNextWellboreId()
        {
            return _nextWellboreId++;
        }

        public int GetNextDrillStringId()
        {
            return _nextDrillStringId++;
        }

        public int GetNextSurveyId()
        {
            return _nextSurveyId++;
        }

        public int GetNextWellTestId()
        {
            return _nextWellTestId++;
        }

        // Helper methods to convert between string and enum
        public static WellboreSectionType StringToSectionType(string value)
        {
            return value switch
            {
                "Casing" => WellboreSectionType.Casing,
                "Liner" => WellboreSectionType.Liner,
                _ => WellboreSectionType.OpenHole
            };
        }

        public static ComponentType StringToComponentType(string value)
        {
            return value switch
            {
                "Drill Pipe" => ComponentType.DrillPipe,
                "HWDP" => ComponentType.HWDP,
                "Casing" => ComponentType.Casing,
                "Liner" => ComponentType.Liner,
                "Setting Tool" => ComponentType.SettingTool,
                "DC" => ComponentType.DC,
                "LWD" => ComponentType.LWD,
                "MWD" => ComponentType.MWD,
                "PWD" => ComponentType.PWD,
                "Motor" => ComponentType.Motor,
                "XO" => ComponentType.XO,
                "JAR" => ComponentType.Jar,
                "Accelerator" => ComponentType.Accelerator,
                "Stabilizer" => ComponentType.Stabilizer,
                "Near Bit" => ComponentType.NearBit,
                "Bit Sub" => ComponentType.BitSub,
                "Bit" => ComponentType.Bit,
                _ => ComponentType.DrillPipe
            };
        }

        public static string ComponentTypeToString(ComponentType type)
        {
            return type switch
            {
                ComponentType.DrillPipe => "Drill Pipe",
                ComponentType.HWDP => "HWDP",
                ComponentType.Casing => "Casing",
                ComponentType.Liner => "Liner",
                ComponentType.SettingTool => "Setting Tool",
                ComponentType.DC => "DC",
                ComponentType.LWD => "LWD",
                ComponentType.MWD => "MWD",
                ComponentType.PWD => "PWD",
                ComponentType.Motor => "Motor",
                ComponentType.XO => "XO",
                ComponentType.Jar => "JAR",
                ComponentType.Accelerator => "Accelerator",
                ComponentType.NearBit => "Near Bit",
                ComponentType.BitSub => "Bit Sub",
                ComponentType.Bit => "Bit",
                _ => type.ToString()
            };
        }

        public static WellTestType StringToWellTestType(string value)
        {
            return value switch
            {
                "Leak Off" => WellTestType.LeakOff,
                "Fracture gradient" => WellTestType.FractureGradient,
                "Pore pressure" => WellTestType.PorePressure,
                "Integrity" => WellTestType.FormationIntegrity,
                _ => WellTestType.LeakOff
            };
        }

        public static string WellTestTypeToString(WellTestType type)
        {
            return type switch
            {
                WellTestType.LeakOff => "Leak Off",
                WellTestType.FractureGradient => "Fracture gradient",
                WellTestType.PorePressure => "Pore pressure",
                WellTestType.FormationIntegrity => "Integrity",
                _ => type.ToString()
            };
        }
        #endregion

        #region Validation Methods

        /// <summary>
        /// BR-WG-002: Validates depth continuity between wellbore sections
        /// BR-WG-003: Validates that Top MD < Bottom MD for each section
        /// </summary>
        public List<string> ValidateWellboreContinuity()
        {
            var errors = new List<string>();
            if (WellboreComponents == null || WellboreComponents.Count == 0)
                return errors;

            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            
            // Check individual sections (BR-WG-003)
            foreach (var section in sorted)
            {
                if (section.TopMD.HasValue && section.BottomMD.HasValue && section.TopMD.Value >= section.BottomMD.Value)
                    errors.Add($"Section '{section.Name}': Top MD must be less than Bottom MD.");
            }

            // Check continuity (BR-WG-002)
            var continuityErrors = GetContinuityErrors();
            foreach (var (prev, curr) in continuityErrors)
            {
                errors.Add($"Continuity Error: Section '{curr.Name}' Top MD ({(curr.TopMD.HasValue ? curr.TopMD.Value.ToString("F2") : "N/A")}) does not match Section '{prev.Name}' Bottom MD ({(prev.BottomMD.HasValue ? prev.BottomMD.Value.ToString("F2") : "N/A")}).");
            }

            return errors;
        }

        private List<(WellboreComponent Prev, WellboreComponent Curr)> GetContinuityErrors()
        {
            var errors = new List<(WellboreComponent, WellboreComponent)>();
            if (WellboreComponents == null || WellboreComponents.Count < 2)
                return errors;

            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var prev = sorted[i];
                var curr = sorted[i + 1];
                
                // Only consider pairs with both MDs present
                if (prev.BottomMD.HasValue && curr.TopMD.HasValue)
                {
                    // Use a small tolerance for floating point comparison
                    if (Math.Abs(prev.BottomMD.Value - curr.TopMD.Value) > 0.01)
                    {
                        errors.Add((prev, curr));
                    }
                }
            }
            return errors;
        }

        public bool ShowContinuityErrorModal()
        {
            var errors = GetContinuityErrors();
            if (errors.Count > 0)
            {
                var (prev, curr) = errors.First();

                // Show the dialog
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new ProjectReport.Views.Geometry.ContinuityErrorDialog(prev, curr);
                    if (dialog.ShowDialog() == true)
                    {
                        // If fixed, recalculate
                        RecalculateTotals();
                        return true;
                    }
                    return false;
                });
            }
            return true; // No errors
        }


        private void OnWellTestsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ProjectReport.Models.Geometry.WellTest.WellTest test in e.NewItems)
                    test.PropertyChanged += OnWellTestPropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (ProjectReport.Models.Geometry.WellTest.WellTest test in e.OldItems)
                    test.PropertyChanged -= OnWellTestPropertyChanged;
            }
            RecalculateSafetyMetrics();
        }

        private void OnWellTestPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TestValue" || e.PropertyName == "TVD")
            {
                RecalculateSafetyMetrics();
            }
            else if (e.PropertyName == "TestPressurePsi")
            {
                if (sender is ProjectReport.Models.Geometry.WellTest.WellTest test && test.TVD > 0 && test.TestPressurePsi > 0)
                {
                    // Automatic Conversion: PSI to PPG
                    // EMW = MW + (PSI / (0.052 * TVD))
                    double emw = CurrentMudWeight + (test.TestPressurePsi / (0.052 * test.TVD));
                    
                    // Update TestValue only if it's different to prevent loops
                    if (Math.Abs(test.TestValue - emw) > 0.001)
                    {
                        test.TestValue = Math.Round(emw, 2);
                    }
                }
            }
        }

        private void RecalculateSafetyMetrics()
        {
            // 1. Calculate MAASP
            var latestLot = WellTests?
                .Where(t => t.Type == ProjectReport.Models.Geometry.WellTest.WellTestType.LeakOff || t.Type == ProjectReport.Models.Geometry.WellTest.WellTestType.FormationIntegrity)
                .OrderByDescending(t => t.TVD)
                .FirstOrDefault();

            if (latestLot != null && CurrentMudWeight > 0)
            {
                double lotEmu = latestLot.TestValue; // ppb
                MAASP = (lotEmu - CurrentMudWeight) * 0.052 * latestLot.TVD;
                if (MAASP < 0) MAASP = 0;
            }
            else
            {
                MAASP = 0;
            }

            // 2. Calculate Kick Tolerance (Volume)
            double influxGradient = 0.1; // psi/ft (Standard assumptions)
            double currentMudGradient = CurrentMudWeight * 0.052;
            
            if (MAASP > 0 && currentMudGradient > influxGradient)
            {
                double kickHeight = MAASP / (currentMudGradient - influxGradient);
                
                // Get annular capacity at bit/bottom
                var detailsAtBottom = AnnularVolumeDetails.LastOrDefault();
                if (detailsAtBottom != null && detailsAtBottom.Volume > 0 && (detailsAtBottom.BottomMD - detailsAtBottom.TopMD) > 0)
                {
                    double bblPerFt = detailsAtBottom.Volume / (detailsAtBottom.BottomMD - detailsAtBottom.TopMD);
                    KickTolerance = kickHeight * bblPerFt;
                }
                else
                {
                    KickTolerance = 0;
                }
            }
            else
            {
                KickTolerance = 0;
            }

            UpdateSafetyChart();
        }

        private void UpdateSafetyChart()
        {
            if (SafetySeriesCollection == null) SafetySeriesCollection = new SeriesCollection();
            else SafetySeriesCollection.Clear();

            double maxTVD = TotalWellboreMD > 0 ? TotalWellboreMD : 10000; 

            // 1. Hydrostatic Line (Standard Mud Gradient)
            var hydrostaticValues = new ChartValues<ObservablePoint>
            {
                new ObservablePoint(CurrentMudWeight, 0),
                new ObservablePoint(CurrentMudWeight, -maxTVD)
            };

            SafetySeriesCollection.Add(new LineSeries
            {
                Title = "Hydrostatic (Current MW)",
                Values = hydrostaticValues,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 3,
                PointGeometry = null,
                Fill = Brushes.Transparent
            });

            // 2. Leak-Off Tests / Integrity Points
            var lotPoints = WellTests?
                .Where(t => t.Type == ProjectReport.Models.Geometry.WellTest.WellTestType.LeakOff || t.Type == ProjectReport.Models.Geometry.WellTest.WellTestType.FormationIntegrity)
                .Select(t => new ObservablePoint(t.TestValue, -t.TVD))
                .ToList();

            if (lotPoints != null && lotPoints.Any())
            {
                SafetySeriesCollection.Add(new ScatterSeries
                {
                    Title = "Formation Integrity (LOT)",
                    Values = new ChartValues<ObservablePoint>(lotPoints),
                    PointGeometry = DefaultGeometries.Diamond,
                    MaxPointShapeDiameter = 12,
                    Fill = Brushes.Crimson
                });
            }

            // 3. Pore Pressure Line (Theoretical - for diagnostic)
            // Let's assume a default pore pressure of 9.0 ppg as a reference
            var porePressureValues = new ChartValues<ObservablePoint>
            {
                new ObservablePoint(9.0, 0),
                new ObservablePoint(9.0, -maxTVD)
            };

            SafetySeriesCollection.Add(new LineSeries
            {
                Title = "Pore Pressure (Ref)",
                Values = porePressureValues,
                Stroke = Brushes.SlateGray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                PointGeometry = null,
                Fill = Brushes.Transparent
            });
        }

        private void UpdateLotChart()
        {
            if (LotSeriesCollection == null) LotSeriesCollection = new SeriesCollection();
            else LotSeriesCollection.Clear();

            if (SelectedWellTest == null || SelectedWellTest.PumpDataPoints == null || !SelectedWellTest.PumpDataPoints.Any())
                return;

            var pressureValues = new ChartValues<ObservablePoint>(
                SelectedWellTest.PumpDataPoints.Select(p => new ObservablePoint(p.Volume > 0 ? p.Volume : p.Time, p.Pressure)));

            LotSeriesCollection.Add(new LineSeries
            {
                Title = "Pump Pressure",
                Values = pressureValues,
                Stroke = Brushes.OrangeRed,
                StrokeThickness = 2,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 6,
                Fill = Brushes.Transparent
            });
        }

        private void ExecuteImportPumpData()
        {
            if (SelectedWellTest == null)
            {
                ToastNotificationService.Instance.ShowWarning("Please select a Well Test of type 'Leak Off' first.");
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var points = new List<PumpDataPoint>();
                    string[] lines = File.ReadAllLines(openFileDialog.FileName);
                    
                    // Basic CSV support: Time,Pressure or Volume,Pressure
                    foreach (var line in lines.Skip(1)) // Skip header
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 2 && double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y))
                        {
                            points.Add(new PumpDataPoint { Time = x, Volume = x, Pressure = y });
                        }
                    }

                    if (points.Any())
                    {
                        SelectedWellTest.PumpDataPoints = new ObservableCollection<PumpDataPoint>(points);
                        UpdateLotChart();
                        ToastNotificationService.Instance.ShowSuccess($"Imported {points.Count} data points for LOT.");
                    }
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Error importing CSV: {ex.Message}");
                }
            }
        }

        #endregion
    }
}
