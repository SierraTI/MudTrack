using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Views.Geometry
{
    public partial class WellboreSchematicView : UserControl
    {
        private const double CanvasWidth = 240;
        private const double BaseScale = 0.05; // 1 ft = 0.05 px (can be adjusted)
        private const double DiameterScale = 8; // 1 inch = 8 px

        public WellboreSchematicView()
        {
            InitializeComponent();
            this.DataContextChanged += WellboreSchematicView_DataContextChanged;
        }

        private INotifyCollectionChanged? _currentWbCollection;
        private INotifyCollectionChanged? _currentDsCollection;

        private void WellboreSchematicView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                UnsubscribeFromCollections();
            }
            if (e.NewValue is INotifyPropertyChanged newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                SubscribeToCollections(newVm);
            }
            DrawSchematic();
        }

        private void SubscribeToCollections(object vm)
        {
            UnsubscribeFromCollections();

            var type = vm.GetType();
            _currentWbCollection = type.GetProperty("WellboreComponents")?.GetValue(vm) as INotifyCollectionChanged;
            _currentDsCollection = type.GetProperty("DrillStringComponents")?.GetValue(vm) as INotifyCollectionChanged;

            if (_currentWbCollection != null)
                _currentWbCollection.CollectionChanged += OnCollectionChanged;
            
            if (_currentDsCollection != null)
                _currentDsCollection.CollectionChanged += OnCollectionChanged;
        }

        private void UnsubscribeFromCollections()
        {
            if (_currentWbCollection != null)
                _currentWbCollection.CollectionChanged -= OnCollectionChanged;
            
            if (_currentDsCollection != null)
                _currentDsCollection.CollectionChanged -= OnCollectionChanged;

            _currentWbCollection = null;
            _currentDsCollection = null;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => DrawSchematic();

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TotalWellboreMD" || e.PropertyName == "SelectedTabIndex" || e.PropertyName == "TotalDrillStringLength")
            {
                DrawSchematic();
            }
        }

        public void DrawSchematic()
        {
            SchematicCanvas.Children.Clear();

            // Get data from DataContext
            var vm = DataContext;
            if (vm == null) return;

            var type = vm.GetType();
            var wbComponents = (type.GetProperty("WellboreComponents")?.GetValue(vm) as IEnumerable<WellboreComponent>)?.OrderBy(c => c.TopMD ?? 0).ToList();
            var dsComponents = type.GetProperty("DrillStringComponents")?.GetValue(vm) as IEnumerable<DrillStringComponent>;

            if (wbComponents == null || !wbComponents.Any()) return;

            double maxDepth = wbComponents.Max(c => c.BottomMD ?? 0);
            double totalDsLength = dsComponents?.Sum(c => c.Length.GetValueOrDefault()) ?? 0;
            maxDepth = Math.Max(maxDepth, totalDsLength);
            if (maxDepth < 100) maxDepth = 100;

            // Update Canvas Height based on scale
            SchematicCanvas.Height = maxDepth * BaseScale + 100;

            // 0. Draw Fluid Background (Annulus area)
            double maxWbWidth = wbComponents.Max(c => c.OD ?? 0) * DiameterScale;
            var fluidRect = new Rectangle
            {
                Width = maxWbWidth,
                Height = maxDepth * BaseScale,
                Fill = (Brush)new BrushConverter().ConvertFrom("#EFF6FF"), // Very light blue
                Opacity = 0.5
            };
            Canvas.SetLeft(fluidRect, (CanvasWidth - maxWbWidth) / 2);
            Canvas.SetTop(fluidRect, 0);
            SchematicCanvas.Children.Add(fluidRect);

            // 1. Draw Wellbore Sections (Casing, Liner, OpenHole)
            foreach (var section in wbComponents)
            {
                if (!section.TopMD.HasValue || !section.BottomMD.HasValue || section.OD <= 0) continue;

                double top = section.TopMD.Value * BaseScale;
                double height = (section.BottomMD.Value - section.TopMD.Value) * BaseScale;
                double width = (section.OD ?? 0) * DiameterScale;
                double left = (CanvasWidth - width) / 2;

                Brush fill;
                Brush stroke;

                switch (section.SectionType)
                {
                    case WellboreSectionType.Casing:
                        fill = (Brush)new BrushConverter().ConvertFrom("#9CA3AF"); // Gray-400
                        stroke = (Brush)new BrushConverter().ConvertFrom("#374151"); // Gray-700
                        break;
                    case WellboreSectionType.Liner:
                        fill = (Brush)new BrushConverter().ConvertFrom("#60A5FA"); // Blue-400
                        stroke = (Brush)new BrushConverter().ConvertFrom("#1D4ED8"); // Blue-700
                        break;
                    default: // OpenHole
                        fill = (Brush)new BrushConverter().ConvertFrom("#FCD34D"); // Amber-300
                        stroke = (Brush)new BrushConverter().ConvertFrom("#B45309"); // Amber-700
                        break;
                }

                var rect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = fill,
                    Stroke = stroke,
                    StrokeThickness = 2,
                    ToolTip = $"{section.Name}\n{section.OD}\" x {section.ID}\"\n{section.TopMD}-{section.BottomMD} ft"
                };

                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                SchematicCanvas.Children.Add(rect);

                // Highlight Shoe depth or Liner Top
                if (section.SectionType == WellboreSectionType.Liner)
                {
                    AddMarkerLabel($"LT: {section.TopMD} ft", section.TopMD.Value, Brushes.Blue, true);
                }
                if (section.SectionType != WellboreSectionType.OpenHole)
                {
                    AddMarkerLabel($"Shoe: {section.BottomMD} ft", section.BottomMD.Value, Brushes.DarkSlateGray, false);
                }
            }

            // 2. Draw Drill String
            if (dsComponents != null)
            {
                double currentTop = 0;
                foreach (var comp in dsComponents)
                {
                    if ((comp.Length ?? 0) <= 0 || (comp.OD ?? 0) <= 0) continue;

                    double height = (comp.Length ?? 0) * BaseScale;
                    double width = (comp.OD ?? 0) * DiameterScale;
                    double left = (CanvasWidth - width) / 2;

                    var rect = new Rectangle
                    {
                        Width = width,
                        Height = height,
                        Fill = comp.ComponentType == ComponentType.Bit ? (Brush)new BrushConverter().ConvertFrom("#EF4444") : (Brush)new BrushConverter().ConvertFrom("#F97316"),
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.5,
                        ToolTip = $"{comp.Name}\nOD: {comp.OD}\"\nLen: {comp.Length} ft"
                    };

                    Canvas.SetLeft(rect, left);
                    Canvas.SetTop(rect, currentTop * BaseScale);
                    SchematicCanvas.Children.Add(rect);

                    if (comp.ComponentType == ComponentType.Bit)
                    {
                        AddMarkerLabel($"Bit: {currentTop + comp.Length} ft", currentTop + comp.Length.Value, Brushes.Red, true);
                    }

                    currentTop += comp.Length.Value;
                }
            }
            
            // 3. Draw Depth Grid every 1000 ft
            for (int d = 0; d <= maxDepth; d += 1000)
            {
                var label = new TextBlock
                {
                    Text = $"{d}'",
                    FontSize = 9,
                    Foreground = Brushes.Gray,
                    Opacity = 0.6
                };
                Canvas.SetLeft(label, 2);
                Canvas.SetTop(label, d * BaseScale + 2);
                SchematicCanvas.Children.Add(label);
                
                var line = new Line
                {
                    X1 = 0, X2 = CanvasWidth,
                    Y1 = d * BaseScale, Y2 = d * BaseScale,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    Opacity = 0.5
                };
                SchematicCanvas.Children.Add(line);
            }
        }

        private void AddMarkerLabel(string text, double depth, Brush color, bool alignLeft)
        {
            var label = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = color,
                Background = new SolidColorBrush(Colors.White) { Opacity = 0.7 }
            };
            
            double top = depth * BaseScale;
            Canvas.SetTop(label, top - 6);
            
            if (alignLeft)
                Canvas.SetLeft(label, 5);
            else
                Canvas.SetRight(label, 5); // Note: SetRight only works if we use a framework element that supports it or just calculate left
            
            // Actually Canvas.SetRight might not work as expected if CanvasWidth is fixed but element width is dynamic
            // Let's use left calculation
            if (!alignLeft)
            {
                // We'll just put it at the right side
                // To do this accurately we should probably use a different container or measure the text
                // For now, let's just use a fixed offset from right
                label.TextAlignment = TextAlignment.Right;
                Canvas.SetLeft(label, CanvasWidth - 100);
                label.Width = 95;
            }

            SchematicCanvas.Children.Add(label);
        }
    }
}
