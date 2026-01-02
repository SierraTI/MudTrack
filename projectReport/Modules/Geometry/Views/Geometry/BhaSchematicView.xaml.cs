using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectReport.ViewModels.Geometry;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Views.Geometry
{
    public partial class BhaSchematicView : UserControl
    {
        private const double BaseWidth = 140;
        private const double ODScale = 10.0; // Scale for OD visualization
        private const double MinHeight = 10;
        private const double MaxHeightPerComponent = 150;

        public BhaSchematicView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => DrawSchematic();
        }

        public void DrawSchematic()
        {
            if (DataContext is not GeometryViewModel vm || SchematicCanvas == null) return;

            SchematicCanvas.Children.Clear();
            double currentY = 10;
            double canvasWidth = SchematicCanvas.ActualWidth > 0 ? SchematicCanvas.ActualWidth : BaseWidth;
            double centerX = canvasWidth / 2;

            var components = vm.DrillStringComponents.ToList();
            if (!components.Any()) return;

            // Calculate height scale
            double totalLength = components.Sum(c => c.Length ?? 0);
            double availableHeight = 1000; // Estimated scrollable height
            double scale = totalLength > 0 ? Math.Min(5.0, availableHeight / totalLength) : 1.0;

            foreach (var comp in components)
            {
                double h = (comp.Length ?? 0) * scale;
                if (h < MinHeight) h = MinHeight;
                if (h > MaxHeightPerComponent) h = MaxHeightPerComponent;

                double w = (comp.OD ?? 5.0) * ODScale;
                
                // Drawing Rect
                var rect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    Fill = GetColorForComponent(comp.ComponentType),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    ToolTip = $"{comp.ComponentType}: {comp.Length} ft x {comp.OD}\" OD"
                };

                Canvas.SetLeft(rect, centerX - (w / 2));
                Canvas.SetTop(rect, currentY);
                SchematicCanvas.Children.Add(rect);

                // Add text label if height permits
                if (h > 20)
                {
                    var label = new TextBlock
                    {
                        Text = comp.Name ?? comp.ComponentType.ToString(),
                        FontSize = 9,
                        Width = w,
                        TextAlignment = TextAlignment.Center,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(label, centerX - (w / 2));
                    Canvas.SetTop(label, currentY + (h / 2) - 6);
                    SchematicCanvas.Children.Add(label);
                }

                currentY += h;
            }

            // OnBottom Indicator
            if (vm.BitToBottom != null && Math.Abs(vm.BitToBottom.Value) < 0.1)
            {
                var marker = new Path
                {
                    Data = System.Windows.Media.Geometry.Parse("M 0,0 L 10,10 L -10,10 Z"),
                    Fill = Brushes.Green,
                    ToolTip = "Bit is ON BOTTOM"
                };
                Canvas.SetLeft(marker, centerX);
                Canvas.SetTop(marker, currentY);
                SchematicCanvas.Children.Add(marker);
            }

            SchematicCanvas.Height = currentY + 50;
        }

        private Brush GetColorForComponent(ComponentType type)
        {
            return type switch
            {
                ComponentType.Bit => Brushes.Crimson,
                ComponentType.DC => Brushes.DarkSlateGray,
                ComponentType.HWDP => Brushes.DodgerBlue,
                ComponentType.Motor => Brushes.Purple,
                ComponentType.MWD => Brushes.Indigo,
                _ => Brushes.SteelBlue
            };
        }
    }
}
