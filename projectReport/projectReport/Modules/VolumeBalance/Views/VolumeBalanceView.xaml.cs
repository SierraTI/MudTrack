using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectReport.Modules.VolumeBalance.ViewModels;

namespace ProjectReport.Modules.VolumeBalance.Views
{
    public partial class VolumeBalanceView : UserControl
    {
        public VolumeBalanceView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private VolumeBalanceViewModel? _vm;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _vm = DataContext as VolumeBalanceViewModel;
        }

        // ── Trend Chart ───────────────────────────────────────────────

        private void TrendCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawTrendChart();
        }

        private void DrawTrendChart()
        {
            TrendCanvas.Children.Clear();

            var vm = DataContext as VolumeBalanceViewModel;
            var points = vm?.TrendPoints;
            if (points == null || points.Count == 0)
            {
                // "No data" label
                var noData = new TextBlock
                {
                    Text = "No events logged yet. Add events to see the trend.",
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(noData, TrendCanvas.ActualWidth / 2 - 160);
                Canvas.SetTop(noData, TrendCanvas.ActualHeight / 2 - 10);
                TrendCanvas.Children.Add(noData);
                return;
            }

            double w = TrendCanvas.ActualWidth;
            double h = TrendCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            double minVol = points.Min(p => p.Volume);
            double maxVol = points.Max(p => p.Volume);
            double volRange = maxVol - minVol;
            if (volRange < 10) { minVol = Math.Max(0, minVol - 10); maxVol += 10; volRange = maxVol - minVol; }

            var times = points.Select(p => p.Time).ToList();
            double minT = times.Min().Ticks;
            double maxT = times.Max().Ticks;
            double tRange = maxT - minT;
            if (tRange == 0) tRange = 1;

            double padding = 8;

            // Grid lines (5 horizontal)
            for (int i = 0; i <= 4; i++)
            {
                double y = padding + (h - 2 * padding) * i / 4;
                var line = new Line
                {
                    X1 = 0, Y1 = y, X2 = w, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    StrokeThickness = 1
                };
                TrendCanvas.Children.Add(line);

                double vol = maxVol - (volRange * i / 4);
                var label = new TextBlock
                {
                    Text = $"{vol:F0}",
                    FontSize = 10,
                    Foreground = Brushes.Gray
                };
                Canvas.SetLeft(label, -40);
                Canvas.SetTop(label, y - 8);
                TrendCanvas.Children.Add(label);
            }

            // Draw polyline
            var poly = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            var screenPts = points.Select(p =>
            {
                double x = tRange > 0
                    ? padding + (p.Time.Ticks - minT) / tRange * (w - 2 * padding)
                    : w / 2;
                double y = padding + (1 - (p.Volume - minVol) / volRange) * (h - 2 * padding);
                return new System.Windows.Point(x, y);
            }).ToList();

            foreach (var pt in screenPts)
                poly.Points.Add(pt);

            TrendCanvas.Children.Add(poly);

            // Dots with tooltip
            for (int i = 0; i < points.Count; i++)
            {
                var pt = screenPts[i];
                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Cursor = Cursors.Hand,
                    ToolTip = $"{points[i].Time:MM/dd HH:mm}\n{points[i].Volume:F1} bbl"
                };
                Canvas.SetLeft(dot, pt.X - 5);
                Canvas.SetTop(dot, pt.Y - 5);
                TrendCanvas.Children.Add(dot);
            }
        }

        /// <summary>Called from ViewModel after events change to refresh the chart.</summary>
        public void RefreshTrendChart() => DrawTrendChart();
    }
}
