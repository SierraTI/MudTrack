using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Microsoft.Win32;
using ProjectReport.Models.Geometry.ThermalGradient;
using ProjectReport.Models.Geometry;
using ProjectReport.Services;
using System.Windows.Media;

namespace ProjectReport.ViewModels.Geometry.ThermalGradient
{
    public class ThermalGradientViewModel : BaseViewModel
    {
        private readonly ThermalGradientService _thermalService;
        private readonly ThermalGradientImportService _importService;
        private int _nextId = 1;
        private const double SurfaceTempMin = 32.0;
        private const double SurfaceTempMax = 120.0;
        
        // Added IsLoading property to suppress chart updates during bulk loading
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    if (!value)
                    {
                        ValidateAllPoints();
                        RecalculateSummaryStatistics();
                        UpdateChart();
                    }
                }
            }
        }

        // Offshore vs Land well configuration
        private bool _isOffshoreWell = true; // Default to offshore (with Mudline)
        public bool IsOffshoreWell
        {
            get => _isOffshoreWell;
            set
            {
                if (SetProperty(ref _isOffshoreWell, value))
                {
                    OnOffshoreModeChanged();
                }
            }
        }

        private double _ambientTemperature = 75.0; // Surface/ambient temperature (editable)
        public double AmbientTemperature
        {
            get => _ambientTemperature;
            set
            {
                if (SetProperty(ref _ambientTemperature, value))
                {
                    UpdateSurfaceTemperature();
                }
            }
        }

        public ThermalGradientViewModel(ThermalGradientService thermalService)
        {
            _thermalService = thermalService ?? throw new ArgumentNullException(nameof(thermalService));
            _importService = new ThermalGradientImportService();
            
            ThermalGradientPoints = new ObservableCollection<ThermalGradientPoint>();
            ThermalGradientPoints.CollectionChanged += OnThermalPointsCollectionChanged;
            
            Formations.CollectionChanged += OnFormationsCollectionChanged;

            // Subscribe to WellContextService for dynamic depth updates (Rule B)
            WellContextService.Instance.DepthUpdated += OnGlobalDepthUpdated;
            
            // Subscribe to Report thermal data updates for automatic synchronization
            WellContextService.Instance.ReportThermalDataUpdated += OnReportThermalDataUpdated;

            // Ensure default points
            InitializeDefaults();

            // Initialize Chart
            var gradientFill = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1)
            };
            gradientFill.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E0F2FE"), 0.0)); // Light Blue
            gradientFill.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FEE2E2"), 1.0)); // Light Red

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Temperature",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Square, // User requested Square markers
                    PointGeometrySize = 8,
                    PointForeground = (Brush?)new BrushConverter().ConvertFrom("#3B82F6") ?? Brushes.Blue, // Blue markers
                    LineSmoothness = 0.5, // Smooth line
                    Stroke = (Brush?)new BrushConverter().ConvertFrom("#3B82F6") ?? Brushes.Blue, // Blue line
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent, 
                    LabelPoint = point => $"Depth: {Math.Abs(point.Y):N0} ft | Temp: {point.X:N1} °F"
                },
                new LineSeries // Series for Anomalies (Red markers)
                {
                    Title = "Anomalies",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Diamond,
                    PointGeometrySize = 12,
                    PointForeground = Brushes.Red,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Transparent, // No connecting line
                    StrokeThickness = 0,
                    LabelPoint = point => $"⚠ ANOMALY\nDepth: {Math.Abs(point.Y):N0} ft | Temp: {point.X:N1} °F"
                },
                new LineSeries
                {
                    Title = "Reference",
                    Values = new ChartValues<ObservablePoint>(),
                    StrokeDashArray = new DoubleCollection { 2, 2 },
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                },
                new LineSeries
                {
                    Title = "Prediction (TD)",
                    Values = new ChartValues<ObservablePoint>(),
                    Stroke = Brushes.Orange,
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0
                }
            };

            VisualElements = new VisualElementsCollection();
            AxisSections = new SectionsCollection();

            // X-axis = Temperature, Y-axis = TVD (inverted)
            XFormatter = value => $"{value:N1} °F";
            YFormatter = value => $"{Math.Abs(value):N0} ft";

            // Initialize commands
            AddPointCommand = new RelayCommand(_ => AddThermalPoint());
            DeletePointCommand = new RelayCommand(DeleteThermalPoint, CanDeletePoint);
            AutoSortCommand = new RelayCommand(_ => AutoSortPoints());
            ImportDataCommand = new RelayCommand(_ => ImportData());
            ExportDataCommand = new RelayCommand(_ => ExportData());
            ImportFromSurveyCommand = new RelayCommand(_ => ImportFromSurvey(), _ => CanImportFromSurvey);
            SyncWithSurveyCommand = new RelayCommand(_ => SyncWithSurvey(), _ => CanImportFromSurvey);
            AddFormationCommand = new RelayCommand(_ => AddFormation());
            DeleteFormationCommand = new RelayCommand(DeleteFormation);
            AddControlPointCommand = new RelayCommand(_ => AddControlPoint());
            RemoveMudlineCommand = new RelayCommand(_ => RemoveMudline());
            AddMudlineCommand = new RelayCommand(_ => AddMudline());
            
            // Sample formation for demo
            Formations.Add(new Formation("Shale Zone", 1000, 3000, "#F3F4F6"));
        }

        #region Properties

        public ObservableCollection<ThermalGradientPoint> ThermalGradientPoints { get; }

        public SeriesCollection SeriesCollection { get; set; }
        public VisualElementsCollection VisualElements { get; set; }
        private SectionsCollection _axisSections = new();
        public SectionsCollection AxisSections 
        { 
            get => _axisSections; 
            set => SetProperty(ref _axisSections, value); 
        }
        public ObservableCollection<Formation> Formations { get; } = new();

        public Func<double, string> YFormatter { get; set; } = value => value.ToString();
        public Func<double, string> XFormatter { get; set; } = value => value.ToString();
        public List<string> FormationColors { get; } = new()
        {
            "#F3F4FB", // Gray-100 (Default)
            "#E0F2FE", // Blue-100
            "#DCFCE7", // Green-100
            "#FEF9C3", // Yellow-100
            "#FEE2E2", // Red-100
            "#F5F3FF", // Violet-100
            "#FFEDD5"  // Orange-100
        };

        private double _xAxisMinValue = 50;
        public double XAxisMinValue => _xAxisMinValue;

        private double _xAxisMaxValue = 250;
        public double XAxisMaxValue => _xAxisMaxValue;
        // ... (rest of properties)


        private double _surfaceTemperature;
        public double SurfaceTemperature
        {
            get => _surfaceTemperature;
            set => SetProperty(ref _surfaceTemperature, value);
        }

        private double _bottomHoleTemperature;
        public double BottomHoleTemperature
        {
            get => _bottomHoleTemperature;
            set => SetProperty(ref _bottomHoleTemperature, value);
        }

        private double _temperatureRange;
        public double TemperatureRange
        {
            get => _temperatureRange;
            set => SetProperty(ref _temperatureRange, value);
        }

        private double _averageGradient;
        public double AverageGradient
        {
            get => _averageGradient;
            set => SetProperty(ref _averageGradient, value);
        }

        private double _regressionSlope;
        public double RegressionSlope
        {
            get => _regressionSlope;
            set => SetProperty(ref _regressionSlope, value);
        }

        private double _regressionIntercept;
        public double RegressionIntercept
        {
            get => _regressionIntercept;
            set => SetProperty(ref _regressionIntercept, value);
        }

        private int _dataPointsCount;
        public int DataPointsCount
        {
            get => _dataPointsCount;
            set => SetProperty(ref _dataPointsCount, value);
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        private bool _hasValidationError;
        public bool HasValidationError
        {
            get => _hasValidationError;
            set => SetProperty(ref _hasValidationError, value);
        }

        private double _maxWellboreTVD = 0;
        public double MaxWellboreTVD
        {
            get => _maxWellboreTVD;
            set
            {
                if (SetProperty(ref _maxWellboreTVD, value))
                {
                    ValidateAllPoints();
                    RecalculateSummaryStatistics();
                    OnPropertyChanged(nameof(CanImportFromSurvey));
                    // Auto-sync BHT when TVD changes
                    SyncBHTFromReport();
                }
            }
        }

        // Report synchronization properties
        private double? _reportMaxBHT;
        public double? ReportMaxBHT
        {
            get => _reportMaxBHT;
            set
            {
                if (SetProperty(ref _reportMaxBHT, value))
                {
                    SyncBHTFromReport();
                }
            }
        }

        private double? _reportTVD;
        public double? ReportTVD
        {
            get => _reportTVD;
            set
            {
                if (SetProperty(ref _reportTVD, value))
                {
                    if (value.HasValue && value.Value > 0)
                    {
                        MaxWellboreTVD = value.Value;
                    }
                }
            }
        }

        // Indicates if BHT is locked (synced from report)
        private bool _isBHTLocked;
        public bool IsBHTLocked
        {
            get => _isBHTLocked;
            set => SetProperty(ref _isBHTLocked, value);
        }

        private bool _hasSurveyData;
        public bool HasSurveyData
        {
            get => _hasSurveyData;
            set
            {
                if (SetProperty(ref _hasSurveyData, value))
                {
                    OnPropertyChanged(nameof(CanImportFromSurvey));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanImportFromSurvey => HasSurveyData && MaxWellboreTVD > 0;

        // ShowChart is true if we have enough points, even if there are warnings. 
        // We only hide it if there's a critical error (which we should define explicitly if needed)
        public bool ShowChart => ThermalGradientPoints.Count >= 2;

        private ObservableCollection<SegmentGradient> _segmentGradients = new ObservableCollection<SegmentGradient>();
        public ObservableCollection<SegmentGradient> SegmentGradients
        {
            get => _segmentGradients;
            set => SetProperty(ref _segmentGradients, value);
        }

        private string _temperatureZones = string.Empty;
        public string TemperatureZones
        {
            get => _temperatureZones;
            set => SetProperty(ref _temperatureZones, value);
        }

        private int _anomaliesDetectedCount;
        public int AnomaliesDetectedCount
        {
            get => _anomaliesDetectedCount;
            set => SetProperty(ref _anomaliesDetectedCount, value);
        }

        private double _referenceGradient = 1.0;
        public double ReferenceGradient
        {
            get => _referenceGradient;
            set
            {
                if (SetProperty(ref _referenceGradient, value))
                {
                    UpdateChart();
                }
            }
        }

        private bool _showReferenceLine;
        public bool ShowReferenceLine
        {
            get => _showReferenceLine;
            set
            {
                if (SetProperty(ref _showReferenceLine, value))
                {
                    UpdateChart();
                }
            }
        }

        // Segmented Gradients (Water vs Geothermal)
        private double _waterGradient;
        public double WaterGradient
        {
            get => _waterGradient;
            set => SetProperty(ref _waterGradient, value);
        }

        private double _geothermalGradient;
        public double GeothermalGradient
        {
            get => _geothermalGradient;
            set => SetProperty(ref _geothermalGradient, value);
        }

        private bool _showSegmentedGradients = true;
        public bool ShowSegmentedGradients
        {
            get => _showSegmentedGradients;
            set => SetProperty(ref _showSegmentedGradients, value);
        }

        // Rule B: Dynamic Y-axis scaling based on current depth
        private double _currentDepth;
        public double CurrentDepth
        {
            get => _currentDepth;
            set
            {
                if (SetProperty(ref _currentDepth, value))
                {
                    OnPropertyChanged(nameof(YAxisMinValue));
                    OnPropertyChanged(nameof(YAxisMaxValue));
                    ValidateAllPoints();
                    UpdateChart();
                }
            }
        }

        // Rule A: Inverted Y-axis (0 at top, depth increases downward)
        // Calculations and visualization are clipped to CurrentDepth (Report TVD)
        public double YAxisMinValue => -Math.Max(1000, CurrentDepth);
        public double YAxisMaxValue => 0;

        #endregion

        #region Commands

        public ICommand AddPointCommand { get; }
        public ICommand DeletePointCommand { get; }
        public ICommand AutoSortCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ImportFromSurveyCommand { get; }
        public ICommand SyncWithSurveyCommand { get; }
        public ICommand AddFormationCommand { get; }
        public ICommand DeleteFormationCommand { get; }
        public ICommand AddControlPointCommand { get; }
        public ICommand RemoveMudlineCommand { get; }
        public ICommand AddMudlineCommand { get; }

        #endregion

        #region Command Implementations

        private void InitializeDefaults()
        {
            // 1. Ensure Surface Point (TVD 0)
            var surfacePoint = ThermalGradientPoints.FirstOrDefault(p => Math.Abs(p.TVD) < 0.001 || p.Label == "Surface");
            if (surfacePoint == null)
            {
                surfacePoint = new ThermalGradientPoint(_nextId++, 0, AmbientTemperature);
                surfacePoint.Label = "Surface";
                surfacePoint.IsLocked = true;
                surfacePoint.PropertyChanged += OnThermalPointPropertyChanged;
                ThermalGradientPoints.Insert(0, surfacePoint);
            }
            else
            {
                surfacePoint.Label = "Surface";
                surfacePoint.TVD = 0;
                surfacePoint.IsLocked = true;
            }
            
            // 2. Ensure BHT Point (if MaxTVD > 0)
            if (MaxWellboreTVD > 0)
            {
                 var bhtPoint = ThermalGradientPoints.FirstOrDefault(p => p.Label == "BHT");
                 if (bhtPoint == null)
                 {
                     double bhtTemp = 180; 
                     bhtPoint = new ThermalGradientPoint(_nextId++, MaxWellboreTVD, bhtTemp);
                     bhtPoint.Label = "BHT";
                     bhtPoint.PropertyChanged += OnThermalPointPropertyChanged;
                     ThermalGradientPoints.Add(bhtPoint);
                 }
                 else
                 {
                     if (Math.Abs(bhtPoint.TVD - MaxWellboreTVD) > 0.1)
                         bhtPoint.TVD = MaxWellboreTVD; 
                 }
            }
            
            // 3. Ensure Mudline if Offshore
            if (IsOffshoreWell)
            {
                 var mudline = ThermalGradientPoints.FirstOrDefault(p => p.Label == "Mudline");
                 if (mudline == null)
                 {
                     double mudlineDepth = MaxWellboreTVD > 0 ? MaxWellboreTVD * 0.45 : 4500;
                     var newMudline = new ThermalGradientPoint(_nextId++, mudlineDepth, 110);
                     newMudline.Label = "Mudline";
                     newMudline.PropertyChanged += OnThermalPointPropertyChanged;
                     ThermalGradientPoints.Add(newMudline);
                 }
            }
            
            AutoSortPoints();
        }

        private void AddThermalPoint()
        {
            var newPoint = new ThermalGradientPoint(_nextId++, 0, 70);
            newPoint.PropertyChanged += OnThermalPointPropertyChanged;
            ThermalGradientPoints.Add(newPoint);
        }

        private void DeleteThermalPoint(object? parameter)
        {
            if (parameter is ThermalGradientPoint point)
            {
                point.PropertyChanged -= OnThermalPointPropertyChanged;
                ThermalGradientPoints.Remove(point);
            }
        }

        private bool CanDeletePoint(object? parameter)
        {
            return parameter is ThermalGradientPoint;
        }

        private void AutoSortPoints()
        {
            var sortedPoints = _thermalService.SortByTVD(ThermalGradientPoints.ToList());
            
            ThermalGradientPoints.Clear();
            foreach (var point in sortedPoints)
            {
                ThermalGradientPoints.Add(point);
            }

            ToastNotificationService.Instance.ShowSuccess("Thermal gradient points sorted by TVD");
        }

        private void ImportData()
        {
            try
            {
                var importedPoints = _importService.ShowImportDialog();
                
                if (importedPoints != null && importedPoints.Count > 0)
                {
                    // Validate imported data
                    var validationErrors = _importService.ValidateImportedData(importedPoints);
                    
                    if (validationErrors.Count > 0)
                    {
                        var message = "Imported data has warnings:\n" + string.Join("\n", validationErrors.Take(5));
                        ToastNotificationService.Instance.ShowWarning(message);
                    }

                    // Clear existing points and add imported ones
                    ThermalGradientPoints.Clear();
                    
                    foreach (var point in importedPoints)
                    {
                        point.PropertyChanged += OnThermalPointPropertyChanged;
                        ThermalGradientPoints.Add(point);
                    }

                    _nextId = importedPoints.Max(p => p.Id) + 1;
                    
                    ToastNotificationService.Instance.ShowSuccess($"Imported {importedPoints.Count} thermal points");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error importing data: {ex.Message}");
            }
        }

        private void ExportData()
        {
            try
            {
                if (ThermalGradientPoints.Count == 0)
                {
                    ToastNotificationService.Instance.ShowWarning("No data to export");
                    return;
                }

                _importService.ShowExportDialog(ThermalGradientPoints.ToList());
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting data: {ex.Message}");
            }
        }

        private void ImportFromSurvey()
        {
            if (!CanImportFromSurvey)
            {
                ToastNotificationService.Instance.ShowWarning("Advertencia: Imposible importar TVD. Complete el módulo Survey primero.");
                return;
            }

            // Suggest a BHT temperature based on existing data (interpolation) if possible
            double suggestedTemp = 0.0;
            if (ThermalGradientPoints.Count >= 2)
            {
                suggestedTemp = _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), MaxWellboreTVD);
            }

            var newPoint = new ThermalGradientPoint(_nextId++, MaxWellboreTVD, suggestedTemp);
            newPoint.Label = "BHT"; // Auto-label imported point
            newPoint.PropertyChanged += OnThermalPointPropertyChanged;
            ThermalGradientPoints.Add(newPoint);

            ToastNotificationService.Instance.ShowInfo($"TVD máxima del survey importada ({MaxWellboreTVD:F2} ft). Temperatura sugerida: {suggestedTemp:F1}°F");
        }

        /// <summary>
        /// Syncs BHT point from Report data (MaxBHT and Report TVD)
        /// This is called automatically when Report data changes
        /// </summary>
        private void SyncBHTFromReport()
        {
            // Only sync if we have TVD from report
            if (!ReportTVD.HasValue || ReportTVD.Value <= 0)
                return;

            var targetTVD = ReportTVD.Value;
            var targetBHT = ReportMaxBHT ?? 0;

            // Find or create BHT point
            var existingBht = ThermalGradientPoints.FirstOrDefault(p => p.Label == "BHT");
            
            if (existingBht != null)
            {
                // Only update if the point is locked (synced from report) or if values have changed
                bool shouldUpdate = existingBht.IsLocked || 
                                    Math.Abs(existingBht.TVD - targetTVD) > 0.01 ||
                                    (targetBHT > 0 && Math.Abs(existingBht.Temperature - targetBHT) > 0.1);

                if (shouldUpdate)
                {
                    // Temporarily disable property change notifications to avoid recursive updates
                    existingBht.PropertyChanged -= OnThermalPointPropertyChanged;
                    
                    existingBht.TVD = targetTVD;
                    if (targetBHT > 0)
                    {
                        existingBht.Temperature = targetBHT;
                        existingBht.IsLocked = true; // Lock if we have BHT from report
                    }
                    else if (existingBht.IsLocked)
                    {
                        // Keep locked if it was previously locked, but allow temperature to be updated
                        existingBht.IsLocked = true;
                    }
                    
                    existingBht.PropertyChanged += OnThermalPointPropertyChanged;
                    IsBHTLocked = targetBHT > 0 || existingBht.IsLocked;
                }
            }
            else if (targetTVD > 0)
            {
                // Create new BHT point
                double suggestedTemp = targetBHT > 0 ? targetBHT : 180.0;
                
                // If no BHT from report, try to interpolate from existing points
                if (targetBHT <= 0 && ThermalGradientPoints.Count >= 2)
                {
                    suggestedTemp = _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), targetTVD);
                }

                var newPoint = new ThermalGradientPoint(_nextId++, targetTVD, suggestedTemp);
                newPoint.Label = "BHT";
                newPoint.IsLocked = targetBHT > 0; // Lock if synced from report
                newPoint.PropertyChanged += OnThermalPointPropertyChanged;
                ThermalGradientPoints.Add(newPoint);
                IsBHTLocked = targetBHT > 0;
                
                AutoSortPoints();
            }

            // Update MaxWellboreTVD to match Report TVD (only if different)
            if (targetTVD > 0 && (MaxWellboreTVD == 0 || Math.Abs(MaxWellboreTVD - targetTVD) > 0.01))
            {
                MaxWellboreTVD = targetTVD;
            }
        }

        /// <summary>
        /// Public method to sync with Report data (called from external sources)
        /// </summary>
        public void SyncWithReport(double? reportTVD, double? reportMaxBHT)
        {
            ReportTVD = reportTVD;
            ReportMaxBHT = reportMaxBHT;
            SyncBHTFromReport();
            
            if (reportTVD.HasValue && reportMaxBHT.HasValue)
            {
                ToastNotificationService.Instance.ShowSuccess($"✓ Sincronizado con Daily Report: TVD {reportTVD.Value:F0} ft, BHT {reportMaxBHT.Value:F1}°F.");
            }
        }

        private void SyncWithSurvey()
        {
            if (!CanImportFromSurvey)
            {
                ToastNotificationService.Instance.ShowWarning("Advertencia: Imposible sincronizar. Complete el módulo Survey primero.");
                return;
            }

            // Remove any existing point that might be the BHT from a previous sync to avoid duplicates
            // But only if it's not locked from report
            var existingBht = ThermalGradientPoints.FirstOrDefault(p => p.Label == "BHT");
            if (existingBht != null && !IsBHTLocked)
            {
                ThermalGradientPoints.Remove(existingBht);
            }

            // Update Surface Temperature to match Ambient Temperature
            var surfacePoint = ThermalGradientPoints.FirstOrDefault(p => Math.Abs(p.TVD) < 0.01 || p.Label == "Surface");
            if (surfacePoint != null)
            {
                surfacePoint.Temperature = AmbientTemperature;
            }

            // Only update BHT if not locked from report
            if (!IsBHTLocked)
            {
                // Suggest a BHT temperature based on existing data (interpolation) if possible
                double suggestedTemp = 180.0;
                if (ThermalGradientPoints.Count >= 2)
                {
                    suggestedTemp = _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), MaxWellboreTVD);
                }

                // Create or update BHT point with TVD from MaxWellboreTVD
                if (existingBht == null)
                {
                    var newPoint = new ThermalGradientPoint(_nextId++, MaxWellboreTVD, suggestedTemp);
                    newPoint.Label = "BHT"; 
                    newPoint.PropertyChanged += OnThermalPointPropertyChanged;
                    ThermalGradientPoints.Add(newPoint);
                }
                else
                {
                    existingBht.TVD = MaxWellboreTVD;
                    existingBht.Temperature = suggestedTemp;
                }

                // Auto-sort to ensure correct order
                AutoSortPoints();
            }

            ToastNotificationService.Instance.ShowSuccess($"✓ Sincronizado con Survey: TVD máxima {MaxWellboreTVD:F0} ft, Temperatura Ambiente {AmbientTemperature:F1}°F.");
        }

        private void AddFormation()
        {
            double top = Formations.Any() ? Formations.Max(f => f.BottomTVD) : 0;
            Formations.Add(new Formation("New Formation", top, top + 1000, "#F3F4F6"));
            UpdateChart();
        }

        private void DeleteFormation(object? parameter)
        {
            if (parameter is Formation formation)
            {
                Formations.Remove(formation);
                UpdateChart();
            }
        }

        #endregion

        #region Event Handlers

        private void OnThermalPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isLoading) return;

            if (e.NewItems != null)
            {
                foreach (ThermalGradientPoint point in e.NewItems)
                {
                    point.PropertyChanged += OnThermalPointPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (ThermalGradientPoint point in e.OldItems)
                {
                    point.PropertyChanged -= OnThermalPointPropertyChanged;
                }
            }

            // Validar antes de recalcular para que ShowChart refleje el estado correcto
            ValidateAllPoints();
            RecalculateSummaryStatistics();
            UpdateChartScaling();
            UpdateChart();
        }

        private void OnFormationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isLoading) return;

            if (e.NewItems != null)
            {
                foreach (Formation f in e.NewItems)
                {
                    f.PropertyChanged += (s, ev) => 
                    {
                        if (!_isLoading) UpdateChart();
                    };
                }
            }
            if (e.OldItems != null)
            {
                foreach (Formation f in e.OldItems)
                {
                    // Unsubscribe optional but good practice
                }
            }
            UpdateChart();
        }

        private void OnThermalPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThermalGradientPoint.TVD) || 
                e.PropertyName == nameof(ThermalGradientPoint.Temperature))
            {
                // Validar primero
                ValidateAllPoints();
                RecalculateSummaryStatistics();
                UpdateChart();
            }
        }

        #endregion

        #region Validation

        private void ValidateAllPoints()
        {
            if (ThermalGradientPoints.Count == 0)
            {
                ValidationMessage = string.Empty;
                HasValidationError = false;
                return;
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            // BR-TG-001: TVD Ordering (Error)
            var orderingErrors = _thermalService.ValidateTVDOrdering(ThermalGradientPoints.ToList());
            errors.AddRange(orderingErrors);

            // BR-TG-002: TVD Range Validation (Warning)
            double depthLimit = CurrentDepth > 0 ? CurrentDepth : (MaxWellboreTVD > 0 ? MaxWellboreTVD : double.MaxValue);
            
            var rangeWarnings = _thermalService.ValidateTVDRange(ThermalGradientPoints.ToList(), depthLimit);
            warnings.AddRange(rangeWarnings);

            // BR-TG-003: Temperature Gradient Logic
            var gradientWarnings = _thermalService.ValidateTemperatureGradient(ThermalGradientPoints.ToList());
            warnings.AddRange(gradientWarnings);
            
            // Surface temperature reasonableness
            var surfacePoint = ThermalGradientPoints.OrderBy(p => p.TVD).FirstOrDefault();
            var surfaceWarning = surfacePoint != null ? _thermalService.ValidateSurfaceTemperature(surfacePoint) : null;
            if (!string.IsNullOrEmpty(surfaceWarning))
            {
                warnings.Add(surfaceWarning);
            }

            // BR-TG-004: Minimum Data Points
            if (ThermalGradientPoints.Count < 2)
            {
                warnings.Add("Add at least 2 thermal points to generate temperature profile");
            }

            // Clear per-point warnings first
            foreach (var p in ThermalGradientPoints)
            {
                p.HasValidationWarning = false;
                p.ValidationMessage = string.Empty;
            }

            // Mark rows with warnings (search for ID match in messages)
            var allMessages = errors.Concat(warnings).ToList();
            foreach (var msg in allMessages)
            {
                try
                {
                    var marker = "ID ";
                    var idx = msg.IndexOf(marker);
                    if (idx >= 0)
                    {
                        var start = idx + marker.Length;
                        var end = msg.IndexOf(':', start);
                        if (end > start)
                        {
                            var idStr = msg.Substring(start, end - start).Trim();
                            if (int.TryParse(idStr, out int alertId))
                            {
                                var point = ThermalGradientPoints.FirstOrDefault(pt => pt.Id == alertId);
                                if (point != null)
                                {
                                    point.HasValidationWarning = true;
                                    point.ValidationMessage = msg;
                                }
                            }
                        }
                    }
                }
                catch { /* non-fatal parsing */ }
            }

            // Construct Final Message
            var finalMessage = string.Empty;
            
            if (errors.Any())
            {
                finalMessage = string.Join("\n", errors);
                HasValidationError = true;
            }
            else
            {
                HasValidationError = false;
                if (warnings.Any())
                {
                    finalMessage = string.Join("\n", warnings);
                }
            }
            
            ValidationMessage = finalMessage;

            // Notify chart visibility update
            OnPropertyChanged(nameof(ShowChart));
        }

        #endregion

        #region Summary Statistics

        private void RecalculateSummaryStatistics()
        {
            DataPointsCount = ThermalGradientPoints.Count;

            if (ThermalGradientPoints.Count == 0)
            {
                SurfaceTemperature = 0;
                BottomHoleTemperature = 0;
                TemperatureRange = 0;
                AverageGradient = 0;
                RegressionSlope = 0;
                RegressionIntercept = 0;
                WaterGradient = 0;
                GeothermalGradient = 0;
                OnPropertyChanged(nameof(ShowChart));
                return;
            }

            var sortedPoints = ThermalGradientPoints.OrderBy(p => p.TVD).ToList();

            SurfaceTemperature = sortedPoints.First().Temperature;
            
                if (sortedPoints.Count >= 2)
                {
                    // Regression for trend display
                    var (slope, intercept) = _thermalService.ComputeLinearRegression(sortedPoints);
                    RegressionSlope = slope;
                    RegressionIntercept = intercept;

                    // Use interpolation for BHT (temperature at target TVD)
                    double targetTvd = MaxWellboreTVD > 0 ? MaxWellboreTVD : sortedPoints.Last().TVD;
                    BottomHoleTemperature = _thermalService.InterpolateTemperature(sortedPoints, targetTvd);

                    // Temperature range and average gradient per spec
                    TemperatureRange = BottomHoleTemperature - SurfaceTemperature;
                    AverageGradient = _thermalService.CalculateAverageGradient(sortedPoints); // °F per 100 ft (per spec)

                // Calculate segment gradients
                var segments = _thermalService.CalculateSegmentGradients(sortedPoints);
                SegmentGradients.Clear();
                foreach (var segment in segments)
                {
                    SegmentGradients.Add(segment);
                }
                
                // Calculate segmented gradients (Water vs Geothermal for offshore wells)
                CalculateSegmentedGradients();
                
                // Calculate temperature zones
                CalculateTemperatureZones(sortedPoints);
            }
            else
            {
                BottomHoleTemperature = sortedPoints.Last().Temperature;
                TemperatureRange = BottomHoleTemperature - SurfaceTemperature;
                AverageGradient = 0;
                RegressionSlope = 0;
                RegressionIntercept = 0;
                WaterGradient = 0;
                GeothermalGradient = 0;
                SegmentGradients.Clear();
                TemperatureZones = string.Empty;
            }

            // Notificar cambio de ShowChart (depende de DataPointsCount y HasValidationError)
            UpdateChartScaling();
            OnPropertyChanged(nameof(ShowChart));
        }

        /// <summary>
        /// Updates point labels automatically based on position.
        /// </summary>
        private void UpdatePointLabels()
        {
            if (ThermalGradientPoints.Count == 0) return;

            var sortedPoints = ThermalGradientPoints.OrderBy(p => p.TVD).ToList();

            // Auto-label surface point (TVD = 0 or first point)
            var surfacePoint = sortedPoints.FirstOrDefault(p => Math.Abs(p.TVD) < 0.01);
            if (surfacePoint != null && string.IsNullOrEmpty(surfacePoint.Label))
            {
                surfacePoint.Label = "Surface";
            }

            // Auto-label BHT (deepest point) if not already labeled
            var bhtPoint = sortedPoints.LastOrDefault();
            if (bhtPoint != null && string.IsNullOrEmpty(bhtPoint.Label) && bhtPoint != surfacePoint)
            {
                bhtPoint.Label = "BHT";
            }
        }

        private void CalculateTemperatureZones(List<ThermalGradientPoint> sortedPoints)
        {
            var zones = new List<string>();
            
            // Find temperature ranges
            var minTemp = sortedPoints.Min(p => p.Temperature);
            var maxTemp = sortedPoints.Max(p => p.Temperature);
            
            if (minTemp < 150)
                zones.Add($"Cool (< 150°F): {sortedPoints.Where(p => p.Temperature < 150).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature < 150).Max(p => p.TVD):F0} ft");
            
            if (sortedPoints.Any(p => p.Temperature >= 150 && p.Temperature < 250))
                zones.Add($"Moderate (150-250°F): {sortedPoints.Where(p => p.Temperature >= 150 && p.Temperature < 250).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature >= 150 && p.Temperature < 250).Max(p => p.TVD):F0} ft");
            
            if (sortedPoints.Any(p => p.Temperature >= 250 && p.Temperature < 350))
                zones.Add($"Hot (250-350°F): {sortedPoints.Where(p => p.Temperature >= 250 && p.Temperature < 350).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature >= 250 && p.Temperature < 350).Max(p => p.TVD):F0} ft");
            
            if (maxTemp >= 350)
                zones.Add($"Very Hot (> 350°F): {sortedPoints.Where(p => p.Temperature >= 350).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature >= 350).Max(p => p.TVD):F0} ft");
            
            TemperatureZones = zones.Count > 0 ? string.Join(" | ", zones) : "No zones defined";

            // Calculate per-point gradients
            _thermalService.CalculatePointGradients(sortedPoints);

            // Detect and flag anomalies
            var anomalousIds = _thermalService.DetectGradientAnomalies(sortedPoints);
            AnomaliesDetectedCount = anomalousIds.Count;

            // Clear all anomaly flags first
            foreach (var point in ThermalGradientPoints)
            {
                point.IsAnomalous = false;
            }

            // Set anomaly flags
            foreach (var id in anomalousIds)
            {
                var point = ThermalGradientPoints.FirstOrDefault(p => p.Id == id);
                if (point != null)
                {
                    point.IsAnomalous = true;
                }
            }

            // Update point labels
            UpdatePointLabels();
        }

        private void UpdateChartScaling()
        {
            if (ThermalGradientPoints.Count == 0)
            {
                _xAxisMinValue = 50;
                _xAxisMaxValue = 250;
            }
            else
            {
                var validPoints = ThermalGradientPoints.Where(p => !double.IsNaN(p.Temperature) && !double.IsInfinity(p.Temperature)).ToList();
                if (validPoints.Count == 0)
                {
                    _xAxisMinValue = 50;
                    _xAxisMaxValue = 250;
                }
                else
                {
                    var minTemp = validPoints.Min(p => p.Temperature);
                    var maxTemp = validPoints.Max(p => p.Temperature);

                    // Ensure at least 20 degree range for the X axis
                    if (maxTemp - minTemp < 20)
                    {
                        double mid = (maxTemp + minTemp) / 2;
                        _xAxisMinValue = Math.Max(0, mid - 10);
                        _xAxisMaxValue = mid + 10;
                    }
                    else
                    {
                        // Add 5% padding
                        double padding = (maxTemp - minTemp) * 0.05;
                        _xAxisMinValue = Math.Max(0, minTemp - padding);
                        _xAxisMaxValue = maxTemp + padding;
                    }
                }
            }

            OnPropertyChanged(nameof(XAxisMinValue));
            OnPropertyChanged(nameof(XAxisMaxValue));
            OnPropertyChanged(nameof(YAxisMinValue));
        }

        private void UpdateChart()
        {
            // Ensure SeriesCollection is initialized
            if (SeriesCollection == null || SeriesCollection.Count == 0)
            {
                // Should have been initialized in constructor, but safe ref
                return; 
            }

            if (SeriesCollection != null && SeriesCollection.Count > 0)
            {
                var values = new ChartValues<ObservablePoint>();
                var anomalyValues = new ChartValues<ObservablePoint>();
                
                // Create NEW collections to avoid LiveCharts threading/update crash on Clear()
                var newVisualElements = new VisualElementsCollection();
                var newSections = new SectionsCollection();
                
                // Add formations shading to AxisSections
                foreach (var formation in Formations)
                {
                    double topY = -formation.TopTVD;
                    double bottomY = -formation.BottomTVD;
                    double midY = (topY + bottomY) / 2;

                    // Add shading section
                    newSections.Add(new AxisSection
                    {
                        Value = -formation.BottomTVD,
                        SectionWidth = Math.Abs(formation.BottomTVD - formation.TopTVD),
                        Fill = (Brush?)new BrushConverter().ConvertFrom(formation.Color) ?? Brushes.LightGray,
                        Opacity = 0.4,
                        DataLabel = false
                    });

                    // Add text label as VisualElement
                    newVisualElements.Add(new VisualElement
                    {
                        X = 40, 
                        Y = midY,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        UIElement = new System.Windows.Controls.TextBlock
                        {
                            Text = formation.Name,
                            FontSize = 10,
                            Foreground = (Brush?)new BrushConverter().ConvertFrom("#4B5563") ?? Brushes.DimGray,
                            FontWeight = System.Windows.FontWeights.SemiBold,
                            Opacity = 0.8
                        }
                    });
                }

                var sortedPoints = ThermalGradientPoints.OrderBy(p => p.TVD).ToList();
                
                // Populate Temperature Series and Anomalies Series
                for (int i = 0; i < sortedPoints.Count; i++)
                {
                    var point = sortedPoints[i];
                    // X = Temperature, Y = TVD (negative for inversion)
                    values.Add(new ObservablePoint(point.Temperature, -point.TVD));

                    // Add to Anomalies series if marked
                    if (point.IsAnomalous || point.HasValidationWarning)
                    {
                        anomalyValues.Add(new ObservablePoint(point.Temperature, -point.TVD));
                    }
                    else
                    {
                         // Maintain index alignment or just skip? ScatterSeries doesn't need alignment
                    }

                    // Add label if present
                    if (!string.IsNullOrEmpty(point.Label))
                    {
                        newVisualElements.Add(new VisualElement
                        {
                            X = point.Temperature,
                            Y = -point.TVD,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                            VerticalAlignment = System.Windows.VerticalAlignment.Center,
                            UIElement = new System.Windows.Controls.TextBlock
                            {
                                Text = point.Label,
                                FontWeight = System.Windows.FontWeights.Bold,
                                Foreground = (Brush?)new BrushConverter().ConvertFrom("#6366F1") ?? Brushes.Indigo, 
                                Padding = new System.Windows.Thickness(6, 0, 0, 0),
                                Background = Brushes.Transparent,
                                IsHitTestVisible = false 
                            }
                        });
                    }
                }

                // [0] Temperature Series
                if (SeriesCollection.Count > 0 && SeriesCollection[0] != null)
                {
                    if (SeriesCollection[0].Values == null) SeriesCollection[0].Values = new ChartValues<ObservablePoint>();
                    
                    if (SeriesCollection[0].Values is ChartValues<ObservablePoint> existingValues)
                    {
                        existingValues.Clear();
                        foreach (var v in values) existingValues.Add(v);
                    }
                    else
                    {
                        SeriesCollection[0].Values = values;
                    }
                }

                // [1] Anomalies Series
                if (SeriesCollection.Count > 1 && SeriesCollection[1] != null)
                {
                    if (SeriesCollection[1].Values == null) SeriesCollection[1].Values = new ChartValues<ObservablePoint>();

                    if (SeriesCollection[1].Values is ChartValues<ObservablePoint> existingAnomalies)
                    {
                        existingAnomalies.Clear();
                         foreach (var v in anomalyValues) existingAnomalies.Add(v);
                    }
                    else
                    {
                        SeriesCollection[1].Values = anomalyValues;
                    }
                }

                // [2] Reference Gradient Line
                if (SeriesCollection.Count > 2 && SeriesCollection[2] != null)
                {
                    var refValues = new ChartValues<ObservablePoint>();
                    if (ShowReferenceLine && ThermalGradientPoints.Count > 0)
                    {
                        double startTemp = SurfaceTemperature;
                        double maxTVD = MaxWellboreTVD > 0 ? MaxWellboreTVD : (ThermalGradientPoints.Any() ? ThermalGradientPoints.Max(p => p.TVD) : 10000);
                        double slope = ReferenceGradient / 100.0;
                        double endTemp = startTemp + (slope * maxTVD);
                        
                        refValues.Add(new ObservablePoint(startTemp, 0));
                        refValues.Add(new ObservablePoint(endTemp, -maxTVD));
                    }
                    
                    if (SeriesCollection[2].Values == null) SeriesCollection[2].Values = new ChartValues<ObservablePoint>();

                    if (SeriesCollection[2].Values is ChartValues<ObservablePoint> existingRef)
                    {
                        existingRef.Clear();
                        foreach (var v in refValues) existingRef.Add(v);
                    }
                    else
                    {
                        SeriesCollection[2].Values = refValues;
                    }
                }

                // [3] Prediction Line (dotted to TD)
                if (SeriesCollection.Count > 3 && SeriesCollection[3] != null)
                {
                    var predictionValues = new ChartValues<ObservablePoint>();
                    if (sortedPoints.Count >= 2 && MaxWellboreTVD > sortedPoints.Last().TVD)
                    {
                        var lastPoint = sortedPoints.Last();
                        double predictedTempTD = _thermalService.PredictTemperatureAtTD(sortedPoints, MaxWellboreTVD);
                        
                        predictionValues.Add(new ObservablePoint(lastPoint.Temperature, -lastPoint.TVD));
                        predictionValues.Add(new ObservablePoint(predictedTempTD, -MaxWellboreTVD));
                    }
                    
                    if (SeriesCollection[3].Values == null) SeriesCollection[3].Values = new ChartValues<ObservablePoint>();

                    if (SeriesCollection[3].Values is ChartValues<ObservablePoint> existingPred)
                    {
                        existingPred.Clear();
                        foreach (var v in predictionValues) existingPred.Add(v);
                    }
                    else
                    {
                        SeriesCollection[3].Values = predictionValues;
                    }
                }

                // Total Depth Line Section
                if (MaxWellboreTVD > 0)
                {
                     newSections.Add(new AxisSection
                     {
                         Value = -MaxWellboreTVD,
                         Stroke = (Brush?)new BrushConverter().ConvertFrom("#EF4444") ?? Brushes.Red, // Red-500
                         StrokeThickness = 2,
                         StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                         DataLabel = false
                     });

                    newVisualElements.Add(new VisualElement
                    {
                        X = 40, 
                        Y = -MaxWellboreTVD,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                        UIElement = new System.Windows.Controls.TextBlock
                        {
                            Text = "Total Depth",
                            FontSize = 10,
                            Foreground = Brushes.Red,
                            FontWeight = System.Windows.FontWeights.Bold,
                            Background = Brushes.White,
                            Padding = new System.Windows.Thickness(2)
                        }
                    });
                }

                // Assign the completely built collections atomically
                VisualElements = newVisualElements;
                AxisSections = newSections;
            }
        }

        #endregion

        #region Offshore/Land Well Configuration

        /// <summary>
        /// Handles transition when switching between offshore and land well modes
        /// </summary>
        private void OnOffshoreModeChanged()
        {
            if (IsOffshoreWell)
            {
                // Transitioning to offshore: ensure Mudline exists
                if (!ThermalGradientPoints.Any(p => p.Label == "Mudline"))
                {
                    //Insert Mudline at 4500ft default or 45% of depth
                    double mudlineDepth = MaxWellboreTVD > 0 ? MaxWellboreTVD * 0.45 : 4500;
                    double mudlineTemp = 110.0;
                    
                    if (ThermalGradientPoints.Count >= 2)
                    {
                        mudlineTemp = _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), mudlineDepth);
                    }

                    var mudlinePoint = new ThermalGradientPoint(_nextId++, mudlineDepth, mudlineTemp) { Label = "Mudline" };
                    mudlinePoint.PropertyChanged += OnThermalPointPropertyChanged;
                    ThermalGradientPoints.Add(mudlinePoint);
                    AutoSortPoints();
                }
                CalculateSegmentedGradients();
            }
            else
            {
                // Transitioning to land: remove Mudline if present
                var mudlinePoint = ThermalGradientPoints.FirstOrDefault(p => p.Label == "Mudline");
                if (mudlinePoint != null)
                {
                    mudlinePoint.PropertyChanged -= OnThermalPointPropertyChanged;
                    ThermalGradientPoints.Remove(mudlinePoint);
                }
                WaterGradient = 0;
            }

            RecalculateSummaryStatistics();
            UpdateChart();
            OnPropertyChanged(nameof(IsOffshoreWell));
        }

        /// <summary>
        /// Updates Surface temperature from Ambient Temperature
        /// </summary>
        private void UpdateSurfaceTemperature()
        {
            var surfacePoint = ThermalGradientPoints.FirstOrDefault(p => Math.Abs(p.TVD) < 0.01 || p.Label == "Surface");
            if (surfacePoint != null)
            {
                surfacePoint.Temperature = AmbientTemperature;
            }
            SurfaceTemperature = AmbientTemperature;
        }

        /// <summary>
        /// Calculates separate gradients for water column (Surface to Mudline) and geothermal (Mudline to BHT)
        /// For land wells, only calculates geothermal gradient (Surface to BHT)
        /// </summary>
        private void CalculateSegmentedGradients()
        {
            if (ThermalGradientPoints.Count < 2)
            {
                WaterGradient = 0;
                GeothermalGradient = 0;
                return;
            }

            var sortedPoints = ThermalGradientPoints.OrderBy(p => p.TVD).ToList();
            var surfacePoint = sortedPoints.FirstOrDefault(p => Math.Abs(p.TVD) < 0.01 || p.Label == "Surface");
            var mudlinePoint = sortedPoints.FirstOrDefault(p => p.Label == "Mudline");
            var bhtPoint = sortedPoints.LastOrDefault(p => p.Label == "BHT") ?? sortedPoints.LastOrDefault();

            if (IsOffshoreWell && mudlinePoint != null && surfacePoint != null)
            {
                // Offshore: Calculate Water Gradient (Surface to Mudline) and Geothermal Gradient (Mudline to BHT)
                WaterGradient = _thermalService.CalculateGradient(
                    surfacePoint.TVD, surfacePoint.Temperature,
                    mudlinePoint.TVD, mudlinePoint.Temperature
                );

                // Geothermal Gradient: Mudline to BHT
                if (bhtPoint != null && bhtPoint != mudlinePoint)
                {
                    GeothermalGradient = _thermalService.CalculateGradient(
                        mudlinePoint.TVD, mudlinePoint.Temperature,
                        bhtPoint.TVD, bhtPoint.Temperature
                    );
                }
                else
                {
                    GeothermalGradient = AverageGradient;
                }
            }
            else if (sortedPoints.Count >= 2)
            {
                // Land well: Only Geothermal Gradient (Surface to BHT), no Water Gradient
                WaterGradient = 0;
                if (surfacePoint != null && bhtPoint != null)
                {
                    GeothermalGradient = _thermalService.CalculateGradient(
                        surfacePoint.TVD, surfacePoint.Temperature,
                        bhtPoint.TVD, bhtPoint.Temperature
                    );
                }
                else
                {
                    GeothermalGradient = AverageGradient;
                }
            }
        }

        #endregion

        #region Control Point Management

        private void AddControlPoint()
        {
            // Create dialog to input TVD for new control point
            double tvd = (ThermalGradientPoints.Any() ? ThermalGradientPoints.Max(p => p.TVD) / 2 : 5000);
            double suggestedTemp = 0;

            if (ThermalGradientPoints.Count >= 2)
            {
                suggestedTemp = _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), tvd);
            }

            var newPoint = new ThermalGradientPoint(_nextId++, tvd, suggestedTemp);
            newPoint.Label = "Control Point";
            newPoint.PropertyChanged += OnThermalPointPropertyChanged;
            ThermalGradientPoints.Add(newPoint);

            AutoSortPoints();
            ToastNotificationService.Instance.ShowSuccess("Control point added at " + tvd.ToString("F0") + " ft");
        }

        private void RemoveMudline()
        {
            if (!IsOffshoreWell) return;

            var mudlinePoint = ThermalGradientPoints.FirstOrDefault(p => p.Label == "Mudline");
            if (mudlinePoint != null)
            {
                mudlinePoint.PropertyChanged -= OnThermalPointPropertyChanged;
                ThermalGradientPoints.Remove(mudlinePoint);
                ToastNotificationService.Instance.ShowSuccess("Mudline point removed");
            }
        }

        private void AddMudline()
        {
            if (!IsOffshoreWell) return;

            if (ThermalGradientPoints.Any(p => p.Label == "Mudline"))
            {
                ToastNotificationService.Instance.ShowWarning("Mudline already exists");
                return;
            }

            // Find optimal position between Surface and BHT
            var sortedPoints = ThermalGradientPoints.OrderBy(p => p.TVD).ToList();
            double tvd = 4500; // Default mudline depth

            if (sortedPoints.Count > 1)
            {
                var lastPoint = sortedPoints.Last();
                tvd = lastPoint.TVD * 0.45; // Position at 45% of deepest point
            }

            double suggestedTemp = 0;
            if (ThermalGradientPoints.Count >= 2)
            {
                suggestedTemp = _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), tvd);
            }

            var mudlinePoint = new ThermalGradientPoint(_nextId++, tvd, suggestedTemp) { Label = "Mudline" };
            mudlinePoint.PropertyChanged += OnThermalPointPropertyChanged;
            ThermalGradientPoints.Add(mudlinePoint);

            AutoSortPoints();
            ToastNotificationService.Instance.ShowSuccess("Mudline point added at " + tvd.ToString("F0") + " ft");
        }

        #endregion

        #region WellContextService Integration

        /// <summary>
        /// Rule B: Updates Y-axis scaling when current depth changes from Daily Reports
        /// </summary>
        private void OnGlobalDepthUpdated(object? sender, double newDepth)
        {
            CurrentDepth = newDepth;
            
            // Auto-Sync BHT Point logic
            // Find BHT point
            var bhtPoint = ThermalGradientPoints.FirstOrDefault(p => p.Label == "BHT");
            
            if (newDepth > 0)
            {
                MaxWellboreTVD = newDepth;
                
                if (bhtPoint != null)
                {
                    // Update BHT depth automatically 
                    bhtPoint.TVD = newDepth;
                }
                else
                {
                    // If missing, add it
                    InitializeDefaults();
                }
            }
        }

        /// <summary>
        /// Handles automatic synchronization when report thermal data (MaxBHT and TVD) changes
        /// </summary>
        private void OnReportThermalDataUpdated(object? sender, ReportThermalDataEventArgs e)
        {
            // Only sync if we have valid data and it's different from current values
            if (e.ReportTVD.HasValue && e.ReportTVD.Value > 0)
            {
                // Avoid unnecessary updates if values haven't changed
                bool tvdChanged = !ReportTVD.HasValue || Math.Abs(ReportTVD.Value - e.ReportTVD.Value) > 0.01;
                bool bhtChanged = !ReportMaxBHT.HasValue || 
                                  !e.ReportMaxBHT.HasValue || 
                                  Math.Abs(ReportMaxBHT.Value - e.ReportMaxBHT.Value) > 0.1;

                if (tvdChanged || bhtChanged)
                {
                    SyncWithReport(e.ReportTVD, e.ReportMaxBHT);
                }
            }
        }

        #endregion

        #region Public Methods

        public int GetNextId()
        {
            return _nextId++;
        }

        public double GetTemperatureAtTVD(double tvd)
        {
            if (ThermalGradientPoints.Count < 2)
                return 0;

            return _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), tvd);
        }

        #endregion
    }
}
