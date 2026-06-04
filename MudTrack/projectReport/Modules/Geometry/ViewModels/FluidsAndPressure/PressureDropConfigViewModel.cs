using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.FluidsAndPressure;
using ProjectReport.ViewModels;
using PressureDropConfigModel = ProjectReport.Models.Geometry.PressureDropConfig;

namespace ProjectReport.ViewModels.Geometry.FluidsAndPressure
{
    public class PressureDropConfigViewModel : BaseViewModel
    {
        public PressureDropConfigModel Model { get; }

        public ObservableCollection<PressureDropPoint> Data => Model.Data;

        public double MudDensity
        {
            get => Model.MudDensity;
            set
            {
                if (Model.MudDensity != value)
                {
                    Model.MudDensity = value;
                    OnPropertyChanged();
                    UpdateChart();
                }
            }
        }

        private SeriesCollection _seriesCollection = new SeriesCollection();
        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            set => SetProperty(ref _seriesCollection, value);
        }

        public Func<double, string> XFormatter { get; } = value => $"{value:N0} gpm";
        public Func<double, string> YFormatter { get; } = value => $"{value:N1} psi";

        private string _compatibilityWarning = string.Empty;
        public string CompatibilityWarning
        {
            get => _compatibilityWarning;
            set => SetProperty(ref _compatibilityWarning, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public PressureDropConfigViewModel(PressureDropConfigModel model)
        {
            Model = model;

            SaveCommand = new RelayCommand(_ => RequestClose?.Invoke(true));
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

            Data.CollectionChanged += (s, e) => 
            {
                if (e.NewItems != null)
                {
                    foreach (PressureDropPoint p in e.NewItems)
                        p.PropertyChanged += (s2, e2) => UpdateChart();
                }
                UpdateChart();
            };

            // Subscribe to existing items
            foreach (var p in Data)
            {
                p.PropertyChanged += (s, e) => UpdateChart();
            }

            InitializeChart();
            UpdateChart();
        }

        private void InitializeChart()
        {
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Pressure Drop",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    StrokeThickness = 3
                }
            };
        }

        private void UpdateChart()
        {
            if (SeriesCollection.Count == 0 || SeriesCollection[0].Values == null) return;

            var values = SeriesCollection[0].Values;
            values.Clear();

            var sortedPoints = Data.Where(p => p.FlowRate > 0)
                                   .OrderBy(p => p.FlowRate)
                                   .ToList();

            foreach (var p in sortedPoints)
            {
                values.Add(new ObservablePoint(p.FlowRate, p.PressureDrop));
            }

            // Compatibility Check
            double currentGpm = ProjectReport.Services.WellContextService.Instance.CurrentFlowRate;
            
            if (sortedPoints.Count >= 2)
            {
                double minFlow = sortedPoints.First().FlowRate;
                double maxFlow = sortedPoints.Last().FlowRate;

                if (currentGpm < minFlow)
                {
                    CompatibilityWarning = $"⚠ Current Flow Rate ({currentGpm} gpm) is BELOW the defined range. Calculations may be inaccurate.";
                }
                else if (currentGpm > maxFlow)
                {
                    CompatibilityWarning = $"⚠ Current Flow Rate ({currentGpm} gpm) is ABOVE the defined range. Calculations may be inaccurate.";
                }
                else
                {
                    CompatibilityWarning = string.Empty;
                }
            }
            else if (sortedPoints.Count > 0)
            {
                CompatibilityWarning = "⚠ Define at least two points for accurate interpolation.";
            }
            else
            {
                CompatibilityWarning = string.Empty;
            }
        }
    }
}
