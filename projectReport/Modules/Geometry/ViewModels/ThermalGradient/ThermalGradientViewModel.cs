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

        public ThermalGradientViewModel(ThermalGradientService thermalService)
        {
            _thermalService = thermalService ?? throw new ArgumentNullException(nameof(thermalService));
            _importService = new ThermalGradientImportService();
            
            ThermalGradientPoints = new ObservableCollection<ThermalGradientPoint>();
            ThermalGradientPoints.CollectionChanged += OnThermalPointsCollectionChanged;
            
            Formations.CollectionChanged += OnFormationsCollectionChanged;

            // Ensure a default Surface point (ID=1, TVD=0)
            if (ThermalGradientPoints.Count == 0)
            {
                var surface = new ThermalGradientPoint(_nextId++, 0, 70.0);
                surface.PropertyChanged += OnThermalPointPropertyChanged;
                ThermalGradientPoints.Add(surface);
            }

            // Initialize Chart
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Temperature",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    LineSmoothness = 0,
                    Stroke = (Brush)new BrushConverter().ConvertFrom("#F97316"), // Brand Orange
                    Fill = Brushes.Transparent,
                    LabelPoint = point => $"Depth: {Math.Abs(point.Y):N0} ft | Temp: {point.X:N1} °F"
                },
                new LineSeries
                {
                    Title = "Regression",
                    Values = new ChartValues<ObservablePoint>(),
                    StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    Stroke = Brushes.Gray
                },
                new LineSeries
                {
                    Title = "Reference",
                    Values = new ChartValues<ObservablePoint>(),
                    StrokeDashArray = new System.Windows.Media.DoubleCollection { 2, 2 },
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    Stroke = (Brush)new BrushConverter().ConvertFrom("#3B82F6"), // Blue-500
                    StrokeThickness = 2
                },
                new ScatterSeries
                {
                    Title = "Anomalies",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Diamond,
                    MaxPointShapeDiameter = 15,
                    MinPointShapeDiameter = 15,
                    Fill = (Brush)new BrushConverter().ConvertFrom("#DC2626"), // Red-600
                    Stroke = (Brush)new BrushConverter().ConvertFrom("#DC2626"),
                    LabelPoint = point => $"⚠ Check Data at {Math.Abs(point.Y):N0} ft"
                },
                new LineSeries
                {
                    Title = "Diagnostic Alert",
                    Values = new ChartValues<ObservablePoint>(),
                    Stroke = (Brush)new BrushConverter().ConvertFrom("#DC2626"), // Bright Red
                    StrokeThickness = 3,
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0
                },
                new LineSeries
                {
                    Title = "Prediction (TD)",
                    Values = new ChartValues<ObservablePoint>(),
                    Stroke = (Brush)new BrushConverter().ConvertFrom("#6B7280"), // Gray-500
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 2, 2 },
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
            AddFormationCommand = new RelayCommand(_ => AddFormation());
            DeleteFormationCommand = new RelayCommand(DeleteFormation);
            
            // Sample formation for demo
            Formations.Add(new Formation("Shale Zone", 1000, 3000, "#F3F4F6"));
        }

        #region Properties

        public ObservableCollection<ThermalGradientPoint> ThermalGradientPoints { get; }

        public SeriesCollection SeriesCollection { get; set; } = new();
        public VisualElementsCollection VisualElements { get; set; } = new();
        public SectionsCollection AxisSections { get; set; } = new();
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
                }
            }
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

        // Nueva propiedad calculada requerida por el código existente
        public bool ShowChart => ThermalGradientPoints.Count >= 2 && !HasValidationError;

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

        #endregion

        #region Commands

        public ICommand AddPointCommand { get; }
        public ICommand DeletePointCommand { get; }
        public ICommand AutoSortCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ImportFromSurveyCommand { get; }
        public ICommand AddFormationCommand { get; }
        public ICommand DeleteFormationCommand { get; }

        #endregion

        #region Command Implementations

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
            newPoint.Label = "BHT (Survey)"; // Auto-label imported point
            newPoint.PropertyChanged += OnThermalPointPropertyChanged;
            ThermalGradientPoints.Add(newPoint);

            ToastNotificationService.Instance.ShowInfo($"TVD máxima del survey importada ({MaxWellboreTVD:F2} ft). Temperatura sugerida: {suggestedTemp:F1}°F");
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
            UpdateChart();
        }

        private void OnFormationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Formation f in e.NewItems)
                {
                    f.PropertyChanged += (s, ev) => UpdateChart();
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

            // BR-TG-001: TVD Ordering
            var orderingErrors = _thermalService.ValidateTVDOrdering(ThermalGradientPoints.ToList());
            errors.AddRange(orderingErrors);

            // BR-TG-002: TVD Range Validation
            if (MaxWellboreTVD > 0)
            {
                var rangeErrors = _thermalService.ValidateTVDRange(ThermalGradientPoints.ToList(), MaxWellboreTVD);
                errors.AddRange(rangeErrors);
            }

            // BR-TG-003: Temperature Gradient Logic
            var gradientWarnings = _thermalService.ValidateTemperatureGradient(ThermalGradientPoints.ToList());
            errors.AddRange(gradientWarnings);

            // Clear per-point warnings
            foreach (var p in ThermalGradientPoints)
            {
                p.HasValidationWarning = false;
                p.ValidationMessage = string.Empty;
            }

            // Mark rows with warnings when gradient validation returns an ID (format includes "ID {id}:")
            foreach (var warn in gradientWarnings)
            {
                try
                {
                    var marker = "ID ";
                    var idx = warn.IndexOf(marker);
                    if (idx >= 0)
                    {
                        var start = idx + marker.Length;
                        var end = warn.IndexOf(':', start);
                        if (end > start)
                        {
                            var idStr = warn.Substring(start, end - start).Trim();
                            if (int.TryParse(idStr, out int warnId))
                            {
                                var point = ThermalGradientPoints.FirstOrDefault(pt => pt.Id == warnId);
                                if (point != null)
                                {
                                    point.HasValidationWarning = true;
                                    point.ValidationMessage = warn;
                                }
                            }
                        }
                    }
                }
                catch { /* non-fatal parsing */ }
            }

            // Surface temperature reasonableness (T4 surface check)
            var surfacePoint = ThermalGradientPoints.OrderBy(p => p.TVD).FirstOrDefault();
            var surfaceWarning = surfacePoint != null ? _thermalService.ValidateSurfaceTemperature(surfacePoint) : null;
            if (!string.IsNullOrEmpty(surfaceWarning))
            {
                errors.Add(surfaceWarning);
            }

            // BR-TG-004: Minimum Data Points
            if (ThermalGradientPoints.Count < 2)
            {
                errors.Add("Add at least 2 thermal points to generate temperature profile");
            }

            if (errors.Any())
            {
                ValidationMessage = string.Join("\n", errors);
                HasValidationError = true;
            }
            else
            {
                ValidationMessage = string.Empty;
                HasValidationError = false;
            }

            // Notificar que ShowChart puede haber cambiado
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
                SegmentGradients.Clear();
                TemperatureZones = string.Empty;
            }

            // Notificar cambio de ShowChart (depende de DataPointsCount y HasValidationError)
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

        private void UpdateChart()
        {
            if (SeriesCollection != null && SeriesCollection.Count > 0)
            {
                var values = new ChartValues<ObservablePoint>();
                
                // Clear existing visual elements (labels) and sections
                if (VisualElements != null) VisualElements.Clear();
                if (AxisSections != null) AxisSections.Clear();
                
                // Add formations shading to AxisSections
                foreach (var formation in Formations)
                {
                    AxisSections.Add(new AxisSection
                    {
                        Label = formation.Name,
                        Value = -formation.BottomTVD,
                        SectionWidth = Math.Abs(formation.BottomTVD - formation.TopTVD),
                        Fill = (Brush)new BrushConverter().ConvertFrom(formation.Color),
                        Opacity = 0.4,
                        DataLabel = true,
                        DataLabelForeground = (Brush)new BrushConverter().ConvertFrom("#4B5563")
                    });
                }

                var sortedPoints = ThermalGradientPoints.OrderBy(p => p.TVD).ToList();
                var alertValues = new ChartValues<ObservablePoint>();

                for (int i = 0; i < sortedPoints.Count; i++)
                {
                    var point = sortedPoints[i];
                    // X = Temperature, Y = TVD (negative for inversion)
                    values.Add(new ObservablePoint(point.Temperature, -point.TVD));

                    // Diagnostic Highlighting: If gradient to next point > 2.0, add segment to alert series
                    if (i < sortedPoints.Count - 1)
                    {
                        var nextPoint = sortedPoints[i + 1];
                        double grad = _thermalService.CalculateGradient(point.TVD, point.Temperature, nextPoint.TVD, nextPoint.Temperature);
                        if (grad > 2.0)
                        {
                            alertValues.Add(new ObservablePoint(point.Temperature, -point.TVD));
                            alertValues.Add(new ObservablePoint(nextPoint.Temperature, -nextPoint.TVD));
                            alertValues.Add(new ObservablePoint(double.NaN, double.NaN)); // Disconnect segment
                        }
                    }
// ... (labels logic)
                    // Add label if present
                    if (!string.IsNullOrEmpty(point.Label) && VisualElements != null)
                    {
                        VisualElements.Add(new VisualElement
                        {
                            X = point.Temperature,
                            Y = -point.TVD,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                            VerticalAlignment = System.Windows.VerticalAlignment.Center,
                            UIElement = new System.Windows.Controls.TextBlock
                            {
                                Text = point.Label,
                                FontWeight = System.Windows.FontWeights.Bold,
                                Foreground = (Brush)new BrushConverter().ConvertFrom("#6366F1"), // Indigo
                                Padding = new System.Windows.Thickness(6, 0, 0, 0),
                                Background = Brushes.Transparent,
                                IsHitTestVisible = false // prevent interference
                            }
                        });
                    }
                }

                SeriesCollection[0].Values = values;

                // Regression line
                if (SeriesCollection.Count > 1)
                {
                    var regValues = new ChartValues<ObservablePoint>();
                    if (ThermalGradientPoints.Count >= 2)
                    {
                        double startTemp = RegressionIntercept; // at TVD = 0
                        double maxTVD = MaxWellboreTVD > 0 ? MaxWellboreTVD : (ThermalGradientPoints.Any() ? ThermalGradientPoints.Max(p => p.TVD) : 10000);
                        
                        double endTemp = RegressionSlope * maxTVD + RegressionIntercept;
                        double endTvd = maxTVD;

                        regValues.Add(new ObservablePoint(startTemp, 0));
                        regValues.Add(new ObservablePoint(endTemp, -endTvd));
                    }
                    SeriesCollection[1].Values = regValues;
                }

                // Reference Gradient Line
                if (SeriesCollection.Count > 2)
                {
                    var refValues = new ChartValues<ObservablePoint>();
                    if (ShowReferenceLine && ThermalGradientPoints.Count > 0)
                    {
                        double startTemp = SurfaceTemperature;
                        double maxTVD = MaxWellboreTVD > 0 ? MaxWellboreTVD : (ThermalGradientPoints.Any() ? ThermalGradientPoints.Max(p => p.TVD) : 10000);
                        
                        // Reference Gradient is typically in °F/100ft
                        // Slope = (RefGrad / 100)
                        double slope = ReferenceGradient / 100.0;
                        double endTemp = startTemp + (slope * maxTVD);
                        
                        refValues.Add(new ObservablePoint(startTemp, 0));
                        refValues.Add(new ObservablePoint(endTemp, -maxTVD));
                    }
                    SeriesCollection[2].Values = refValues;
                }

                // Anomalies Scatter
                if (SeriesCollection.Count > 3)
                {
                    var anomalyValues = new ChartValues<ObservablePoint>();
                    foreach (var point in sortedPoints)
                    {
                        if (point.IsAnomalous || point.HasValidationWarning)
                        {
                            anomalyValues.Add(new ObservablePoint(point.Temperature, -point.TVD));
                        }
                    }
                    SeriesCollection[3].Values = anomalyValues;
                }

                // Diagnostic Alert Series (Red Segments)
                if (SeriesCollection.Count > 4)
                {
                    SeriesCollection[4].Values = alertValues;
                }

                // Prediction Line (dotted to TD)
                if (SeriesCollection.Count > 5)
                {
                    var predictionValues = new ChartValues<ObservablePoint>();
                    if (sortedPoints.Count >= 2 && MaxWellboreTVD > sortedPoints.Last().TVD)
                    {
                        var lastPoint = sortedPoints.Last();
                        double predictedTempTD = _thermalService.PredictTemperatureAtTD(sortedPoints, MaxWellboreTVD);
                        
                        predictionValues.Add(new ObservablePoint(lastPoint.Temperature, -lastPoint.TVD));
                        predictionValues.Add(new ObservablePoint(predictedTempTD, -MaxWellboreTVD));
                    }
                    SeriesCollection[5].Values = predictionValues;
                }

                // Total Depth Line Section
                if (MaxWellboreTVD > 0 && AxisSections != null)
                {
                     AxisSections.Add(new AxisSection
                     {
                         Value = -MaxWellboreTVD,
                         Stroke = (Brush)new BrushConverter().ConvertFrom("#EF4444"), // Red-500
                         StrokeThickness = 2,
                         StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                         DataLabel = true,
                         DataLabelForeground = Brushes.Red,
                         Label = "Total Depth"
                     });
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
