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
    public partial class BhaSchematicView : UserControl
    {
        private const double BaseWidth = 140;
        private const double ODScale = 10.0; // Scale for OD visualization
        private const double MinComponentHeight = 10;
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
            var wellbore = vm.WellboreComponents.OrderBy(w => w.TopMD).ToList();

            if (!components.Any()) return;

            // Calculate height scale based on wellbore and drill string
            double totalWellboreMD = wellbore.Any() ? wellbore.Max(w => w.BottomMD ?? 0) : 0;
            double totalDrillStringLength = components.Sum(c => c.Length ?? 0);
            double totalMD = Math.Max(totalWellboreMD, totalDrillStringLength);
            
            // Limit minimum total MD for scaling to avoid "huge blocks" for very short strings
            double scalingMD = Math.Max(500, totalMD);
            double availableHeight = SchematicCanvas.ActualHeight > 0 ? SchematicCanvas.ActualHeight : 1000;
            double verticalScale = scalingMD > 0 ? Math.Min(5.0, availableHeight / scalingMD) : 1.0;

            // 0. Draw Formation Background (Ground)
            var formationBackground = new Rectangle
            {
                Width = canvasWidth,
                Height = totalMD * verticalScale + 50,
                Fill = new SolidColorBrush(Color.FromRgb(240, 235, 230)), // Pale earth color
                Opacity = 0.5
            };
            Canvas.SetLeft(formationBackground, 0);
            Canvas.SetTop(formationBackground, currentY);
            SchematicCanvas.Children.Add(formationBackground);

            // Draw Wellbore Background (light gray contours showing ID)
            if (wellbore.Any())
            {
                foreach (var section in wellbore)
                {
                    double sectionTop = (section.TopMD ?? 0) * verticalScale;
                    double sectionBottom = (section.BottomMD ?? 0) * verticalScale;
                    double sectionHeight = Math.Max(MinComponentHeight, sectionBottom - sectionTop);
                    
                    // OD: Wellbore wall / Formation
                    double sectionODWidth = (section.OD ?? 12.0) * ODScale;
                    double sectionInternalWidth;

                    // Calculate internal width (Hole Size for OpenHole, ID for Casing)
                    if (section.Component == ComponentType.OpenHole)
                    {
                        // OpenHole: Use OD as the base hole size
                        sectionInternalWidth = sectionODWidth;

                        // Visualize Washout if present
                        if (section.Washout.GetValueOrDefault() > 0)
                        {
                            // Expand width by Washout % (e.g. 10% -> 1.1x width)
                            double washoutFactor = 1.0 + (section.Washout.GetValueOrDefault() / 100.0);
                            sectionInternalWidth *= washoutFactor;
                        }
                    }
                    else
                    {
                        // Cased Hole: Use ID
                        sectionInternalWidth = (section.ID ?? 0) * ODScale;
                    }

                    // 1. Draw Formation/Wellbore Wall (The visual "hole" in the ground)
                    var outerRect = new Rectangle
                    {
                        Width = sectionInternalWidth, 
                        Height = sectionHeight,
                        Fill = section.Component == ComponentType.OpenHole 
                            ? new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)) // Bright hole effect
                            : Brushes.LightGray,
                        Stroke = section.Component == ComponentType.OpenHole ? Brushes.SaddleBrown : Brushes.DimGray,
                        StrokeThickness = section.Component == ComponentType.OpenHole ? 1.5 : 2.0,
                        StrokeDashArray = section.Component == ComponentType.OpenHole 
                            ? new DoubleCollection { 2, 2 } // Rough formation look
                            : null,
                        ToolTip = $"{section.Name}\nDepth: {section.TopMD}-{section.BottomMD} ft\n" +
                                  (section.Component == ComponentType.OpenHole 
                                    ? $"Hole: {section.OD}\" (Washout: {section.Washout}%)" 
                                    : $"Casing OD: {section.OD}\" ID: {section.ID}\"")
                    };

                    Canvas.SetLeft(outerRect, centerX - (sectionInternalWidth / 2));
                    Canvas.SetTop(outerRect, sectionTop);
                    SchematicCanvas.Children.Add(outerRect);

                    // 2. Draw Casing Pipe (if not OpenHole)
                    if (section.Component != ComponentType.OpenHole)
                    {
                        double casingODWidth = (section.OD ?? 0) * ODScale;
                        double casingIDWidth = (section.ID ?? 0) * ODScale;

                        // Pipe Body (Gray fill)
                        var pipeRect = new Rectangle
                        {
                            Width = casingODWidth,
                            Height = sectionHeight,
                            Fill = new SolidColorBrush(Color.FromArgb(40, 50, 50, 50)),
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(pipeRect, centerX - (casingODWidth / 2));
                        Canvas.SetTop(pipeRect, sectionTop);
                        SchematicCanvas.Children.Add(pipeRect);
                        
                        // Pipe Inner transparent (to show it's hollow)
                        // Actually, we just need the side walls. 
                        // But calculating "width" is easier with rectangles.
                    }
                }
            }

            // Draw Drill String Components (on top)
            // Iterate normally from top of list (Surface) to bottom (Bit)
            double maxStringY = 0;
            foreach (var comp in components)
            {
                double h = (comp.Length ?? 0) * verticalScale;
                if (h < 1 && comp.ComponentType != ComponentType.Bit) continue; // Skip near-zero segments except Bit
                if (h < 5 && comp.ComponentType == ComponentType.Bit) h = 10; // Ensure Bit is visible

                double w = (comp.OD ?? 5.0) * ODScale;
                double topY = (comp.TopMD ?? 0) * verticalScale;
                
                // Drawing Rect
                var rect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    Fill = GetColorForComponent(comp.ComponentType),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    ToolTip = $"{comp.ComponentType}: {comp.Length} ft x {comp.OD}\" OD ({comp.TopMD}-{comp.BottomMD} ft)"
                };

                Canvas.SetLeft(rect, centerX - (w / 2));
                Canvas.SetTop(rect, topY);
                SchematicCanvas.Children.Add(rect);

                // Add text label if height permits
                if (h > 15)
                {
                    var label = new TextBlock
                    {
                        Text = comp.Name ?? comp.ComponentType.ToString(),
                        FontSize = 8,
                        Width = w,
                        TextAlignment = TextAlignment.Center,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Canvas.SetLeft(label, centerX - (w / 2));
                    Canvas.SetTop(label, topY + (h / 2) - 6);
                    SchematicCanvas.Children.Add(label);
                }

                if (topY + h > maxStringY) maxStringY = topY + h;
            }
            currentY = maxStringY;

            // OnBottom Indicator
            if (vm.BitToBottom != null && Math.Abs(vm.BitToBottom.Value) < 0.1)
            {
                var marker = new System.Windows.Shapes.Path
                {
                    Data = System.Windows.Media.Geometry.Parse("M 0,0 L 10,10 L -10,10 Z"),
                    Fill = Brushes.Green,
                    ToolTip = "Bit is ON BOTTOM"
                };
                Canvas.SetLeft(marker, centerX);
                Canvas.SetTop(marker, currentY);
                SchematicCanvas.Children.Add(marker);
            }

            SchematicCanvas.Height = Math.Max(580, currentY + 100);
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
