using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Services;
using ProjectReport.Services.Survey;
using ProjectReport.ViewModels.Geometry;
using ProjectReport.ViewModels.Geometry.BitAndJets;
using ProjectReport.ViewModels.Geometry.DrillString;
using ProjectReport.ViewModels.Geometry.FluidsAndPressure;
using ProjectReport.ViewModels.Geometry.ThermalGradient;
using ProjectReport.Views.Geometry;
using ProjectReport.Views.Geometry.BitAndJets;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProjectReport.Views
{
    public partial class GeometryView : UserControl
    {
        private GeometryViewModel? _viewModel;
        private object? _draggedItem;
        private int _draggedIndex = -1;
        private Point _dragStartPoint;
        public GeometryView()
        {
            try
            {
                InitializeComponent();

                Loaded += GeometryView_Loaded;
                DataContextChanged += GeometryView_DataContextChanged;
                KeyDown += GeometryView_KeyDown;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error initializing GeometryView: {ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

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
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // Note: RecalculateTotals is already handled internally by the ViewModel
            // through its own PropertyChanged and CollectionChanged subcriptions.
            // No redundant subscriptions needed here.
        }

        private void UnsubscribeFromViewModelEvents()
        {
            if (_viewModel == null) return;
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void GeometryView_KeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;

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
                    case 0: _viewModel.AddWellboreSectionCommand.Execute(null); break;
                    case 1: _viewModel.AddDrillStringComponentCommand.Execute(null); break;
                    case 2: _viewModel.AddSurveyPointCommand.Execute(null); break;
                    case 4: 
                        if (_viewModel.AddWellTestCommand.CanExecute(null))
                            _viewModel.AddWellTestCommand.Execute(null); 
                        break;
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
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
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
            // Use WellboreSchematicView's DrawSchematic instead of a separate canvas visualizer
            if (_viewModel == null) return;

            if (_viewModel.SelectedTabIndex == 1)
            {
                try
                {
                    this.BhaVisualizer?.DrawSchematic();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating BHA visualization: {ex.Message}");
                }
            }

            if (_viewModel.SelectedTabIndex == 5)
            {
                // Summary tab - update the MasterSchematic visual
                try
                {
                    if (this.MasterSchematic != null)
                    {
                        this.MasterSchematic.DataContext = _viewModel;
                        this.MasterSchematic.DrawSchematic();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating Summary visualization: {ex.Message}");
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
                        var toolJointWindow = new ProjectReport.Views.Geometry.ToolJointConfigWindow(component.ToolJoint ?? null, component.ComponentType);
                        if (toolJointWindow.ShowDialog() == true)
                        {
                            component.ToolJoint = toolJointWindow.Config;
                            component.IsToolJointConfigured = true;
                        }
                        break;

                    case ComponentType.Motor:
                    case ComponentType.MWD:
                    case ComponentType.LWD:
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
                        var bitJetsWindow = new ProjectReport.Views.Geometry.BitAndJets.BitJetsConfigWindow(component.MultiBitJetsConfig ?? new ProjectReport.Models.Geometry.BitAndJets.MultiBitJetsConfig());
                        if (bitJetsWindow.ShowDialog() == true)
                        {
                            component.MultiBitJetsConfig = bitJetsWindow.Config;
                            component.IsTfaConfigured = true;
                        }
                        break;
                }

                _viewModel?.RecalculateTotals();
            }
        }

        private void AddSurveyPoint_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            
            // Check if surface point (MD=0) exists
            var surfacePoint = _viewModel.SurveyPoints.FirstOrDefault(p => Math.Abs(p.MD) < 0.01);
            
            if (surfacePoint == null && _viewModel.SurveyPoints.Count == 0)
            {
                // Create surface point (MD=0) as first point
                var newPoint = new SurveyPoint
                {
                    Id = _viewModel.GetNextSurveyId(),
                    MD = 0,
                    HoleAngle = 0,
                    Azimuth = 0,
                    IsTieInPoint = true
                };
                _viewModel.SurveyPoints.Add(newPoint);
                newPoint.PropertyChanged += SurveyPoint_PropertyChanged;
            }
            else
            {
                // Create new point at next depth
                var sorted = _viewModel.SurveyPoints.OrderBy(p => p.MD).ToList();
                double nextMD = sorted.Count > 0 ? sorted.Last().MD + 100 : 100; // Default increment of 100 ft
                
                var newPoint = new SurveyPoint
                {
                    Id = _viewModel.GetNextSurveyId(),
                    MD = nextMD,
                    HoleAngle = sorted.Count > 0 ? sorted.Last().HoleAngle : 0,
                    Azimuth = sorted.Count > 0 ? sorted.Last().Azimuth : 0
                };
                _viewModel.SurveyPoints.Add(newPoint);
                newPoint.PropertyChanged += SurveyPoint_PropertyChanged;
            }
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
            if (_viewModel == null) return;

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
            if (_viewModel == null) return;

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
            var sorted = _viewModel?.WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            if (sorted == null) return;

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
                            _viewModel?.RecalculateTotals();
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

            _viewModel?.RecalculateTotals();
        }

        // Property Changed Handlers
        // Redundant Event Handlers (Handled by ViewModel)
        private void WellboreComponents_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }

        private void WellboreComponent_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) { }

        private void DrillStringComponent_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) { }

        private void SurveyPoint_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Redundant: Handled by VM
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
                            _viewModel?.SurveyPoints.Clear();
                        }

                        // Add imported points
                        int nextId = (_viewModel?.SurveyPoints.Count ?? 0) > 0
                            ? _viewModel?.SurveyPoints.Max(p => p.Id) + 1 ?? 1
                            : 1;

                        if (result.SurveyPoints != null)
                        {
                            foreach (var point in result.SurveyPoints)
                            {
                                point.Id = nextId++;
                                point.PropertyChanged += SurveyPoint_PropertyChanged;
                                _viewModel?.SurveyPoints.Add(point);
                            }
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
                if (_viewModel == null) return;
                int index = _viewModel.WellboreComponents.IndexOf(section);
                if (index > 0) _viewModel.WellboreComponents.Move(index, index - 1);
            }
        }

        private void MoveWellboreDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellboreComponent section)
            {
                if (_viewModel == null) return;
                int index = _viewModel.WellboreComponents.IndexOf(section);
                if (index < _viewModel.WellboreComponents.Count - 1) _viewModel.WellboreComponents.Move(index, index + 1);
            }
        }


        // Survey Actions
        private void DeleteSurvey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SurveyPoint point)
            {
                _viewModel?.SurveyPoints.Remove(point);
            }
        }
        private void MoveSurveyUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SurveyPoint point)
            {
                if (_viewModel == null) return;
                int index = _viewModel.SurveyPoints.IndexOf(point);
                if (index > 0) _viewModel.SurveyPoints.Move(index, index - 1);
            }
        }
        private void MoveSurveyDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SurveyPoint point)
            {
                 if (_viewModel == null) return;
                int index = _viewModel.SurveyPoints.IndexOf(point);
                if (index < _viewModel.SurveyPoints.Count - 1) _viewModel.SurveyPoints.Move(index, index + 1);
            }
        }
        // Well Test Actions
        private void DeleteWellTest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellTest test)
            {
                _viewModel?.WellTests.Remove(test);
            }
        }
        private void MoveWellTestUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellTest test)
            {
                if (_viewModel == null) return;
                int index = _viewModel.WellTests.IndexOf(test);
                if (index > 0) _viewModel.WellTests.Move(index, index - 1);
            }
        }
        private void MoveWellTestDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellTest test)
            {
                if (_viewModel == null) return;
                int index = _viewModel.WellTests.IndexOf(test);
                if (index < _viewModel.WellTests.Count - 1) _viewModel.WellTests.Move(index, index + 1);
            }
        }

   
    }
}
