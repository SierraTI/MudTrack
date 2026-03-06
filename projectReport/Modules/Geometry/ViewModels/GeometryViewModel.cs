using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Win32;
using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Services;
using ProjectReport.Services.DrillString;
using ProjectReport.Services.Survey;
using ProjectReport.Services.Wellbore;
using ProjectReport.ViewModels.Geometry.ThermalGradient;
using ProjectReport.Views.Geometry;
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
        private readonly DrillStringAutoAdjustService _autoAdjustService; // Auto-adjust drill string to bit depth
        private readonly WellboreHydraulicsService _hydraulicsService; // Wellbore & Hydraulics Integration service
        private readonly SurveyValidationService _surveyValidationService; // Survey validation service
        private const double DepthTolerance = 0.01;
        private SeriesCollection _surveySeriesCollection = new();
        private SeriesCollection _planViewSeriesCollection = new();
        private SeriesCollection _safetySeriesCollection = new();
        private SeriesCollection _lotSeriesCollection = new();
        private Well? _currentWell; // Reference to the current well being edited
        private string _wellName = string.Empty;
        private string _reportNumber = string.Empty;
        private string _operator = string.Empty;
        private string _location = string.Empty;
        private string _rigName = string.Empty;
        private string _rigType = string.Empty;
        private string _contractor = string.Empty;
        private int _selectedTabIndex;
        private bool _depthOverrunToastShown;
        private string _drillStringDepthErrorMessage = string.Empty;
        private string _bhaWarningMessage = string.Empty;
        private string _bhaInsertPosition = "Bottom";
        private readonly List<string> _bhaInsertPositions = new() { "Top", "Bottom" };
        public Report? CurrentReport { get; private set; }

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
            _autoAdjustService = new DrillStringAutoAdjustService(); // Initialize auto-adjust service
            _hydraulicsService = new WellboreHydraulicsService(); // Initialize hydraulics service
            _surveyValidationService = new SurveyValidationService(); // Initialize survey validation service

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
            var sectionTypes = new List<WellSectionType?> { null };
            sectionTypes.AddRange(Enum.GetValues(typeof(WellSectionType)).Cast<WellSectionType?>());
            WellboreSectionTypes = new ObservableCollection<WellSectionType?>(sectionTypes);

            var stages = new List<WellboreStage?> { null };
            stages.AddRange(Enum.GetValues(typeof(WellboreStage)).Cast<WellboreStage?>());
            WellboreStages = new ObservableCollection<WellboreStage?>(stages);

            // Component Types for Wellbore Geometry (Casing, Liner, OpenHole only)
            ComponentTypes = new ObservableCollection<ComponentType>(new[]
            {
                ComponentType.Casing,
                ComponentType.Liner,
                ComponentType.OpenHole
            });

            // Component Types for Drill String (all drill string components)
            DrillStringComponentTypes = new ObservableCollection<ComponentType>(new[]
            {
                ComponentType.DrillPipe,
                ComponentType.HWDP,
                ComponentType.DC,
                ComponentType.LWD,
                ComponentType.MWD,
                ComponentType.PWD,
                ComponentType.Motor,
                ComponentType.XO,
                ComponentType.Jar,
                ComponentType.Accelerator,
                ComponentType.NearBit,
                ComponentType.Stabilizer,
                ComponentType.Bit
            });


            WellTestTypes = new ObservableCollection<string>
            {
                "Leak Off", "Fracture gradient", "Pore pressure", "Integrity"
            };

            // Subscribe to collection changes
            WellboreComponents.CollectionChanged += OnWellboreCollectionChanged;
            DrillStringComponents.CollectionChanged += OnDrillStringCollectionChanged;
            SurveyPoints.CollectionChanged += OnSurveyCollectionChanged;
            WellboreComponents.CollectionChanged += (s, e) => OnPropertyChanged(nameof(WellboreSectionNames));

            // Initialize formatters
            YAxisLabelFormatter = value =>
            {
                if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
                return Math.Abs(value).ToString("N0");
            };

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
            foreach (var component in DrillStringComponents)
            {
                ValidateDrillStringComponent(component);   // Reglas S1-S5
                ValidateDrillVsWellbore(component);        // Validación OD vs Wellbore
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

        public void LoadReport(Report report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            // Desuscribir anterior
            if (CurrentReport != null)
                CurrentReport.PropertyChanged -= OnReportPropertyChanged;

            CurrentReport = report;
            // Suscribir nuevo
            CurrentReport.PropertyChanged += OnReportPropertyChanged;
            OnPropertyChanged(nameof(CurrentReport));
        }


        public void SyncGeometryWithReport()
        {
            if (CurrentReport == null || !CurrentReport.MD.HasValue)
                return;

            var lastSection = WellboreComponents
                .OrderBy(c => c.TopMD ?? double.MaxValue)
                .LastOrDefault();

            if (lastSection != null && lastSection.TopMD.HasValue)
            {
                if (CurrentReport.MD.Value > lastSection.TopMD.Value)
                {
                    lastSection.BottomMD = CurrentReport.MD.Value;
                }
            }

            RecalculateTotals();
        }

        private void OnReportPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Report.MD))
            {
                if (CurrentReport?.MD == null) return;

                var lastSection = WellboreComponents
                    .OrderBy(c => c.TopMD ?? double.MaxValue)
                    .LastOrDefault();

                if (lastSection != null)
                {
                    lastSection.BottomMD = CurrentReport.MD.Value;
                }
            }
        }



        private void InitializeSurveyChart()
        {
            // Initialize multiple series for multi-view visualization
            SurveySeriesCollection = new SeriesCollection
            {
                // Series 0: Vertical Section (Profile View)
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
            
            // Initialize Plan View series (North vs East)
            PlanViewSeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Plan View (North vs East)",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 6,
                    Stroke = Brushes.DarkGreen,
                    Fill = Brushes.Transparent,
                    LabelPoint = point => $"N: {point.X:N1} ft | E: {point.Y:N1} ft"
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
            
            // Propagation: Update all drill string components
            foreach (var component in DrillStringComponents)
            {
                component.FluidDensity = density;
            }
            
            RecalculateTotals();
        }

        private void OnGlobalDepthUpdated(object? sender, double newMD)
        {
             // If we want to auto-extend the last wellbore section or just alert?
             // For now, let's just toast
             ToastNotificationService.Instance.ShowInfo($"Global Depth Updated to {newMD} ft");
        }



        // Dropdown options
        public ObservableCollection<WellSectionType?> WellboreSectionTypes { get; }
        public ObservableCollection<WellboreStage?> WellboreStages { get; }

        public ObservableCollection<ComponentType> ComponentTypes { get; } // For Wellbore Geometry
        public ObservableCollection<ComponentType> DrillStringComponentTypes { get; } // For Drill String
        public ObservableCollection<string> WellTestTypes { get; }

        // Sub-ViewModels
        public ThermalGradientViewModel ThermalGradientViewModel { get; }
        
        // Wellbore section names for Well Test dropdown
        public ObservableCollection<string> WellboreSectionNames => 
            new ObservableCollection<string>(WellboreComponents.Select(w => w.Name).Where(n => !string.IsNullOrEmpty(n)));

        private bool _isProcessingCollectionChange = false;
        private bool _isLoading = false;

        private void OnWellboreCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isProcessingCollectionChange || _isLoading) return;

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

            // Re-validate all components after collection change
            foreach (var component in WellboreComponents)
            {
                ValidateWellboreComponent(component);
            }

            // 🔹 Sync Report MD with last section
            var lastSection = WellboreComponents
                .OrderBy(c => c.TopMD ?? double.MaxValue)
                .LastOrDefault();

            if (lastSection?.BottomMD != null && CurrentReport != null)
            {
                CurrentReport.MD = lastSection.BottomMD;
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
            if (_isProcessingCollectionChange || _isLoading) return;

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
            if (_isProcessingCollectionChange || _isLoading) return;
            _isProcessingCollectionChange = true;
            try
            {
                int idCounter = 1;
                double currentTopMD = 0; // Drill string starts from surface (RKB=0 in relative terms for string)

                foreach (var component in DrillStringComponents)
                {
                    component.Id = idCounter++;
                    
                    // Depth Chaining
                    // CRITICAL FIX: Capture Length BEFORE setting TopMD.
                    // If we set TopMD first, and it moves past the old BottomMD, the calculcated Length (Bottom-Top) 
                    // becomes negative. If we then use that negative length to set the new BottomMD, it corrupts the data.
                    double? currentLen = component.Length;
                    
                    component.TopMD = currentTopMD;
                    
                    if (currentLen.HasValue)
                    {
                        // Ensure no negative length carries over
                        double safeLen = currentLen.Value > 0 ? currentLen.Value : 0;
                        
                        component.BottomMD = currentTopMD + safeLen;
                        currentTopMD = component.BottomMD.Value;
                    }
                    else
                    {
                        component.BottomMD = null;
                        // Chain might stop here if length is undefined
                    }
                }
            }
            finally
            {
                _isProcessingCollectionChange = false;
            }
        }

        private void OnWellboreComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isLoading) return;

            if (e.PropertyName == nameof(WellboreComponent.TopMD) ||
                e.PropertyName == nameof(WellboreComponent.BottomMD) ||
                e.PropertyName == nameof(WellboreComponent.ID) ||
                e.PropertyName == nameof(WellboreComponent.OD) ||
                e.PropertyName == nameof(WellboreComponent.SectionType) ||
                e.PropertyName == nameof(WellboreComponent.Component) ||
                e.PropertyName == nameof(WellboreComponent.Washout))
            {
                if (sender is WellboreComponent component)
                {
                    // OpenHole Guard
                    if (e.PropertyName == nameof(WellboreComponent.Component) &&
                        component.Component == ComponentType.OpenHole)
                    {
                        component.ID = 0.0;
                    }

                    // Ordenar componentes por TopMD
                    var sorted = WellboreComponents
                        .OrderBy(c => c.TopMD ?? double.MaxValue)
                        .ToList();

                    int index = sorted.IndexOf(component);
                    var prev = index > 0 ? sorted[index - 1] : null;

                    // Calcular volumen del componente actual
                    _geometryService.CalculateWellboreComponentVolume(component, "Imperial", prev);

                    ValidateWellboreComponent(component);

                    // ================================
                    // DEPTH CHAINING
                    // ================================
                    if (e.PropertyName == nameof(WellboreComponent.BottomMD))
                    {
                        var next = index < sorted.Count - 1 ? sorted[index + 1] : null;

                        if (next != null)
                        {
                            next.SetPreviousBottomMD(component.BottomMD);
                            ValidateWellboreComponent(next);
                        }

                        // Validar si es la última sección
                        if (index == sorted.Count - 1)
                        {
                            ValidateLastSectionDepth(component);
                        }

                        // ====================================
                        // ACTUALIZAR MD DEL REPORTE ACTIVO
                        // ====================================
                        if (CurrentReport != null)
                        {
                            var deepest = WellboreComponents
                                .Where(c => c.BottomMD.HasValue)
                                .Max(c => c.BottomMD!.Value);

                            // ⚠ Solo aumentar el MD, nunca reducirlo automáticamente
                            if (!CurrentReport.MD.HasValue || deepest > CurrentReport.MD)
                            {
                                CurrentReport.MD = deepest;

                                System.Diagnostics.Debug.WriteLine(
                                    $"Report MD updated to {CurrentReport.MD}"
                                );
                            }
                        }
                    }

                    // ================================
                    // VOLUME CASCADING
                    // ================================
                    if (e.PropertyName == nameof(WellboreComponent.ID) ||
                        e.PropertyName == nameof(WellboreComponent.OD))
                    {
                        var next = index < sorted.Count - 1 ? sorted[index + 1] : null;

                        if (next != null)
                        {
                            _geometryService.CalculateWellboreComponentVolume(next, "Imperial", component);
                            ValidateWellboreComponent(next);
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

        /// <summary>
        /// Validates that the last wellbore section reaches or exceeds the bit depth.
        /// Adds a warning if the wellbore is shallower than the current drilling depth.
        /// </summary>
        private void ValidateLastSectionDepth(WellboreComponent lastSection)
        {
            if (lastSection == null) return;
            
            var bitDepth = WellContextService.Instance.CurrentDepth;
            if (bitDepth > 0 && lastSection.BottomMD.HasValue)
            {
                if (lastSection.BottomMD.Value < bitDepth)
                {
                    var message = $"⚠️ Wellbore ({lastSection.BottomMD:F0} ft) is shallower than bit depth ({bitDepth:F0} ft)";
                    lastSection.AddValidationWarning(message);
                }
                else
                {
                    // Clear warning if depth is now sufficient
                    lastSection.ClearValidationWarnings();
                }
            }
        }    

        private void CheckForCasingOverwrite(WellboreComponent current, WellboreComponent? previous)
        {
            if (previous != null && 
                (current.SectionType == ComponentType.Casing || current.SectionType == ComponentType.Liner) &&
                (previous.SectionType == ComponentType.Casing || previous.SectionType == ComponentType.Liner))
            {
                // Logic disabled to allow History Stacking without nagging.
                // The user explicitly wants to allow stacking/history.
            }
        }

        private void OnDrillStringComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isLoading) return;

            if (sender is DrillStringComponent component)
            {
                // Regla S4: Si el componente cambia a Bit, moverlo al final
                if (e.PropertyName == nameof(DrillStringComponent.ComponentType) && 
                    component.ComponentType == ComponentType.Bit)
                {
                    // Si no está al final, moverlo
                    if (DrillStringComponents.LastOrDefault() != component)
                    {
                        DrillStringComponents.Remove(component);
                        // Remover cualquier otro Bit existente
                        var existingBit = DrillStringComponents.FirstOrDefault(c => c.ComponentType == ComponentType.Bit);
                        if (existingBit != null)
                        {
                            DrillStringComponents.Remove(existingBit);
                        }
                        DrillStringComponents.Add(component);
                    }
                }

                if (e.PropertyName == nameof(DrillStringComponent.Length) || 
                    e.PropertyName == nameof(DrillStringComponent.OD) ||
                    e.PropertyName == nameof(DrillStringComponent.ID) ||
                    e.PropertyName == nameof(DrillStringComponent.ComponentType))
                {
                    // If length changed, we must re-chain the entire string to update and volumes
                    if (e.PropertyName == nameof(DrillStringComponent.Length) || e.PropertyName == nameof(DrillStringComponent.ComponentType))
                    {
                        RenumberDrillStringSections();
                    }

                    // Validar con las nuevas reglas S1-S5
                    ValidateDrillStringComponent(component);
                    RecalculateTotals();
                }
            }
        }

        /// <summary>
        /// Valida un componente de drill string usando las reglas S1-S5
        /// </summary>
        private void ValidateDrillStringComponent(DrillStringComponent component)
        {
            if (component == null) return;

            // 1. Limpiar errores solo de las propiedades que vamos a validar
            component.ClearErrors(nameof(component.OD));
            component.ClearErrors(nameof(component.ID));
            component.ClearErrors(nameof(component.Length));

         

            // 3. Validación contra Wellbore
            ValidateDrillVsWellbore(component);
        }





        private void OnSurveyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isLoading) return;
            if (e.NewItems != null)
            {
                foreach (SurveyPoint point in e.NewItems)
                {
                    point.PropertyChanged += OnSurveyPointChanged;
                    ValidateSurveyPoint(point);
                    
                    // Trigger initial calculation for new points (important for Import)
                    RecalculateSurveyTrajectory(point);
                }
            }
            if (e.OldItems != null)
            {
                foreach (SurveyPoint point in e.OldItems)
                {
                    point.PropertyChanged -= OnSurveyPointChanged;
                }
                
                // If points are removed, we must recalculate subsequent points
                // The easiest safe way is to recalc everything, or find the "gap"
                RecalculateAllSurveyTrajectories();
            }
            
            // Ensure surface point exists
            _surveyValidationService.EnsureSurfacePoint(SurveyPoints.ToList());
            
            // Re-validate all points after collection change (order may have changed)
            ValidateAllSurveyPoints();
            UpdateSurveyChart();
        }

        private void OnSurveyPointChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isLoading) return;

            // Only trigger calculation when input fields change (MD, HoleAngle, Azimuth)
            if (e.PropertyName == nameof(SurveyPoint.MD) || 
                e.PropertyName == nameof(SurveyPoint.HoleAngle) ||
                e.PropertyName == nameof(SurveyPoint.Azimuth))
            {
                if (sender is SurveyPoint point)
                {
                    // Recalculate trajectory for this point and all subsequent points
                    RecalculateSurveyTrajectory(point);
                    
                    // Trigger chart update
                    UpdateSurveyChart();
                }
            }
        }

        private void UpdateSurveyChart()
        {
            if (SurveySeriesCollection == null || SurveySeriesCollection.Count == 0)
            {
                // Initialize if not already initialized
                InitializeSurveyChart();
            }

            if (SurveySeriesCollection == null || SurveySeriesCollection.Count == 0) return;
            if (PlanViewSeriesCollection == null || PlanViewSeriesCollection.Count == 0) return;

            var sorted = SurveyPoints.OrderBy(p => p.MD).ToList();
            
            if (sorted.Count == 0)
            {
                // Clear charts if no data
                if (SurveySeriesCollection[0] is LineSeries vsS)
                    vsS.Values = new ChartValues<ObservablePoint>();
                if (PlanViewSeriesCollection[0] is LineSeries planS)
                    planS.Values = new ChartValues<ObservablePoint>();
                
                // Still update scaling to apply defaults
                UpdateSurveyChartScaling();
                return;
            }

            // Update Vertical Section chart (Profile View)
            var vsSeries = SurveySeriesCollection[0] as LineSeries;
            if (vsSeries != null)
            {
                var vsValues = new ChartValues<ObservablePoint>();
                foreach (var p in sorted)
                {
                    if (p == null) continue;
                    // X = Vertical Section (Horizontal Displacement), Y = TVD (Inverted)
                    double vs = p.VerticalSection;
                    double tvd = -p.TVD;
                    
                    if (double.IsNaN(vs) || double.IsInfinity(vs)) vs = 0;
                    if (double.IsNaN(tvd) || double.IsInfinity(tvd)) tvd = 0;
                    
                    vsValues.Add(new ObservablePoint(vs, tvd));
                }
                vsSeries.Values = vsValues;
            }

            // Update Plan View chart (North vs East)
            var planSeries = PlanViewSeriesCollection[0] as LineSeries;
            if (planSeries != null)
            {
                var planValues = new ChartValues<ObservablePoint>();
                foreach (var p in sorted)
                {
                    if (p == null) continue;
                    // X = North, Y = East
                    double n = p.Northing;
                    double e = p.Easting;
                    
                    if (double.IsNaN(n) || double.IsInfinity(n)) n = 0;
                    if (double.IsNaN(e) || double.IsInfinity(e)) e = 0;
                    
                    planValues.Add(new ObservablePoint(n, e));
                }
                planSeries.Values = planValues;
            }
            
            // Update auto-scaling properties
            UpdateSurveyChartScaling();
        }
        
        private void UpdateSurveyChartScaling()
        {
            if (SurveyPoints.Count == 0)
            {
                // Default ranges to avoid "invalid range" exception in LiveCharts
                MaxSurveyTVD = 1000;
                MaxSurveyVerticalSection = 100;
                MaxSurveyNorth = 100;
                MaxSurveyEast = 100;
            }
            else
            {
                var sorted = SurveyPoints.Where(p => p != null).OrderBy(p => p.MD).ToList();
                if (sorted.Count == 0)
                {
                    MaxSurveyTVD = 1000;
                    MaxSurveyVerticalSection = 100;
                    MaxSurveyNorth = 100;
                    MaxSurveyEast = 100;
                    return;
                }
                
                // Calculate max values for auto-scaling with sensible minimums to avoid 0-range
                double maxTVD = sorted.Max(p => p.TVD);
                double maxVS = sorted.Max(p => p.VerticalSection);
                double maxN = sorted.Max(p => Math.Abs(p.Northing));
                double maxE = sorted.Max(p => Math.Abs(p.Easting));

                MaxSurveyTVD = Math.Max(double.IsNaN(maxTVD) ? 0 : maxTVD, 1000);
                MaxSurveyVerticalSection = Math.Max(double.IsNaN(maxVS) ? 0 : maxVS, 100);
                MaxSurveyNorth = Math.Max(double.IsNaN(maxN) ? 0 : maxN, 100);
                MaxSurveyEast = Math.Max(double.IsNaN(maxE) ? 0 : maxE, 100);
            }
            
            OnPropertyChanged(nameof(MaxSurveyTVD));
            OnPropertyChanged(nameof(MaxSurveyVerticalSection));
            OnPropertyChanged(nameof(MaxSurveyNorth));
            OnPropertyChanged(nameof(MaxSurveyEast));
        }
        
        private double _bitDepth;
        public double BitDepth
        {
            get => _bitDepth;
            set
            {
                if (SetProperty(ref _bitDepth, value))
                {
                    RecalculateTotals();
                }
            }
        }

        private double _fluidLevel;
        public double FluidLevel
        {
            get => _fluidLevel;
            set
            {
                if (SetProperty(ref _fluidLevel, value))
                {
                    RecalculateTotals();
                }
            }
        }

        private double _volumeBelowBit;
        public double VolumeBelowBit
        {
            get => _volumeBelowBit;
            set => SetProperty(ref _volumeBelowBit, value);
        }

        private double _activeAnnularVolume;
        public double ActiveAnnularVolume
        {
            get => _activeAnnularVolume;
            set => SetProperty(ref _activeAnnularVolume, value);
        }

        private double _airGapVolume;
        public double AirGapVolume
        {
            get => _airGapVolume;
            set => SetProperty(ref _airGapVolume, value);
        }

        // --- Existing Properties ---
        // Properties for auto-scaling charts
        public double MaxSurveyTVD { get; private set; } = 1000;
        public double MaxSurveyVerticalSection { get; private set; } = 100;
        public double MaxSurveyNorth { get; private set; } = 100;
        public double MaxSurveyEast { get; private set; } = 100;
        
        // Report MD for marker display (returns TVD at Report MD)
        public double ReportMD
        {
            get
            {
                var reportMD = WellContextService.Instance.CurrentDepth;
                if (reportMD <= 0 || SurveyPoints.Count == 0) return 0;
                
                // Find the TVD corresponding to the Report MD
                var sorted = SurveyPoints.OrderBy(p => p.MD).ToList();
                var pointAtOrBefore = sorted.LastOrDefault(p => p.MD <= reportMD);
                
                if (pointAtOrBefore != null)
                {
                    // If exact match, return TVD
                    if (Math.Abs(pointAtOrBefore.MD - reportMD) < 0.01)
                        return pointAtOrBefore.TVD;
                    
                    // Interpolate TVD between points
                    var nextPoint = sorted.FirstOrDefault(p => p.MD > reportMD);
                    if (nextPoint != null)
                    {
                        double deltaMD = nextPoint.MD - pointAtOrBefore.MD;
                        if (Math.Abs(deltaMD) < 0.001) return pointAtOrBefore.TVD;
                        
                        double ratio = (reportMD - pointAtOrBefore.MD) / deltaMD;
                        return pointAtOrBefore.TVD + ratio * (nextPoint.TVD - pointAtOrBefore.TVD);
                    }
                    
                    return pointAtOrBefore.TVD;
                }
                
                return 0;
            }
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

        /// <summary>
        /// Validates all survey points using the comprehensive validation service
        /// </summary>
        private void ValidateAllSurveyPoints()
        {
            var validationErrors = _surveyValidationService.ValidateSurvey(SurveyPoints.ToList());
            
            // Clear previous errors
            foreach (var point in SurveyPoints)
            {
                point.ClearErrors(null);
            }
            
            // Apply validation errors to points
            foreach (var error in validationErrors)
            {
                var point = SurveyPoints.FirstOrDefault(p => p.Id == error.PointId);
                if (point != null)
                {
                    // Add error to the appropriate property
                    if (error.Message.Contains("MD"))
                        point.AddError(nameof(SurveyPoint.MD), error.Message);
                    else if (error.Message.Contains("Hole Angle") || error.Message.Contains("Inclination"))
                        point.AddError(nameof(SurveyPoint.HoleAngle), error.Message);
                    else if (error.Message.Contains("Azimuth"))
                        point.AddError(nameof(SurveyPoint.Azimuth), error.Message);
                    else if (error.Message.Contains("TVD"))
                        point.AddError(nameof(SurveyPoint.TVD), error.Message);
                    else
                        point.AddError(string.Empty, error.Message);
                }
            }
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

        public string RigType
        {
            get => _rigType;
            set => SetProperty(ref _rigType, value);
        }

        public string Contractor
        {
            get => _contractor;
            set => SetProperty(ref _contractor, value);
        }

        // Collections
        public ObservableCollection<WellboreComponent> WellboreComponents { get; }
        public ObservableCollection<DrillStringComponent> DrillStringComponents { get; }
        public ObservableCollection<SurveyPoint> SurveyPoints { get; }
        public ObservableCollection<WellTest> WellTests { get; }
        public ObservableCollection<AnnularVolumeDetail> AnnularVolumeDetails { get; }

        public Func<double, string> YAxisLabelFormatter { get; private set; }

        public double AnnularVolumePercent => TotalCirculationVolume > 0 ? (TotalAnnularVolume / TotalCirculationVolume) * 100 : 0;
        public double StringVolumePercent => TotalCirculationVolume > 0 ? (TotalDrillStringVolume / TotalCirculationVolume) * 100 : 0;

        public SeriesCollection SurveySeriesCollection
        {
            get => _surveySeriesCollection;
            set => SetProperty(ref _surveySeriesCollection, value);
        }
        
        public SeriesCollection PlanViewSeriesCollection
        {
            get => _planViewSeriesCollection;
            set => SetProperty(ref _planViewSeriesCollection, value);
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

        public ICommand ForceToBottomCommand => new RelayCommand(_ => ExecuteAutoAdjustToBottom(), _ => CanAutoAdjustToBottom);
        public ICommand AddWellTestCommand => new RelayCommand(AddWellTest);
        public ICommand SyncWellTestDataCommand => new RelayCommand(SyncWellTestData, _ => SelectedWellTest != null);

        // Drill String Row Commands
        public ICommand AddDrillStringComponentCommand => new RelayCommand(AddDrillStringComponent);
        public ICommand DeleteDrillStringComponentCommand => new RelayCommand(DeleteDrillStringComponent);
        public ICommand MoveDrillStringUpCommand => new RelayCommand(MoveDrillStringUp);
        public ICommand MoveDrillStringDownCommand => new RelayCommand(MoveDrillStringDown);

        // Dashboard Commands
        private ICommand? _exportToPdfCommand;
        public ICommand ExportToPdfCommand => _exportToPdfCommand ??= new RelayCommand(ExecuteExportToPdf);
        
        private ICommand? _editGeometryCommand;
        public ICommand EditGeometryCommand => _editGeometryCommand ??= new RelayCommand(_ => SelectedTabIndex = 0);
        
        private ICommand? _editStringCommand; 
        public ICommand EditStringCommand => _editStringCommand ??= new RelayCommand(_ => SelectedTabIndex = 1);

        public bool CanAutoAdjustToBottom
        {
            get
            {
                return TotalWellboreMD > 0 && DrillStringComponents.Count > 0;
            }
        }

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
        





        // Wellbore commands
        public ICommand AddWellboreSectionCommand => new RelayCommand(AddWellboreSection);
        public ICommand DeleteWellboreSectionCommand => new RelayCommand(DeleteWellboreSection);
        private void AddWellboreSection(object? parameter)
        {
            // Obtener la última sección si existe
            var lastSection = WellboreComponents
                .OrderBy(c => c.TopMD ?? double.MaxValue)
                .LastOrDefault();

            double initialTopMD = 0.0;
            double bottomMD = 0.0;

            if (lastSection != null && lastSection.BottomMD.HasValue)
            {
                // TopMD inicia donde terminó la anterior
                initialTopMD = lastSection.BottomMD.Value;

                // BottomMD será el doble del anterior
                bottomMD = lastSection.BottomMD.Value * 2;
            }
            else if (_currentWell?.RigProfile != null)
            {
                double rkb = _currentWell.RigProfile.RkbElevation;
                double wh = _currentWell.RigProfile.CasingHeadElevation;

                initialTopMD = (rkb > 0 && wh > 0 && rkb > wh) ? rkb - wh : 0.0;

                // Si no hay sección previa, usar MD del reporte o valor base
                if (CurrentReport != null && CurrentReport.MD.HasValue)
                {
                    bottomMD = CurrentReport.MD.Value;
                }
                else
                {
                    bottomMD = initialTopMD + 100; // fallback seguro
                }
            }

            // Crear nueva sección
            var newSection = new WellboreComponent
            {
                Id = GetNextWellboreId(),
                Name = string.Empty,
                SectionType = default,
                TopMD = initialTopMD,
                BottomMD = bottomMD,
                OD = null,
                ID = null,
                Washout = null
            };

            WellboreComponents.Add(newSection);
            newSection.PropertyChanged += OnWellboreComponentChanged;

            ValidateWellboreComponent(newSection);
            RecalculateTotals();

            // Actualizar MD del reporte activo
            if (CurrentReport != null)
            {
                CurrentReport.MD = newSection.BottomMD;
            }
        }



        private void DeleteWellboreSection(object? parameter)
        {
            if (parameter is WellboreComponent section)
            {
                WellboreComponents.Remove(section);
            }
        }

        private void AddDrillStringComponent(object? parameter)
        {
            // 1️⃣ Validate that at least one Wellbore section exists
            if (WellboreComponents == null || WellboreComponents.Count == 0)
            {
                MessageBox.Show(
                    "Cannot add a Drill String because there are no Wellbore sections.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return; // Exit without adding
            }

            // 2️⃣ Check if there are any Drill Strings with errors
            if (DrillStringComponents.Any(c => !c.IsValid))
            {
                MessageBox.Show(
                    "Please fix OD/ID errors in the existing Drill String components before adding a new one.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return; // Do not add while errors exist
            }

            // 3️⃣ Create a new DrillStringComponent
            var newComponent = new DrillStringComponent
            {
                Id = GetNextDrillStringId(),
                Name = "Drill Pipe",
                ComponentType = ComponentType.DrillPipe,
                Length = 100.0,
                OD = 5.0,   // Initial value
                ID = 4.276, // Initial value
                WellboreComponents = WellboreComponents // Associate Wellbore collection
            };

            // 4️⃣ Immediate validation upon creation
            newComponent.ValidateODDrill();

            // 5️⃣ Check if the new component is valid
            if (!newComponent.IsValid)
            {
                MessageBox.Show(
                    $"Cannot add Drill String component:\n{newComponent.ValidationMessage}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return; // Do not add until corrected
            }

            // 6️⃣ Add to Drill String
            DrillStringComponents.Add(newComponent);

            // 7️⃣ Recalculate MD and totals
            RenumberDrillStringSections();
            RecalculateTotals();
        }



        private void DeleteDrillStringComponent(object? parameter)
        {
            if (parameter is DrillStringComponent component)
            {
                DrillStringComponents.Remove(component);
            }
        }

        private void MoveDrillStringUp(object? parameter)
        {
            if (parameter is DrillStringComponent component)
            {
                int index = DrillStringComponents.IndexOf(component);
                if (index > 0)
                {
                    DrillStringComponents.Move(index, index - 1);
                }
            }
        }

        private void MoveDrillStringDown(object? parameter)
        {
            if (parameter is DrillStringComponent component)
            {
                int index = DrillStringComponents.IndexOf(component);
                if (index < DrillStringComponents.Count - 1)
                {
                    DrillStringComponents.Move(index, index + 1);
                }
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
        public ICommand AddSurveyPointCommand => new RelayCommand(AddSurveyPoint);

        private void AddSurveyPoint(object? parameter)
        {
            // Mirror logic from view's code-behind to add a survey point with smart defaults
            // Check if surface point (MD=0) exists
            var surfacePoint = SurveyPoints.FirstOrDefault(p => Math.Abs(p.MD) < 0.01);

            if (surfacePoint == null && SurveyPoints.Count == 0)
            {
                var newPoint = new SurveyPoint
                {
                    Id = GetNextSurveyId(),
                    MD = 0,
                    HoleAngle = 0,
                    Azimuth = 0,
                    IsTieInPoint = true
                };
                newPoint.PropertyChanged += OnSurveyPointChanged;
                SurveyPoints.Add(newPoint);
                // Recalculate and validate
                RecalculateSurveyTrajectory(newPoint);
                ValidateAllSurveyPoints();
                UpdateSurveyChart();
                RecalculateTotals();
            }
            else
            {
                var sorted = SurveyPoints.OrderBy(p => p.MD).ToList();
                double nextMD = sorted.Count > 0 ? sorted.Last().MD + 100 : 100;

                var newPoint = new SurveyPoint
                {
                    Id = GetNextSurveyId(),
                    MD = nextMD,
                    HoleAngle = sorted.Count > 0 ? sorted.Last().HoleAngle : 0,
                    Azimuth = sorted.Count > 0 ? sorted.Last().Azimuth : 0
                };
                newPoint.PropertyChanged += OnSurveyPointChanged;
                SurveyPoints.Add(newPoint);
                RecalculateSurveyTrajectory(newPoint);
                ValidateAllSurveyPoints();
                UpdateSurveyChart();
                RecalculateTotals();
            }
        }

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
                    // Check if there are hard errors (containing "Error:" or "excede")
                    bool hasHardError = ThermalGradientViewModel.ValidationMessage.Contains("Error:") || 
                                       ThermalGradientViewModel.ValidationMessage.Contains("excede") ||
                                       ThermalGradientViewModel.ValidationMessage.Contains("ordering");

                    if (hasHardError)
                    {
                        ToastNotificationService.Instance.ShowError($"Cannot save project. Thermal Gradient has critical errors:\n\n{ThermalGradientViewModel.ValidationMessage}");
                        return;
                    }

                    // Otherwise, treat as warnings and ask for confirmation
                    var result = MessageBox.Show(
                        $"Thermal Gradient module has validation warnings:\n\n{ThermalGradientViewModel.ValidationMessage}\n\nDo you want to save anyway?",
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

            _isLoading = true;
            try
            {
                _currentWell = well; // Store reference to the well
                WellName = well.WellName;
                RigName = well.RigName;
                RigType = well.RigType;
                Contractor = well.Contractor;
                Location = well.Location;
                Operator = well.Operator;

                // Load Wellbore Components
                WellboreComponents.Clear();
                foreach (var component in well.WellboreComponents)
                {
                    // Note: CollectionChanged handler handles subscription, preventing double sub
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
                     // Note: CollectionChanged handler handles subscription
                     DrillStringComponents.Add(component);
                }

                // Load Survey Points
                SurveyPoints.Clear();
                foreach (var point in well.SurveyPoints)
                {
                    SurveyPoints.Add(point);
                }
                
                // Ensure surface point (MD=0) exists
                _surveyValidationService.EnsureSurfacePoint(SurveyPoints.ToList());
                
                // Recalculate all survey trajectories after loading
                RecalculateAllSurveyTrajectories();
                
                // Validate all survey points
                ValidateAllSurveyPoints();

                // Load Well Tests
                WellTests.Clear();
                foreach (var test in well.WellTests)
                {
                    WellTests.Add(test);
                }

                // Load Thermal Gradient Points
                ThermalGradientViewModel.IsLoading = true;
                ThermalGradientViewModel.ThermalGradientPoints.Clear();
                foreach (var point in well.ThermalGradientPoints)
                {
                    ThermalGradientViewModel.ThermalGradientPoints.Add(point);
                }
            }
            finally
            {
                _isLoading = false;
                if (ThermalGradientViewModel != null) ThermalGradientViewModel.IsLoading = false;
                
                // Final Refresh and Calculations
                RecalculateTotals();
                RenumberWellboreSections();
                RenumberDrillStringSections();
                UpdateSurveyChart();
                
                // Update MaxWellboreTVD for thermal gradient validation
                if (ThermalGradientViewModel != null && WellboreComponents.Count > 0)
                {
                    var maxTVD = WellboreComponents.Max(w => w.BottomMD ?? 0);
                    ThermalGradientViewModel.MaxWellboreTVD = maxTVD;
                }

                // Sync Thermal Gradient with latest Report data (MaxBHT and Report TVD)
                if (ThermalGradientViewModel != null && well.LastReport != null)
                {
                    var report = well.LastReport;
                    ThermalGradientViewModel.SyncWithReport(report.TVD, report.MaxBHT);
                }
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

        /// <summary>
        /// Open-end steel displacement of ALL drill string components (bbl).
        /// Formula per component: (OD² - ID²) / 1029.4 × Length
        /// This is the volume of fluid displaced when the string is run in hole.
        /// </summary>
        public double TotalStringDisplacement { get; private set; }

        /// <summary>
        /// Fluid capacity of the wellbore with the drill string in place.
        /// = HoleCapacity (TotalWellboreVolume) − StringDisplacement
        /// </summary>
        public double TheoreticalWellboreVolume { get; private set; }

        // Circulation & Trip Volume (Off-Bottom)
        private double? _currentBitDepth;
        public double CurrentBitDepth
        {
            get => (_currentBitDepth.HasValue && _currentBitDepth.Value > 0) ? _currentBitDepth.Value : TotalDrillStringLength;
            set
            {
                if (SetProperty(ref _currentBitDepth, value))
                {
                    RecalculateTotals();
                }
            }
        }

        public double CalculatedInternalStringVolume { get; private set; }
        public double CalculatedAnnularActiveVolume { get; private set; }
        public double CalculatedOpenHoleVolumeBelowBit { get; private set; }
        public double CalculatedActivePitsVolume { get; private set; }
        // Active Circulation Volume (Systems actively moving fluid: Surface + Internal + Active Annular - AirGap)
        public double CalculatedActiveCirculationVolume => CalculatedActivePitsVolume + CalculatedInternalStringVolume + CalculatedAnnularActiveVolume - (AirGapVolume > 0 ? AirGapVolume : 0);
        
        // Total Mud In System (Active + Stagnant Hole)
        public double CalculatedTotalMudInSystem => CalculatedActiveCirculationVolume + CalculatedOpenHoleVolumeBelowBit;

        public double TotalSystemVolume { get; private set; }
        public double TotalWellboreMD { get; private set; }
        public double TotalWellboreTVD { get; private set; } // New Property for Real TVD
        public double ShoeDepth { get; private set; }
        public string ContinuityError { get; private set; } = string.Empty;
        
        // Hydraulics metrics

        /// <summary>
        /// Returns a warning message if any steps in the master flow were skipped.
        /// Used in Summary tab to alert users of incomplete data flow.
        /// </summary>
        public string MissingStepsWarning
        {
            get
            {
                var missingSteps = WellContextService.Instance.GetMissingSteps();
                if (missingSteps.Count == 0)
                    return string.Empty;
                
                return $"⚠️ Warning: The following steps were skipped: {string.Join(", ", missingSteps)}";
            }
        }

        /// <summary>
        /// Returns warning messages if wellbore geometry or survey don't reach bit depth.
        /// Used in Summary tab to alert users of depth inconsistencies.
        /// </summary>
        public string DepthConsistencyWarning
        {
            get
            {
                var warnings = new List<string>();
                var bitDepth = WellContextService.Instance.CurrentDepth;
                
                if (bitDepth > 0)
                {
                    // Check wellbore vs bit depth
                    var lastWellbore = WellboreComponents
                        .Where(c => c.BottomMD.HasValue)
                        .OrderByDescending(c => c.BottomMD)
                        .FirstOrDefault();
                    
                    if (lastWellbore != null && lastWellbore.BottomMD.HasValue)
                    {
                        if (lastWellbore.BottomMD.Value < bitDepth)
                        {
                            warnings.Add($"Wellbore geometry ({lastWellbore.BottomMD:F0} ft) does not reach bit depth ({bitDepth:F0} ft)");
                        }
                    }
                    
                    // Check survey vs bit depth
                    var lastSurvey = SurveyPoints
                        .OrderByDescending(p => p.MD)
                        .FirstOrDefault();
                    
                    if (lastSurvey != null && lastSurvey.MD < bitDepth)
                    {
                        warnings.Add($"Survey trajectory ({lastSurvey.MD:F0} ft) does not reach bit depth ({bitDepth:F0} ft)");
                    }
                }
                
                return warnings.Count > 0 ? "⚠️ " + string.Join(" | ", warnings) : string.Empty;
            }
        }

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
                    ExecuteAutoAdjustToBottom();
                }
                OnPropertyChanged(nameof(FeetMissing));
                OnPropertyChanged(nameof(DepthDifferential));
            }
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
                
                // Target Depth Logic (Rule 6: Subtract RKB)
                double targetDepth = TotalWellboreMD;
                var rig = WellContextService.Instance.CurrentWell?.RigProfile;
                if (rig != null && rig.RkbElevation > 0)
                {
                    // If TotalWellboreMD matches Report MD, we subtract RKB
                    targetDepth = Math.Max(0, TotalWellboreMD - rig.RkbElevation);
                }
                
                return targetDepth - totalDrillStringLength;
            }
        }

        public bool HasDrillStringDepthError => TotalWellboreMD > 0 && DepthDifferential < -DepthTolerance;

        public string DrillStringDepthErrorMessage
        {
            get => _drillStringDepthErrorMessage;
            private set => SetProperty(ref _drillStringDepthErrorMessage, value);
        }

        public bool CanForceToBottom => !HasDrillStringDepthError && TotalWellboreMD > 0 && DrillStringComponents.Count > 0;

        private void ExecuteExportToPdf(object? parameter)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = $"Geometry_Summary_{DateTime.Now:yyyyMMdd}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var exportService = new ProjectReport.Services.ExportService();
                    exportService.ExportAnnularVolumeDetailsToCsv(AnnularVolumeDetails, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess("Data exported successfully to CSV.");
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Export failed: {ex.Message}");
                }
            }
        }

        // Dashboard Data
        public IEnumerable<ProjectReport.Models.Rig.RigPit> ActivePits => 
            _currentWell?.RigProfile?.Pits.Where(p => p.IsActive) ?? Enumerable.Empty<ProjectReport.Models.Rig.RigPit>();

        public IEnumerable<ProjectReport.Models.Rig.RigSurfaceEquipment> ServiceLines => 
            _currentWell?.RigProfile?.SurfaceEquipment ?? Enumerable.Empty<ProjectReport.Models.Rig.RigSurfaceEquipment>();

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
                if (diff > 0) return $"Short: {diff:F2} ft"; // Positive - not reaching
                return $"Overrun: {Math.Abs(diff):F2} ft"; // Negative - exceeds TD
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

            // Regla S4: Bit siempre debe ser el último componente
            if (componentType == ComponentType.Bit)
            {
                // Si ya hay un Bit, removerlo primero
                var existingBit = DrillStringComponents.FirstOrDefault(c => c.ComponentType == ComponentType.Bit);
                if (existingBit != null)
                {
                    DrillStringComponents.Remove(existingBit);
                }
                // Agregar el Bit al final
                DrillStringComponents.Add(component);
            }
            else
            {
                // Para otros componentes, insertar antes del Bit (si existe) o al final
                var bitComponent = DrillStringComponents.FirstOrDefault(c => c.ComponentType == ComponentType.Bit);
                if (bitComponent != null)
                {
                    int bitIndex = DrillStringComponents.IndexOf(bitComponent);
                    DrillStringComponents.Insert(bitIndex, component);
                }
                else
                {
                    if (string.Equals(BhaInsertPosition, "Top", StringComparison.OrdinalIgnoreCase))
                    {
                        DrillStringComponents.Insert(0, component);
                    }
                    else
                    {
                        DrillStringComponents.Add(component);
                    }
                }
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
            var openHole = WellboreComponents.LastOrDefault(c => c.SectionType == ComponentType.OpenHole);
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

            // Rule: Adjust only the Drill Pipe component
            var components = DrillStringComponents.ToList();
            var drillPipe = _autoAdjustService.GetDrillPipeComponent(components);
            
            if (drillPipe == null)
            {
                // If no DP, we cannot safely "stretch" the string automatically
                return;
            }

            var bhaComponents = _autoAdjustService.GetBHAComponents(components);
            double bhaLength = _autoAdjustService.GetBHATotalLength(bhaComponents);
            
            double newLength = TotalWellboreMD - bhaLength;

            // If BHA is shorter than MD, we can adjust the DP
            if (newLength > DepthTolerance)
            {
                double oldLength = drillPipe.Length.GetValueOrDefault();
                
                // Update the drill pipe length
                drillPipe.Length = newLength;
                
                // Highlight the adjusted field
                drillPipe.IsHighlighted = true;
                
                // Remove highlight after 2 seconds
                Task.Delay(2000).ContinueWith(_ => 
                {
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        drillPipe.IsHighlighted = false;
                    });
                });
            }
        }

        /// <summary>
        /// Adjusts the first component (usually Drill Pipe) to reach the wellbore bottom depth.
        /// </summary>
        /// <summary>
        /// Adjusts the Drill Pipe length to reach the wellbore bottom depth, explicitly protecting BHA components.
        /// Formula: DP_Length = Total_MD - Sum(All_Other_Components)
        /// </summary>
        /// <summary>
        /// Adjusts the FIRST component (Index 0) to reach the wellbore bottom depth.
        /// Rule: Top Component (Index 0) is elastic; all others (Index 1+) are Fixed/BHA.
        /// Formula: TopComponent_Length = Total_MD - Sum(Fixed_Components)
        /// </summary>
        private void ExecuteAutoAdjustToBottom()
        {
            var reportMD = TotalWellboreMD;
            
            if (reportMD <= 0)
            {
                MessageBox.Show(
                    "Wellbore bottom depth is not defined. Please add wellbore sections first.",
                    "Hole Bottom Not Defined",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var components = DrillStringComponents.ToList();
            
            if (components.Count == 0)
            {
                MessageBox.Show(
                    "Drill string is empty. Please add components.",
                    "Empty String",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // CRITICAL FIX: Clear TopMD/BottomMD on ALL components first
            // This ensures they use the _length field which is protected from negatives
            foreach (var comp in components)
            {
                comp.TopMD = null;
                comp.BottomMD = null;
            }

            // 1. Identify Top Component (Index 0) - The ONLY one to adjust
            var topComponent = components[0];

            // 2. Calculate Fixed Length (BHA = all components EXCEPT the first one)
            // Sum of ALL components from Index 1 to End
            double fixedComponentsLength = components.Skip(1).Sum(c => c.Length.GetValueOrDefault());
            
            // 3. Calculate Required Top Component Length
            // Formula: Length_{Top} = MD_{Total} - Sum(BHA)
            double requiredLength = reportMD - fixedComponentsLength;

            // 4. Safety Lock (Validation)
            if (requiredLength < 0)
            {
                MessageBox.Show(
                    $"ERROR: BHA length ({fixedComponentsLength:F2} ft) exceeds Well Depth ({reportMD:F2} ft).\n\n" + 
                    $"The BHA components are too long to fit in the well.\n" +
                    $"Drill Pipe length has been set to 0 ft.\n\n" +
                    $"Please reduce BHA component lengths by at least {Math.Abs(requiredLength):F2} ft.",
                    "BHA Exceeds Well Depth",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                // Force Drill Pipe to 0 to prevent negative math
                topComponent.Length = 0;
                RecalculateTotals();
                return;
            }

            double oldLength = topComponent.Length.GetValueOrDefault();
            double difference = requiredLength - oldLength;

            if (Math.Abs(difference) < DepthTolerance)
            {
                ToastNotificationService.Instance.ShowInfo("Drill string is already on bottom.");
                return;
            }

            // 5. Update and Feedback
            try 
            {
                // Set the new length for the top component (Drill Pipe)
                // TopMD and BottomMD are already null from the loop above
                topComponent.Length = requiredLength;
                
                // Snap Bit Depth to Bottom (Reset Manual Override)
                _currentBitDepth = null; 
                OnPropertyChanged(nameof(CurrentBitDepth));

                topComponent.IsHighlighted = true;

                // Remove highlight after 2 seconds
                Task.Delay(2000).ContinueWith(_ => 
                {
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        topComponent.IsHighlighted = false;
                    });
                });

                // Recalculate totals
                RecalculateTotals();

                // Show notification
                ToastNotificationService.Instance.ShowSuccess(
                    $"✓ Adjusted to Bottom: {topComponent.ComponentTypeString} updated from {oldLength:F2} ft to {requiredLength:F2} ft.");
            }
            catch (Exception ex)
            {
                 MessageBox.Show(
                    $"Error adjusting length: {ex.Message}",
                    "Adjustment Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            // Update "Behind Pipe" status (Recipe 3: The 20" remains but is inactive)
            UpdateBehindPipeStatus();

            TotalWellboreVolume = _geometryService.CalculateTotalWellboreVolume(WellboreComponents, "Imperial");
            TotalDrillStringVolume = _geometryService.CalculateTotalDrillStringVolume(DrillStringComponents, false, "Imperial"); // Internal Volume
            
            // -------------------------------------------------------------------------
            // CIRCULATION & TRIP VOLUME CALCULATION (OFF-BOTTOM LOGIC)
            // -------------------------------------------------------------------------
            double calcBitDepth = (CurrentBitDepth > 0) ? CurrentBitDepth : TotalDrillStringLength;
            
            // 1. Zone B: Open Hole Below Bit
            // Volume of the hole from BitDepth to TD (Empty of pipe)
            CalculatedOpenHoleVolumeBelowBit = _geometryService.CalculateWellboreCapacityBelowDepth(WellboreComponents, calcBitDepth);

            // 2. Build Active String Components (Sliced Bottom-Up)
            // We want to keep the BHA (bottom) and trim the Pipe (top) to match BitDepth
            var slicedComponents = new List<DrillStringComponent>();
            double remainingDepth = calcBitDepth; 
            
            // Iterate Bottom-Up (Index Count-1 to 0) to keep bottom components
            for (int i = DrillStringComponents.Count - 1; i >= 0; i--)
            {
                var original = DrillStringComponents[i];
                double len = original.Length.GetValueOrDefault();
                if (remainingDepth <= 0) break;
                
                double take = Math.Min(len, remainingDepth);
                
                // Create copy with new length for calculation.
                // IMPORTANT: Copies do not have TopMD/BottomMD set automatically.
                var copy = new DrillStringComponent 
                { 
                     Name = original.Name,
                     OD = original.OD,
                     ID = original.ID,
                     Length = take,
                     ComponentType = original.ComponentType
                };
                slicedComponents.Add(copy);
                remainingDepth -= take;
            }
            
            // Do NOT reverse here. The loop above (Count-1 to 0) builds the list Top-Down (Pipe -> Bit).
            // We want Top-Down order for MD assignment starting from Surface (0).
            // slicedComponents.Reverse(); 

            
            // Assign MDs to sliced components (Required for HydraulicsService)
            double runningDepth = 0;
            foreach (var c in slicedComponents)
            {
                c.TopMD = runningDepth;
                // Length is already set, so BottomMD calculates automatically in setter? 
                // No, only if TopMD is set FIRST, then Length. 
                // Here Length is set in init. TopMD set now.
                // We must ensure BottomMD is set.
                c.BottomMD = runningDepth + c.Length.GetValueOrDefault();
                runningDepth = c.BottomMD.Value;
            }

            // 3. Calculated Internal String Volume
            // Use the sliced components (with corrected lengths)
            CalculatedInternalStringVolume = slicedComponents.Sum(c => c.InternalVolume);

            // 4. Calculated Active Annular Volume (Zone A)
            // Use HydraulicsService to ensure consistency with the Segment Table.
            // Filter: OD > 0 (String Exists) => Active Annulus.
            var activeSegments = _hydraulicsService.CalculateAnnularSegments(
                WellboreComponents, 
                slicedComponents
            );
            CalculatedAnnularActiveVolume = activeSegments
                .Where(x => x.DrillStringOD > 0)
                .Sum(x => x.Volume);
            
            // 5. Active Pits
            CalculatedActivePitsVolume = ActivePits.Sum(p => p.CurrentVolume);

            // -------------------------------------------------------------------------

            // Air Gap
            AirGapVolume = _geometryService.CalculateWellboreCapacityAboveDepth(WellboreComponents, FluidLevel);
            
            TotalCirculationVolume = CalculatedAnnularActiveVolume + CalculatedOpenHoleVolumeBelowBit + CalculatedInternalStringVolume - AirGapVolume;
            
            // Legacy Binding Support (TotalSystemVolume)
            TotalSystemVolume = TotalCirculationVolume + CalculatedActivePitsVolume;
            
            // Notify Properties
            OnPropertyChanged(nameof(TotalSystemVolume));
            OnPropertyChanged(nameof(TotalCirculationVolume));
            OnPropertyChanged(nameof(CalculatedInternalStringVolume));
            OnPropertyChanged(nameof(CalculatedAnnularActiveVolume));
            OnPropertyChanged(nameof(CalculatedOpenHoleVolumeBelowBit));
            OnPropertyChanged(nameof(CalculatedActivePitsVolume));
            OnPropertyChanged(nameof(CalculatedActiveCirculationVolume));
            OnPropertyChanged(nameof(CalculatedTotalMudInSystem));

            TotalWellboreMD = WellboreComponents.Count > 0 ? WellboreComponents.Max(w => w.BottomMD ?? 0) : 0;
            
            // Calculate Shoe Depth: BottomMD of the deepest Casing or Liner section
            var lastCasing = WellboreComponents
                .Where(c => c.SectionType == ComponentType.Casing || c.SectionType == ComponentType.Liner)
                .OrderByDescending(c => c.BottomMD)
                .FirstOrDefault();
            ShoeDepth = lastCasing?.BottomMD ?? 0;
            
            // Update Thermal Gradient context with survey depth information
            var maxSurveyTvd = SurveyPoints.Count > 0 ? SurveyPoints.Max(p => p.TVD) : 0;
            // If no survey, assume vertical (TVD = MD)
            double calculatedTVD = (maxSurveyTvd > 0) ? maxSurveyTvd : TotalWellboreMD;
            
            TotalWellboreTVD = calculatedTVD;
            ThermalGradientViewModel.MaxWellboreTVD = calculatedTVD;
            ThermalGradientViewModel.HasSurveyData = SurveyPoints.Count > 0;

            if (ForceDrillStringToBottom)
            {
                // NOTE: If user intentionally set CurrentBitDepth, checking ForceToBottom might reset things?
                // But ForceToBottom adjusts LENGTH. CurrentBitDepth tracks POSITION.
                // We should probably respect ForceDrillStringToBottom flag logic.
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
            OnPropertyChanged(nameof(TotalWellboreTVD)); // Notify
            OnPropertyChanged(nameof(ShoeDepth));
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

            OnPropertyChanged(nameof(ActivePits));
            OnPropertyChanged(nameof(ServiceLines));
            OnPropertyChanged(nameof(AnnularVolumePercent));
            OnPropertyChanged(nameof(StringVolumePercent));
            RecalculateSafetyMetrics();

            // ── Publish to Volume Balance ──────────────────────────────────────────────
            TotalStringDisplacement = DrillStringComponents.Sum(c => c.DisplacementVolume);
            TheoreticalWellboreVolume = Math.Max(0, TotalWellboreVolume - TotalStringDisplacement);
            OnPropertyChanged(nameof(TotalStringDisplacement));
            OnPropertyChanged(nameof(TheoreticalWellboreVolume));

            WellContextService.Instance.PublishGeometryData(
                holeCapacity:        TotalWellboreVolume,
                stringDisplacement:  TotalStringDisplacement,
                stringInternalVolume: CalculatedInternalStringVolume,
                annularVolume:       CalculatedAnnularActiveVolume,
                theoreticalWellbore: TheoreticalWellboreVolume);
        }

        /// <summary>
        /// Updates the 'IsHistory' flag for wellbore components.
        /// Logic: If Component A is completely covered by Component B (and B is inside A), A is "Behind Pipe".
        /// </summary>
        private void UpdateBehindPipeStatus()
        {
            if (WellboreComponents == null || WellboreComponents.Count == 0) return;

            // Reset all first
            foreach (var c in WellboreComponents) c.IsHistory = false;

            // Iterate to find covered components
            // We only check Casing/Liner interactions usually, but theoretically Riser too.
            var candidates = WellboreComponents.Where(c => c.Component == ComponentType.Casing || c.Component == ComponentType.Liner).ToList();

            foreach (var outer in candidates)
            {
                // Check if 'outer' is covered by any 'inner'
                // Covered means: Inner Top <= Outer Top AND Inner Bottom >= Outer Bottom
                // And Inner ID < Outer ID (Inside)
                bool isCovered = candidates.Any(inner => 
                    inner != outer && 
                    (inner.TopMD ?? 0) <= (outer.TopMD ?? 0) && 
                    (inner.BottomMD ?? 0) >= (outer.BottomMD ?? 0) && 
                    inner.ID.GetValueOrDefault() < outer.ID.GetValueOrDefault());

                if (isCovered)
                {
                    outer.IsHistory = true;
                }
            }
        }

        private void UpdateAnnularVolumeDetails()
        {
            AnnularVolumeDetails.Clear();
            
            // Use the new WellboreHydraulicsService for improved segment calculation
            var details = _hydraulicsService.CalculateAnnularSegments(
                WellboreComponents, 
                DrillStringComponents);
                
            foreach (var detail in details)
            {
                AnnularVolumeDetails.Add(detail);
            }
            
            // Calculate TotalAnnularVolume from the sum of all detail volumes
            TotalAnnularVolume = AnnularVolumeDetails.Sum(d => d.Volume);
        }
        
        
        private double GetActivePumpRate()
        {
            // Get pump rate in bbl/min
            var activePump = GetActivePump();
            if (activePump == null)
                return 0;
            
            // GPM to bbl/min: 1 bbl = 42 gallons, so bbl/min = GPM / 42
            return activePump.Gpm / 42.0;
        }
        
        private ProjectReport.Models.Rig.ReportPumpOperation? GetActivePump()
        {
            // Get the active pump from the current well's last report
            return _currentWell?.LastReport?.Pumps?.FirstOrDefault(p => p.Gpm > 0);
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
        public static ComponentType StringToSectionType(string value)
        {
            return value switch
            {
                "Casing" => ComponentType.Casing,
                "Liner" => ComponentType.Liner,
                _ => ComponentType.OpenHole
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
            if (e.PropertyName == nameof(WellTest.TestValue) || e.PropertyName == nameof(WellTest.TVD) || e.PropertyName == nameof(WellTest.TestPressurePsi))
            {
                // Safety metrics like MAASP depend on Well Test values
                RecalculateSafetyMetrics();
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
            // Create NEW collection to avoid LiveCharts threading/update crash on Clear()
            var newSeries = new SeriesCollection();

            double maxTVD = TotalWellboreMD > 0 ? TotalWellboreMD : 10000; 

            // 1. Hydrostatic Line (Standard Mud Gradient)
            var hydrostaticValues = new ChartValues<ObservablePoint>
            {
                new ObservablePoint(CurrentMudWeight, 0),
                new ObservablePoint(CurrentMudWeight, -maxTVD)
            };

            newSeries.Add(new LineSeries
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
                newSeries.Add(new ScatterSeries
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

            newSeries.Add(new LineSeries
            {
                Title = "Pore Pressure (Ref)",
                Values = porePressureValues,
                Stroke = Brushes.SlateGray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                PointGeometry = null,
                Fill = Brushes.Transparent
            });

            // Assign atomically
            SafetySeriesCollection = newSeries;
        }

        private void UpdateLotChart()
        {
            // Create NEW collection to avoid LiveCharts threading/update crash on Clear()
            var newSeries = new SeriesCollection();

            // Note: Pump data plotting is currently disabled in the simplified Well Test model.
            // Assign empty collection atomically
            LotSeriesCollection = newSeries;
        }

        private void ExecuteImportPumpData()
        {
            // Note: Pump data import is deprecated in the simplified Well Test model.
            // If LOT data plots are required in the future, the model should be extended accordingly.
            ToastNotificationService.Instance.ShowInfo("LOT Pump Data import is currently disabled in the simplified view.");
        }

        private void AddWellTest(object? parameter)
        {
            int nextId = 1;
            if (WellTests.Any())
            {
                nextId = WellTests.Max(t => t.Id) + 1;
            }

            var newTest = new WellTest
            {
                Id = nextId,
                Type = WellTestType.LeakOff,
                Section = WellboreSectionNames.FirstOrDefault(), // Default to first section
                TestValue = 0
            };

            WellTests.Add(newTest);
            newTest.PropertyChanged += OnWellTestPropertyChanged;
            SelectedWellTest = newTest;
        }

        private void SyncWellTestData(object? parameter)
        {
            if (SelectedWellTest == null) return;

            // Strategy: 
            // 1. If LOT/Integrity, sync with the 'Shoe Depth' (last casing/liner bottom)
            // 2. Otherwise sync with the deepest point available (Survey or Wellbore Bottom)
            
            double targetTvd = 0;
            if (SelectedWellTest.Type == WellTestType.LeakOff || SelectedWellTest.Type == WellTestType.FormationIntegrity)
            {
                targetTvd = ShoeDepth;
            }
            else
            {
                targetTvd = ThermalGradientViewModel.MaxWellboreTVD;
            }

            if (targetTvd > 0)
            {
                SelectedWellTest.TVD = targetTvd;
                // Update MD if we have survey data to be consistent
                var point = SurveyPoints.OrderBy(p => Math.Abs(p.TVD - targetTvd)).FirstOrDefault();
                if (point != null)
                {
                    SelectedWellTest.MD = point.MD;
                }
                else
                {
                    SelectedWellTest.MD = targetTvd; // fallback
                }
                
                ToastNotificationService.Instance.ShowSuccess($"Sync complete: TVD set to {targetTvd:F0} ft.");
            }
            else
            {
                ToastNotificationService.Instance.ShowWarning("No target depth found for synchronization.");
            }
        }

        private void ValidateDrillVsWellbore(DrillStringComponent component)
        {
            if (component == null) return;

            if (component.OD == null || component.OD <= 0) return;

            if (WellboreComponents == null || WellboreComponents.Count == 0) return;

            // Tomar sección activa (última)
            var section = WellboreComponents
    .Where(c => c.OD.HasValue && c.OD.Value > 0) // solo secciones válidas
    .OrderByDescending(c => c.BottomMD ?? 0)
    .FirstOrDefault();

            if (section == null) return;

            double drillOD = component.OD.Value;

            // =========================
            // OPEN HOLE
            // =========================
            if (section.SectionType == ComponentType.OpenHole)
            {
                double holeOD = section.OD ?? 0;

                if (holeOD > 0 && drillOD >= holeOD)
                {
                    component.AddError(nameof(component.OD), $"OD ({drillOD}) must be smaller than Open Hole ({holeOD}).");
                }

                return;
            }

            // =========================
            // CASING / LINER
            // =========================
            if (section.SectionType == ComponentType.Casing ||
                section.SectionType == ComponentType.Liner)
            {
                double casingID = section.ID ?? 0;

                if (casingID > 0 && drillOD >= casingID)
                {
                    component.AddError(
                        nameof(component.OD),
                        $"OD ({drillOD}) must be smaller than ID ({casingID}) of {section.SectionType}.");
                }
            }
        }


        #endregion
    }
}
