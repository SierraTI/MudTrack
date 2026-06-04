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

            // CRITICAL FIX: Allow rendering if EITHER list has data, don't require wellbore
            if (!wellbore.Any() && !drillString.Any()) return;

            // Calculate Scales - Robust against empty lists
            double wellboreBottom = wellbore.Any() ? wellbore.Max(w => w.BottomMD ?? 0) : 0;
            double stringLen = drillString.Any() ? drillString.Sum(ds => ds.Length ?? 0) : 0;
            
            double maxMD = Math.Max(wellboreBottom, stringLen);
            
            if (maxMD <= 0) maxMD = 1000; // Default fallback
            
            double availableHeight = Math.Max(600, ActualHeight - 40);
            double verticalScale = availableHeight / maxMD;

            // --- LAYER 0: Formation Background (Optional, adds context) ---
            var formationBg = new Rectangle
            {
                Width = canvasWidth,
                Height = maxMD * verticalScale + 50,
                Fill = new SolidColorBrush(Color.FromRgb(250, 250, 250)) // Very light gray/paper
            };
            Canvas.SetLeft(formationBg, 0);
            Canvas.SetTop(formationBg, 0);
            SchematicCanvas.Children.Add(formationBg);

            // --- LAYER 1: Wellbore (Mud/Annulus Background) ---
            if (wellbore.Any())
            {
                foreach (var section in wellbore)
                {
                    double top = (section.TopMD ?? 0) * verticalScale;
                    double bottom = (section.BottomMD ?? 0) * verticalScale;
                    double h = Math.Max(MinSegmentHeight, bottom - top);
                    
                    // Use ID for visual width of the "Hole"
                    double id = section.ID ?? (section.OD ?? 12.0) - 1.0; 
                    double w = Math.Max(2, id * ODScale);

                    // Fluid/Hole Rectangle
                    var rect = new Rectangle
                    {
                        Width = w,
                        Height = h,
                        Fill = (Brush?)new BrushConverter().ConvertFromString("#E0F2FE") ?? Brushes.LightCyan, // Mud Color
                        StrokeThickness = 0
                    };

                    Canvas.SetLeft(rect, centerX - (w / 2));
                    Canvas.SetTop(rect, top + 10);
                    SchematicCanvas.Children.Add(rect);

                    // Wall Lines (Casing/OpenHole boundary)
                    var wallBrush = section.SectionType == ComponentType.OpenHole 
                        ? Brushes.SaddleBrown 
                        : Brushes.Black;
                    
                    double wallThickness = section.SectionType == ComponentType.OpenHole ? 2 : 1;

                    // Left Wall
                    var leftLine = new Line
                    {
                        X1 = centerX - (w / 2), Y1 = top + 10,
                        X2 = centerX - (w / 2), Y2 = top + h + 10,
                        Stroke = wallBrush,
                        StrokeThickness = wallThickness
                    };
                    
                    // Right Wall
                    var rightLine = new Line
                    {
                        X1 = centerX + (w / 2), Y1 = top + 10,
                        X2 = centerX + (w / 2), Y2 = top + h + 10,
                        Stroke = wallBrush,
                        StrokeThickness = wallThickness
                    };
                    
                    SchematicCanvas.Children.Add(leftLine);
                    SchematicCanvas.Children.Add(rightLine);

                    // Start Depth Label (only for 0)
                    if (Math.Abs(section.TopMD ?? 0) < 0.1)
                    {
                         AddDepthLabel(0, 10, centerX - (w/2) - 10, false);
                    }

                    // Shoe Depth Label
                    if (section.SectionType != ComponentType.OpenHole)
                    {
                        AddDepthLabel(section.BottomMD ?? 0, top + h + 10, centerX + (w / 2) + 5, true);
                    }
                }
            }
            else
            {
                // Fallback if no wellbore: Draw generic "Hole" based on max string OD
                double fallbackOD = drillString.Any() ? (drillString.Max(c => c.OD ?? 5) * 1.5) : 12.0;
                double w = fallbackOD * ODScale;
                var rect = new Rectangle
                {
                    Width = w,
                    Height = maxMD * verticalScale,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.LightGray,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                Canvas.SetLeft(rect, centerX - (w / 2));
                Canvas.SetTop(rect, 10);
                SchematicCanvas.Children.Add(rect);
            }

            // --- LAYER 2: Drill String (Foreground) ---
            double currentY = 10; // Start at Surface
            
            // Logic for Off-Bottom / Tripping Visualization
            // If vm.CurrentBitDepth < TotalStringLength, trim the string to fit.
            // We must simulate that the BHA is at BitDepth, and the Top Pipe is shorter.
            
            double bitDepth = vm.CurrentBitDepth > 0 ? vm.CurrentBitDepth : vm.TotalDrillStringLength;
            
            // Build Visual String (Bottom-Up Logic like RecalculateTotals)
            var visualString = new System.Collections.Generic.List<(DrillStringComponent Comp, double Length)>();
            double remainingDepth = bitDepth;

            // Iterate backwards (Bit first)
            for (int i = drillString.Count - 1; i >= 0; i--)
            {
                var comp = drillString[i];
                double len = comp.Length ?? 0;
                
                if (remainingDepth <= 0) break;
                
                double effectiveLength = Math.Min(len, remainingDepth);
                if (effectiveLength > 0)
                {
                    visualString.Insert(0, (comp, effectiveLength)); // Insert at top to maintain Top-Down order for drawing
                }
                
                remainingDepth -= effectiveLength;
            }

            // Draw Visual String
            foreach (var (comp, length) in visualString)
            {
                double h = length * verticalScale;
                double compOD = comp.OD ?? 5.0;
                double w = compOD * ODScale;

                // Container for Rect + Text
                var container = new Grid
                {
                    Width = w,
                    Height = h
                };
                Canvas.SetLeft(container, centerX - (w / 2));
                Canvas.SetTop(container, currentY);

                // Special Draw for Bit
                if (comp.ComponentType == ComponentType.Bit)
                {
                    // Draw Bit as Triangle/Trapezoid pointing DOWN
                    var bitShape = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new Point(0, 0),    // Top Left
                            new Point(w, 0),    // Top Right
                            new Point(w/2, h)   // Bottom Center (Tip)
                        },
                        Fill = Brushes.Crimson,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        ToolTip = $"{comp.Name}\nLength: {length:F2} ft\nOD: {comp.OD}\""
                    };
                    
                    Canvas.SetLeft(bitShape, centerX - (w / 2));
                    Canvas.SetTop(bitShape, currentY);
                    SchematicCanvas.Children.Add(bitShape);
                }
                else
                {
                    // Standard Component Rectangle
                    var rect = new Rectangle
                    {
                        Width = w,
                        Height = h,
                        Fill = GetColorForComponent(comp.ComponentType),
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.5,
                        ToolTip = $"{comp.Name}\nLength: {length:F2} ft\nOD: {comp.OD}\""
                    };

                    container.Children.Add(rect);

                    // Add Label if height is sufficient
                    if (h > 14) 
                    {
                        var label = new TextBlock
                        {
                            Text = comp.Name,
                            FontSize = Math.Max(8, Math.Min(11, w - 2)), // Dynamic font size
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            IsHitTestVisible = false
                        };
                        container.Children.Add(label);
                    }

                    SchematicCanvas.Children.Add(container);
                }

                currentY += h;
            }

            // Final TD Label (Drill String Bottom)
            double drillStringBottom = drillString.Sum(c => c.Length ?? 0);
            AddDepthLabel(drillStringBottom, currentY, centerX - 50, false);

            SchematicCanvas.Height = Math.Max(currentY, (maxMD * verticalScale) + 10) + 50;
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
