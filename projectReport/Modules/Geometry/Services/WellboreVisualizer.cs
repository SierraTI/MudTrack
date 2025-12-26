using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Services
{
    public class WellboreVisualizer
    {
        private readonly Canvas _canvas;
        private const double MARGIN_TOP = 20;
        private const double MARGIN_LEFT = 50;
        private const double SCALE_WIDTH = 10; // Pixels per inch (width)
        
        public WellboreVisualizer(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void Draw(IEnumerable<WellboreComponent> wellboreComponents, IEnumerable<DrillStringComponent> drillStringComponents, double totalDepth)
        {
            _canvas.Children.Clear();

            if (totalDepth <= 0 || _canvas.ActualWidth <= 0 || _canvas.ActualHeight <= 0) return;

            double canvasHeight = _canvas.ActualHeight - (MARGIN_TOP * 2); // Leave margin at bottom too
            double scaleHeight = canvasHeight / totalDepth; // Pixels per foot (depth)
            
            // Calculate center X
            double centerX = _canvas.ActualWidth / 2;

            // Draw Wellbore (Casing/Open Hole)
            foreach (var section in wellboreComponents.OrderBy(s => s.TopMD))
            {
                DrawWellboreSection(section, scaleHeight, centerX);
            }

            // Draw Drill String
            // Drill string is typically configured Bottom-to-Top in the UI list (index 0 is Bit)
            // But visually it hangs from Surface (0).
            // We need to stack them from Top (Surface) downwards.
            // Assuming the list passed here is ordered such that we can stack them.
            // If the list is Bottom-to-Top (Bit first), we need to reverse it to stack from Surface.
            
            var dsList = drillStringComponents.ToList();
            double currentDepth = 0;
            
            // Iterate backwards to stack from Surface (assuming last item is top pipe)
            for (int i = dsList.Count - 1; i >= 0; i--)
            {
                var component = dsList[i];
                DrawDrillStringComponent(component, currentDepth, scaleHeight, centerX);
                currentDepth += component.Length.GetValueOrDefault();
            }
            
            // Draw Depth Scale (Left Axis)
            DrawDepthScale(totalDepth, scaleHeight);
        }

        private void DrawWellboreSection(WellboreComponent section, double scaleHeight, double centerX)
        {
            double topY = MARGIN_TOP + ((section.TopMD ?? 0) * scaleHeight);
            double height = ((section.BottomMD ?? 0) - (section.TopMD ?? 0)) * scaleHeight;
            double width = (section.ID ?? 0) * SCALE_WIDTH; 
            double leftX = centerX - (width / 2);

            // Colors per SRS
            Brush fillBrush;
            Brush strokeBrush = Brushes.Black;
            DoubleCollection? strokeDashArray = null;

            switch (section.SectionType)
            {
                case WellboreSectionType.Casing:
                    fillBrush = new SolidColorBrush(Color.FromRgb(218, 218, 218)); // #DADADA Gray
                    break;
                case WellboreSectionType.Liner:
                    fillBrush = Brushes.LightGray;
                    break;
                case WellboreSectionType.OpenHole:
                    fillBrush = Brushes.White;
                    strokeDashArray = new DoubleCollection { 4, 2 }; // Dashed outline
                    break;
                default:
                    fillBrush = Brushes.WhiteSmoke;
                    break;
            }

            var rect = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = strokeBrush,
                StrokeThickness = 1,
                StrokeDashArray = strokeDashArray,
                Fill = fillBrush,
                ToolTip = CreateToolTip(section.Name, $"ID: {section.ID.GetValueOrDefault()}\" \nDepth: {section.TopMD}-{section.BottomMD} ft")
            };

            Canvas.SetLeft(rect, leftX);
            Canvas.SetTop(rect, topY);
            _canvas.Children.Add(rect);

            // Draw Casing lines (OD) if not Open Hole
            if (section.SectionType != WellboreSectionType.OpenHole && section.OD.GetValueOrDefault() > section.ID.GetValueOrDefault())
            {
                double odWidth = section.OD.GetValueOrDefault() * SCALE_WIDTH;
                double odLeftX = centerX - (odWidth / 2);
                
                var casingRect = new Rectangle
                {
                    Width = odWidth,
                    Height = height,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent, // Only outline for OD
                    IsHitTestVisible = false // Let clicks pass through to ID rect
                };
                Canvas.SetLeft(casingRect, odLeftX);
                Canvas.SetTop(casingRect, topY);
                _canvas.Children.Add(casingRect);
            }
        }

        private void DrawDrillStringComponent(DrillStringComponent component, double startDepth, double scaleHeight, double centerX)
        {
            double topY = MARGIN_TOP + (startDepth * scaleHeight);
            double height = component.Length.GetValueOrDefault() * scaleHeight;
            double width = component.OD.GetValueOrDefault() * SCALE_WIDTH;
            double leftX = centerX - (width / 2);

            // Colors per SRS
            Brush fillBrush;
            switch (component.ComponentType)
            {
                case ComponentType.DrillPipe:
                    fillBrush = new SolidColorBrush(Color.FromRgb(238, 126, 50)); // #EE7E32 Orange
                    break;
                case ComponentType.HWDP:
                    fillBrush = new SolidColorBrush(Color.FromRgb(200, 100, 30)); // Darker Orange
                    break;
                case ComponentType.DC:
                    fillBrush = new SolidColorBrush(Color.FromRgb(112, 112, 111)); // #70706F Dark Gray
                    break;
                case ComponentType.Bit:
                    fillBrush = Brushes.Black;
                    break;
                default:
                    fillBrush = Brushes.Gray;
                    break;
            }

            var rect = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = Brushes.Black,
                StrokeThickness = 0.5,
                Fill = fillBrush,
                ToolTip = CreateToolTip(component.Name, $"Type: {component.ComponentType}\nOD: {component.OD.GetValueOrDefault()}\" ID: {component.ID.GetValueOrDefault()}\"\nLength: {component.Length.GetValueOrDefault()} ft")
            };

            Canvas.SetLeft(rect, leftX);
            Canvas.SetTop(rect, topY);
            _canvas.Children.Add(rect);
        }

        private void DrawDepthScale(double totalDepth, double scaleHeight)
        {
            // Draw markers every 1000 ft
            for (int depth = 0; depth <= totalDepth; depth += 1000)
            {
                double y = MARGIN_TOP + (depth * scaleHeight);
                
                var line = new Line
                {
                    X1 = MARGIN_LEFT - 5,
                    Y1 = y,
                    X2 = MARGIN_LEFT,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                _canvas.Children.Add(line);

                var text = new TextBlock
                {
                    Text = depth.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(text, MARGIN_LEFT - 35);
                Canvas.SetTop(text, y - 7);
                _canvas.Children.Add(text);
            }
        }

        private object CreateToolTip(string title, string content)
        {
            var stack = new StackPanel { Margin = new Thickness(5) };
            stack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = content });
            
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = stack
            };
            return border;
        }
    }
}
