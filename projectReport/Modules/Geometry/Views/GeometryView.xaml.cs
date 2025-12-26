using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Models.Geometry;
using ProjectReport.Services;
using ProjectReport.Services.Survey;
using ProjectReport.ViewModels.Geometry;
using ProjectReport.Views.Geometry.BitAndJets;
using ProjectReport.Views.Geometry;
using ProjectReport.ViewModels.Geometry.BitAndJets;
using ProjectReport.ViewModels.Geometry.FluidsAndPressure;
using ProjectReport.ViewModels.Geometry.ThermalGradient;
using ProjectReport.ViewModels.Geometry.DrillString;

namespace ProjectReport.Views
{
    public partial class GeometryView : UserControl
    {
        private GeometryViewModel? _viewModel;
        private WellboreVisualizer _visualizer;
        private object? _draggedItem;
        private int _draggedIndex = -1;
        private Point _dragStartPoint;
        public GeometryView()
        {
            try
            {
                InitializeComponent();

                // Initialize default ViewModel if not set (will be checked/updated in OnDataContextChanged)
                if (DataContext == null)
                {
                    // Initialize services
                    var geometryService = new GeometryCalculationService();
                    var dataService = new DataPersistenceService();
                    var thermalService = new ThermalGradientService();

                    // Initialize ViewModel with required services
                    _viewModel = new GeometryViewModel(geometryService, dataService, thermalService);
                    DataContext = _viewModel;
                }
                else if (DataContext is GeometryViewModel vm)
                {
                    _viewModel = vm;
                }

                // Initialize Visualizer
                _visualizer = new WellboreVisualizer(VisualSchemeCanvas);

                // Subscribe to events
                Loaded += GeometryView_Loaded;
                DataContextChanged += GeometryView_DataContextChanged;
                KeyDown += GeometryView_KeyDown; // Add keyboard shortcuts

                SubscribeToViewModelEvents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing GeometryView: {ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void GeometryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is GeometryViewModel newVm)
            {
                UnsubscribeFromViewModelEvents();
                _viewModel = newVm;
                SubscribeToViewModelEvents();
                UpdateVisualization();
            }
        }

        private void SubscribeToViewModelEvents()
        {
            if (_viewModel == null) return;

            _viewModel.WellboreComponents.CollectionChanged += WellboreComponents_CollectionChanged;
            foreach (var component in _viewModel.WellboreComponents)
            {
                component.PropertyChanged += WellboreComponent_PropertyChanged;
            }
            foreach (var component in _viewModel.DrillStringComponents)
            {
                component.PropertyChanged += DrillStringComponent_PropertyChanged;
            }
            foreach (var point in _viewModel.SurveyPoints)
            {
                point.PropertyChanged += SurveyPoint_PropertyChanged;
            }

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void UnsubscribeFromViewModelEvents()
        {
            if (_viewModel == null) return;

            _viewModel.WellboreComponents.CollectionChanged -= WellboreComponents_CollectionChanged;
            // Note: Individual item subscriptions are harder to unsubscribe in bulk without tracking, 
            // but for this scope it might be acceptable or we track them.
            // Ideally we iterate existing items and unsubscribe.
            foreach (var component in _viewModel.WellboreComponents)
            {
                component.PropertyChanged -= WellboreComponent_PropertyChanged;
            }
            foreach (var component in _viewModel.DrillStringComponents)
            {
                component.PropertyChanged -= DrillStringComponent_PropertyChanged;
            }
            foreach (var point in _viewModel.SurveyPoints)
            {
                point.PropertyChanged -= SurveyPoint_PropertyChanged;
            }

            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void GeometryView_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+S: Save
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _viewModel.SaveCommand.Execute(null);
                e.Handled = true;
            }
            // Ctrl+N: Add new row to current tab
            else if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (_viewModel.SelectedTabIndex)
                {
                    case 0: AddWellboreSection_Click(this, new RoutedEventArgs()); break;
                    case 1: AddDrillStringComponent_Click(this, new RoutedEventArgs()); break;
                    case 2: AddSurveyPoint_Click(this, new RoutedEventArgs()); break;
                    case 4: AddWellTest_Click(this, new RoutedEventArgs()); break;
                }
                e.Handled = true;
            }
            // Ctrl+Tab: Next/Previous tab
            else if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Ctrl+Shift+Tab: Previous tab
                    if (_viewModel.SelectedTabIndex > 0)
                        _viewModel.SelectedTabIndex--;
                }
                else
                {
                    // Ctrl+Tab: Next tab
                    if (_viewModel.SelectedTabIndex < 5)
                        _viewModel.SelectedTabIndex++;
                }
                e.Handled = true;
            }
        }

        private void GeometryView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateVisualization();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeometryViewModel.SelectedTabIndex) ||
                e.PropertyName == nameof(GeometryViewModel.TotalWellboreMD))
            {
                UpdateVisualization();
            }
        }

        private void UpdateVisualization()
        {
            if (_viewModel.SelectedTabIndex == 5 && VisualSchemeCanvas != null)
            {
                try
                {
                    _visualizer.Draw(
                        _viewModel.WellboreComponents,
                        _viewModel.DrillStringComponents,
                        _viewModel.TotalWellboreMD > 0 ? _viewModel.TotalWellboreMD : 1000);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating visualization: {ex.Message}");
                }
            }
        }

        private void AddWellboreSection_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.AddWellboreSectionCommand.CanExecute(null) == true)
            {
                _viewModel.AddWellboreSectionCommand.Execute(null);
            }
        }

        private void AddDrillStringComponent_Click(object sender, RoutedEventArgs e)
        {
            var newComponent = new DrillStringComponent
            {
                Id = _viewModel.GetNextDrillStringId(),
                Name = "New Component",
                ComponentType = ComponentType.DrillPipe,
                Length = null,
                OD = null,
                ID = null
            };

            var geometryService = new GeometryCalculationService();
            // Volume calculations are now handled automatically in the model

            _viewModel.DrillStringComponents.Add(newComponent);
            newComponent.PropertyChanged += DrillStringComponent_PropertyChanged;
            _viewModel.RecalculateTotals();
        }

        private void AddBitComponent_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.InsertStandardBhaComponent(ComponentType.Bit);
        }

        private void AddDcComponent_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.InsertStandardBhaComponent(ComponentType.DC);
        }

        private void AddHwdpComponent_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.InsertStandardBhaComponent(ComponentType.HWDP);
        }

        private void ConfigureComponent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DrillStringComponent component)
            {
                switch (component.ComponentType)
                {
                    case ComponentType.DrillPipe:
                    case ComponentType.HWDP:
                    case ComponentType.DC:
                        var toolJointWindow = new ProjectReport.Views.Geometry.ToolJointConfigWindow(component.ToolJoint ?? null, component.ComponentType);
                        if (toolJointWindow.ShowDialog() == true)
                        {
                            component.ToolJoint = toolJointWindow.Config;
                            component.IsToolJointConfigured = true;
                        }
                        break;

                    case ComponentType.Motor:
                    case ComponentType.MWD:
                    case ComponentType.PWD:
                        var pdConfig = component.PressureDropConfig ?? new PressureDropConfig { MudDensity = component.FluidDensity.GetValueOrDefault() };
                        var pressureDropWindow = new ProjectReport.Views.Geometry.PressureDropConfigWindow(pdConfig);
                        if (pressureDropWindow.ShowDialog() == true)
                        {
                            component.PressureDropConfig = pressureDropWindow.Config;
                            component.IsPressureDropConfigured = true;
                            component.FluidDensity = component.PressureDropConfig.MudDensity;
                        }
                        break;

                    case ComponentType.Bit:
                        var bitJetsWindow = new ProjectReport.Views.Geometry.BitAndJets.BitJetsConfigWindow(component.BitJetsConfig ?? null);
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

        private void AddSurveyPoint_Click(object sender, RoutedEventArgs e)
        {
            var newPoint = new SurveyPoint
            {
                Id = _viewModel.GetNextSurveyId(),
                MD = 0,
                TVD = 0,
                HoleAngle = 0,
                Azimuth = 0
            };

            _viewModel.SurveyPoints.Add(newPoint);
            newPoint.PropertyChanged += SurveyPoint_PropertyChanged;
        }

        private void AddWellTest_Click(object sender, RoutedEventArgs e)
        {
            var newTest = new WellTest
            {
                Id = _viewModel.GetNextWellTestId(),
                Section = "Section 1",
                Type = WellTestType.LeakOff,
                TestValue = 0,
                MD = 0,
                TVD = 0
            };

            _viewModel.WellTests.Add(newTest);
        }

        // Drag and Drop
        private void WellboreDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var row = GetDataGridRow(dataGrid, e.GetPosition(dataGrid));
                if (row != null)
                {
                    _draggedItem = row.Item;
                    _draggedIndex = dataGrid.Items.IndexOf(_draggedItem);
                    _dragStartPoint = e.GetPosition(null);
                }
            }
        }

        private void WellboreDataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
            {
                Point currentPos = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    try
                    {
                        DragDrop.DoDragDrop(sender as DataGrid, _draggedItem, DragDropEffects.Move);
                    }
                    finally
                    {
                        _draggedItem = null;
                        _draggedIndex = -1;
                    }
                }
            }
        }

        private void WellboreDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (sender is DataGrid dataGrid && _draggedItem is WellboreComponent draggedItem)
            {
                var row = GetDataGridRow(dataGrid, e.GetPosition(dataGrid));
                if (row != null && row.Item is WellboreComponent targetItem)
                {
                    int targetIndex = dataGrid.Items.IndexOf(targetItem);
                    if (targetIndex >= 0 && _draggedIndex >= 0 && targetIndex != _draggedIndex)
                    {
                        _viewModel.WellboreComponents.Move(_draggedIndex, targetIndex);
                        UpdateWellboreContinuity();
                    }
                }
                _draggedItem = null;
                _draggedIndex = -1;
            }
        }

        private void DrillStringDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var row = GetDataGridRow(dataGrid, e.GetPosition(dataGrid));
                if (row != null)
                {
                    _draggedItem = row.Item;
                    _draggedIndex = dataGrid.Items.IndexOf(_draggedItem);
                    _dragStartPoint = e.GetPosition(null);
                }
            }
        }

        private void DrillStringDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (sender is DataGrid dataGrid && _draggedItem is DrillStringComponent draggedItem)
            {
                var row = GetDataGridRow(dataGrid, e.GetPosition(dataGrid));
                if (row != null && row.Item is DrillStringComponent targetItem)
                {
                    int targetIndex = dataGrid.Items.IndexOf(targetItem);
                    if (targetIndex >= 0 && _draggedIndex >= 0 && targetIndex != _draggedIndex)
                    {
                        _viewModel.DrillStringComponents.Move(_draggedIndex, targetIndex);
                        _viewModel.RecalculateTotals();
                    }
                }
                _draggedItem = null;
                _draggedIndex = -1;
            }
        }

        private void DrillStringDataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
            {
                Point currentPos = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    try
                    {
                        DragDrop.DoDragDrop(sender as DataGrid, _draggedItem, DragDropEffects.Move);
                    }
                    finally
                    {
                        _draggedItem = null;
                        _draggedIndex = -1;
                    }
                }
            }
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Optional: Handle key events if needed, e.g. Delete
        }

        private DataGridRow? GetDataGridRow(DataGrid grid, Point position)
        {
            var element = grid.InputHitTest(position) as UIElement;
            while (element != null)
            {
                if (element is DataGridRow row) return row;
                element = VisualTreeHelper.GetParent(element) as UIElement;
            }
            return null;
        }

        private void UpdateWellboreContinuity()
        {
            // Validate continuity after drag-and-drop reordering
            var sorted = _viewModel.WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var current = sorted[i];
                var next = sorted[i + 1];

                // Only validate continuity when both values are present
                if (current.BottomMD.HasValue && next.TopMD.HasValue)
                {
                    // BR-WG-002: Check if Bottom MD of current section equals Top MD of next section
                    if (Math.Abs(current.BottomMD.Value - next.TopMD.Value) > 0.01)
                    {
                        // Show continuity error dialog
                        var dialog = new ProjectReport.Views.Geometry.ContinuityErrorDialog(current, next);
                        var result = dialog.ShowDialog();

                        if (result == true)
                        {
                            // User fixed the error, recalculate totals
                            _viewModel.RecalculateTotals();
                            return;
                        }
                        else
                        {
                            // User cancelled, don't auto-fix
                            return;
                        }
                    }
                }
            }

            _viewModel.RecalculateTotals();
        }

        // Property Changed Handlers
        private void WellboreComponents_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (WellboreComponent item in e.NewItems)
                    item.PropertyChanged += WellboreComponent_PropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (WellboreComponent item in e.OldItems)
                    item.PropertyChanged -= WellboreComponent_PropertyChanged;
            }
            _viewModel?.RecalculateTotals();
        }

        private void WellboreComponent_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WellboreComponent.BottomMD) ||
                e.PropertyName == nameof(WellboreComponent.ID) ||
                e.PropertyName == nameof(WellboreComponent.OD))
            {
                _viewModel?.RecalculateTotals();
            }
        }

        private void DrillStringComponent_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrillStringComponent.Length) ||
                e.PropertyName == nameof(DrillStringComponent.ID) ||
                e.PropertyName == nameof(DrillStringComponent.OD))
            {
                _viewModel?.RecalculateTotals();
            }
        }

        private void SurveyPoint_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle survey changes if needed
        }

        private void LoadSurveyFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    DefaultExt = ".csv",
                    Title = "Import Survey Data"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var importService = new SurveyImportService();
                    var result = importService.ImportFromCsv(openFileDialog.FileName);

                    if (result.Success)
                    {
                        // Ask user: Replace or Append?
                        var messageResult = MessageBox.Show(
                            $"Successfully imported {result.ImportedCount} survey points.\n\n" +
                            $"Do you want to REPLACE existing survey data?\n" +
                            $"Click 'Yes' to replace, 'No' to append, 'Cancel' to abort.",
                            "Import Survey Data",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (messageResult == MessageBoxResult.Cancel)
                        {
                            return;
                        }

                        if (messageResult == MessageBoxResult.Yes)
                        {
                            // Replace: Clear existing data
                            _viewModel.SurveyPoints.Clear();
                        }

                        // Add imported points
                        int nextId = _viewModel.SurveyPoints.Count > 0
                            ? _viewModel.SurveyPoints.Max(p => p.Id) + 1
                            : 1;

                        foreach (var point in result.SurveyPoints)
                        {
                            point.Id = nextId++;
                            point.PropertyChanged += SurveyPoint_PropertyChanged;
                            _viewModel.SurveyPoints.Add(point);
                        }

                        // Show success message with errors if any
                        string message = $"✓ Successfully imported {result.ImportedCount} survey points.";
                        if (result.ErrorCount > 0)
                        {
                            message += $"\n\n⚠ {result.ErrorCount} rows had errors and were skipped.";
                            if (result.DetailedErrors.Count > 0)
                            {
                                message += "\n\nErrors:\n" + string.Join("\n", result.DetailedErrors.Take(10));
                                if (result.DetailedErrors.Count > 10)
                                {
                                    message += $"\n... and {result.DetailedErrors.Count - 10} more errors.";
                                }
                            }
                        }

                        MessageBox.Show(message, "Import Complete", MessageBoxButton.OK,
                            result.ErrorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Import failed: {result.ErrorMessage}\n\n" +
                            $"Errors: {result.ErrorCount}\n" +
                            (result.DetailedErrors.Count > 0 ? "\n" + string.Join("\n", result.DetailedErrors.Take(5)) : ""),
                            "Import Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing survey data: {ex.Message}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteWellbore_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellboreComponent section)
            {
                if (_viewModel?.DeleteWellboreSectionCommand.CanExecute(section) == true)
                {
                    _viewModel.DeleteWellboreSectionCommand.Execute(section);
                }
            }
        }

        private void MoveWellboreUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellboreComponent section)
            {
                int index = _viewModel.WellboreComponents.IndexOf(section);
                if (index > 0) _viewModel.WellboreComponents.Move(index, index - 1);
            }
        }

        private void MoveWellboreDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellboreComponent section)
            {
                int index = _viewModel.WellboreComponents.IndexOf(section);
                if (index < _viewModel.WellboreComponents.Count - 1) _viewModel.WellboreComponents.Move(index, index + 1);
            }
        }

        private void DeleteDrillString_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DrillStringComponent component)
            {
                _viewModel.DrillStringComponents.Remove(component);
            }
        }

        private void MoveDrillStringUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DrillStringComponent component)
            {
                int index = _viewModel.DrillStringComponents.IndexOf(component);
                if (index > 0) _viewModel.DrillStringComponents.Move(index, index - 1);
            }
        }

        private void MoveDrillStringDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DrillStringComponent component)
            {
                int index = _viewModel.DrillStringComponents.IndexOf(component);
                if (index < _viewModel.DrillStringComponents.Count - 1) _viewModel.DrillStringComponents.Move(index, index + 1);
            }
        }

        // Survey Actions
        private void DeleteSurvey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SurveyPoint point)
            {
                _viewModel.SurveyPoints.Remove(point);
            }
        }
        private void MoveSurveyUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SurveyPoint point)
            {
                int index = _viewModel.SurveyPoints.IndexOf(point);
                if (index > 0) _viewModel.SurveyPoints.Move(index, index - 1);
            }
        }
        private void MoveSurveyDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SurveyPoint point)
            {
                int index = _viewModel.SurveyPoints.IndexOf(point);
                if (index < _viewModel.SurveyPoints.Count - 1) _viewModel.SurveyPoints.Move(index, index + 1);
            }
        }
        // Well Test Actions
        private void DeleteWellTest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellTest test)
            {
                _viewModel.WellTests.Remove(test);
            }
        }
        private void MoveWellTestUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellTest test)
            {
                int index = _viewModel.WellTests.IndexOf(test);
                if (index > 0) _viewModel.WellTests.Move(index, index - 1);
            }
        }
        private void MoveWellTestDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellTest test)
            {
                int index = _viewModel.WellTests.IndexOf(test);
                if (index < _viewModel.WellTests.Count - 1) _viewModel.WellTests.Move(index, index + 1);
            }
        }
    }
}