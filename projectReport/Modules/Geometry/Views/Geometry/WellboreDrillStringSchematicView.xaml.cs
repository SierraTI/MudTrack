using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectReport.ViewModels.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Views.Geometry
{
    public partial class WellboreDrillStringSchematicView : UserControl
    {
        private const double BaseWidth = 260;
        private const double DepthScale = 0.05; // 1 ft = 0.05 px (adjusted dynamically)
        private const double ODScale = 12.0;   // Scale for OD visualization
        private const double MinSegmentHeight = 5;

        public WellboreDrillStringSchematicView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => DrawSchematic();
            SizeChanged += (s, e) => DrawSchematic();
        }

        public void DrawSchematic()
        {
            if (DataContext is not GeometryViewModel vm || SchematicCanvas == null) return;

            SchematicCanvas.Children.Clear();
            double canvasWidth = SchematicCanvas.ActualWidth > 0 ? SchematicCanvas.ActualWidth : BaseWidth;
            double centerX = canvasWidth / 2;

            var wellbore = vm.WellboreComponents.OrderBy(w => w.TopMD).ToList();
            var drillString = vm.DrillStringComponents.ToList();

            if (!wellbore.Any()) return;

            // Calculate Scales
            double maxMD = wellbore.Max(w => w.BottomMD ?? 0);
            if (maxMD <= 0) maxMD = 1000;
            
            double availableHeight = Math.Max(600, ActualHeight - 40);
            double verticalScale = availableHeight / maxMD;

            // 1. Draw Wellbore (Background)
            foreach (var section in wellbore)
            {
                double top = (section.TopMD ?? 0) * verticalScale;
                double bottom = (section.BottomMD ?? 0) * verticalScale;
                double h = Math.Max(MinSegmentHeight, bottom - top);
                double w = (section.OD ?? 12.0) * ODScale;

                // Wall Fill
                var rect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    Fill = section.SectionType == WellboreSectionType.OpenHole 
                        ? (Brush?)new BrushConverter().ConvertFromString("#FEF3C7") ?? Brushes.Wheat
                        : (Brush?)new BrushConverter().ConvertFromString("#F3F4F6") ?? Brushes.LightGray,
                    Stroke = section.SectionType == WellboreSectionType.OpenHole 
                        ? (Brush?)new BrushConverter().ConvertFromString("#F59E0B") ?? Brushes.Orange
                        : (Brush?)new BrushConverter().ConvertFromString("#9CA3AF") ?? Brushes.Gray,
                    StrokeThickness = 1,
                    ToolTip = $"{section.Name}\nDepth: {section.TopMD}-{section.BottomMD} ft\nID: {section.ID}\" OD: {section.OD}\""
                };

                Canvas.SetLeft(rect, centerX - (w / 2));
                Canvas.SetTop(rect, top + 10);
                SchematicCanvas.Children.Add(rect);

                // Shoe Depth Label
                if (section.SectionType != WellboreSectionType.OpenHole)
                {
                    AddDepthLabel(section.BottomMD ?? 0, top + h + 10, centerX + (w / 2) + 5, true);
                }
            }

            // 2. Draw Drill String (Foreground)
            // Bit depth from context or last component depth
            double currentY = 10;
            // Iterate from Top to Bottom (Last to First in our model implementation)
            for (int i = drillString.Count - 1; i >= 0; i--)
            {
                var comp = drillString[i];
                double h = (comp.Length ?? 0) * verticalScale;
                double w = (comp.OD ?? 5.0) * ODScale;

                var rect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    Fill = GetColorForComponent(comp.ComponentType),
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.5,
                    ToolTip = $"{comp.Name}\nLength: {comp.Length} ft\nOD: {comp.OD}\""
                };

                Canvas.SetLeft(rect, centerX - (w / 2));
                Canvas.SetTop(rect, currentY);
                SchematicCanvas.Children.Add(rect);

                currentY += h;
            }

            // TD Label
            AddDepthLabel(maxMD, maxMD * verticalScale + 10, centerX - 50, false);

            SchematicCanvas.Height = (maxMD * verticalScale) + 50;
        }

        private void AddDepthLabel(double depth, double y, double x, bool isShoe)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var arrow = new Line
            {
                X1 = 0, Y1 = 0, X2 = isShoe ? -20 : 20, Y2 = 0,
                Stroke = isShoe ? Brushes.DimGray : Brushes.Red,
                StrokeThickness = 1,
                VerticalAlignment = VerticalAlignment.Center
            };

            var label = new TextBlock
            {
                Text = $"{(isShoe ? "Shoe" : "TD")}: {depth:F0} ft",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = isShoe ? Brushes.DimGray : Brushes.Red,
                Margin = new Thickness(5, 0, 0, 0)
            };

            panel.Children.Add(arrow);
            panel.Children.Add(label);

            Canvas.SetLeft(panel, isShoe ? x : x - 30);
            Canvas.SetTop(panel, y - 7);
            SchematicCanvas.Children.Add(panel);
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
