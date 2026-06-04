using System.Windows;
using System.Windows.Controls;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;
using ProjectReport.ViewModels.Geometry.Wellbore;

namespace ProjectReport.Views.Geometry.Wellbore
{
    /// <summary>
    /// Vista específica para Wellbore Geometry.
    /// Muestra la cuadrícula de secciones de wellbore con validaciones y acciones.
    /// </summary>
    public partial class WellboreGeometryView : UserControl
    {
        private GeometryViewModel? _viewModel;
        private WellboreVisualizer? _visualizer;

        public WellboreGeometryView()
        {
            InitializeComponent();
            
            // Initialize visualizer with the Canvas
            if (WellboreCanvas != null)
            {
                _visualizer = new WellboreVisualizer(WellboreCanvas);
            }

            DataContextChanged += (s, e) =>
            {
                // We need the Main GeometryViewModel for DrillString context, 
                // but if the View binds to DataContext which might be main VM or sub VM.
                // Generally GeometryView binds to GeometryViewModel.
                // Let's assume DataContext is GeometryViewModel based on usage in XAML bindings like "WellboreComponents".
                
                if (e.NewValue is GeometryViewModel vm)
                {
                    _viewModel = vm;
                    
                    // Subscribe to collections to trigger redraw
                    if (_viewModel.WellboreComponents != null)
                        _viewModel.WellboreComponents.CollectionChanged += (sender, args) => DrawSchematic();
                        
                    if (_viewModel.DrillStringComponents != null)
                        _viewModel.DrillStringComponents.CollectionChanged += (sender, args) => DrawSchematic();
                        
                    // Subscribe to property changes via RecalculateTotals event or similar?
                    // GeometryViewModel usually fires "TotalWellboreMD" which is a good trigger.
                    _viewModel.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(GeometryViewModel.TotalWellboreMD) ||
                            args.PropertyName == nameof(GeometryViewModel.TotalWellboreVolume) ||
                            args.PropertyName == nameof(GeometryViewModel.ShoeDepth)) // ShoeDepth change implies geometry change
                        {
                            DrawSchematic();
                        }
                    };
                    
                    // Initial Draw
                    DrawSchematic();
                }
            };
            
            // Redraw on resize
            SizeChanged += (s, e) => DrawSchematic();
        }

        private void DrawSchematic()
        {
            if (_viewModel == null || _visualizer == null || WellboreCanvas == null) return;
            
            // Ensure we are on UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DrawSchematic);
                return;
            }

            _visualizer.Draw(
                _viewModel.WellboreComponents, 
                _viewModel.DrillStringComponents, 
                _viewModel.TotalWellboreMD > 0 ? _viewModel.TotalWellboreMD : 1000 // Default depth if empty
            );
        }
    }
}
