using System.Windows;
using System.Windows.Controls;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;

namespace ProjectReport.Views.Geometry
{
    public partial class DrillStringGeometryView : UserControl
    {
        private readonly GeometryViewModel _viewModel;

        public DrillStringGeometryView()
        {
            InitializeComponent();
            
            // Initialize services
            var geometryService = new GeometryCalculationService();
            var dataService = new DataPersistenceService();
            
            // Initialize ViewModel with required services
            _viewModel = new GeometryViewModel(geometryService, dataService, new ThermalGradientService());
            DataContext = _viewModel;
        }

        private void AddDrillStringComponent_Click(object sender, RoutedEventArgs e)
        {
            var newComponent = new DrillStringComponent
            {
                Name = "New Component",
                ComponentType = ComponentType.DrillPipe,
                Length = 100,
                OD = 5,
                ID = 4.276
            };
            
            // Calculate initial volume
            var geometryService = new GeometryCalculationService();
            // Volume calculations are now handled automatically in the model
            
            _viewModel.DrillStringComponents.Add(newComponent);
        }

        private void ConfigureComponent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DrillStringComponent component)
            {
                switch (component.ComponentType)
                {
                    case ComponentType.DrillPipe:
                    case ComponentType.HWDP:
                        var toolJointWindow = new ToolJointConfigWindow(component.ToolJoint ?? null, component.ComponentType);
                        if (toolJointWindow.ShowDialog() == true)
                        {
                            component.ToolJoint = toolJointWindow.Config;
                            component.IsToolJointConfigured = true;
                        }
                        break;

                    case ComponentType.Motor:
                    case ComponentType.MWD:
                    case ComponentType.PWD:
                        var pressureDropWindow = new PressureDropConfigWindow(component.PressureDropConfig ?? null);
                        if (pressureDropWindow.ShowDialog() == true)
                        {
                            component.PressureDropConfig = pressureDropWindow.Config;
                            component.IsPressureDropConfigured = true;
                        }
                        break;

                    case ComponentType.Bit:
                        var bitJetsWindow = new BitJetsConfigWindow(component.BitJetsConfig ?? null);
                        if (bitJetsWindow.ShowDialog() == true)
                        {
                            component.BitJetsConfig = bitJetsWindow.Config;
                            component.IsTfaConfigured = true;
                        }
                        break;
                }
                
                _viewModel.RecalculateTotals();
            }
        }

        #region Drag and Drop

        private void DrillStringDataGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Optional: Handle grid level mouse down if needed
        }

        private void DrillStringDataGrid_Drop(object sender, DragEventArgs e)
        {
            // Optional: Handle grid level drop if needed
        }

        private void Row_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                DragDrop.DoDragDrop(row, row.Item, DragDropEffects.Move);
            }
        }

        private void Row_Drop(object sender, DragEventArgs e)
        {
            if (sender is DataGridRow targetRow && e.Data.GetDataPresent(typeof(DrillStringComponent)))
            {
                var droppedData = e.Data.GetData(typeof(DrillStringComponent)) as DrillStringComponent;
                var targetData = targetRow.Item as DrillStringComponent;

                if (droppedData != null && targetData != null && droppedData != targetData)
                {
                    int oldIndex = _viewModel.DrillStringComponents.IndexOf(droppedData);
                    int newIndex = _viewModel.DrillStringComponents.IndexOf(targetData);

                    if (oldIndex != -1 && newIndex != -1)
                    {
                        _viewModel.DrillStringComponents.Move(oldIndex, newIndex);
                        _viewModel.RecalculateTotals();
                    }
                }
            }
        }

        #endregion
    }
}

