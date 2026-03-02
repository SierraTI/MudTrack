using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Data;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Input;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Models.Inventory;
using ProjectReport.Services;
using System.Reflection;
using System.Windows.Controls.Primitives;
using System.Collections.Specialized;

namespace ProjectReport.Views.Inventory
{
    public partial class WholeFluidsView : UserControl
    {
        private ICollectionView? _linesView;
        private string _movementFilter = "All";
        private INotifyCollectionChanged? _linesCollection;

        public WholeFluidsView()
        {
            InitializeComponent();

            // Si el DataContext heredado no es un WholeFluidsViewModel, crear uno propio.
            // Así la vista siempre tendrá acceso a WholeFluids y DailyTotalCost.
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (!(this.DataContext is WholeFluidsViewModel))
                {
                    this.DataContext = new WholeFluidsViewModel();
                }
            }

            this.DataContextChanged += WholeFluidsView_DataContextChanged;
            InitializeLinesViewFromDataContext();
        }

        private void WholeFluidsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            InitializeLinesViewFromDataContext();
        }

        private void InitializeLinesViewFromDataContext()
        {
            try
            {
                if (_linesCollection != null)
                {
                    _linesCollection.CollectionChanged -= Lines_CollectionChanged;
                    _linesCollection = null;
                }

                if (_linesView != null && _linesView.SourceCollection is System.Collections.IEnumerable oldEnum)
                {
                    foreach (var item in oldEnum.OfType<INotifyPropertyChanged>())
                    {
                        item.PropertyChanged -= Line_PropertyChanged;
                    }
                }

                if (DataContext is WholeFluidsViewModel vm && vm.WholeFluids != null)
                {
                    _linesView = CollectionViewSource.GetDefaultView(vm.WholeFluids);
                    if (_linesView != null)
                    {
                        _linesView.Filter = FilterByMovement;
                        LinesDataGrid.ItemsSource = _linesView;
                        _linesView.Refresh();
                    }

                    _linesCollection = vm.WholeFluids as INotifyCollectionChanged;
                    if (_linesCollection != null)
                    {
                        _linesCollection.CollectionChanged -= Lines_CollectionChanged;
                        _linesCollection.CollectionChanged += Lines_CollectionChanged;
                    }

                    foreach (var line in vm.WholeFluids.OfType<INotifyPropertyChanged>())
                    {
                        line.PropertyChanged -= Line_PropertyChanged;
                        line.PropertyChanged += Line_PropertyChanged;
                    }
                }
                else
                {
                    _linesView = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[WholeFluidsView] InitializeLinesViewFromDataContext fallo: " + ex);
                _linesView = null;
            }
        }

        private void Lines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var ni in e.NewItems.OfType<INotifyPropertyChanged>())
                {
                    ni.PropertyChanged -= Line_PropertyChanged;
                    ni.PropertyChanged += Line_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (var oi in e.OldItems.OfType<INotifyPropertyChanged>())
                {
                    oi.PropertyChanged -= Line_PropertyChanged;
                }
            }

            try { _linesView?.Refresh(); } catch (Exception ex) { Debug.WriteLine(ex); }
        }

        private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(WholeFluidItem.MovementType), StringComparison.OrdinalIgnoreCase))
            {
                try { _linesView?.Refresh(); } catch (Exception ex) { Debug.WriteLine(ex); }
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not WholeFluidsViewModel vm)
            {
                Debug.WriteLine("[WholeFluidsView] DataContext no es WholeFluidsViewModel. Add abortado.");
                return;
            }

            vm.Add();
            var last = vm.WholeFluids.LastOrDefault();
            if (last != null)
            {
                LinesDataGrid.SelectedItem = last;
                LinesDataGrid.ScrollIntoView(last);

                var row = await WaitForRowAsync(last, 12, 40);
                try
                {
                    LinesDataGrid.UpdateLayout();
                    var targetColumn = LinesDataGrid.Columns.FirstOrDefault(c => (c.Header?.ToString() ?? "").Contains("Fluid")) ?? LinesDataGrid.Columns.FirstOrDefault();
                    if (targetColumn != null)
                    {
                        LinesDataGrid.CurrentCell = new DataGridCellInfo(last, targetColumn);
                        LinesDataGrid.BeginEdit();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[WholeFluidsView] BeginEdit falló: " + ex);
                }
            }
        }

        private Task<DataGridRow?> WaitForRowAsync(object item, int attempts = 10, int delayMs = 50)
        {
            return Task.Run(async () =>
            {
                for (int i = 0; i < attempts; i++)
                {
                    DataGridRow? row = null;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        row = LinesDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    }, DispatcherPriority.Normal);
                    if (row != null) return row;
                    await Task.Delay(delayMs);
                    await Application.Current.Dispatcher.InvokeAsync(() => LinesDataGrid.UpdateLayout(), DispatcherPriority.Background);
                }
                return null;
            });
        }

        // <-- AÑADIDO: Preparar editor para la columna "Fluid" (pobla ComboBox con Products)
        private void LinesDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if ((e.Column?.Header?.ToString() ?? "") != "Fluid") return;
            if (DataContext is not WholeFluidsViewModel vm) return;
            var line = e.Row.Item as WholeFluidItem;
            if (line == null) return;

            FrameworkElement editingElement = e.EditingElement as FrameworkElement;
            if (editingElement == null)
                editingElement = FindVisualChild<ContentPresenter>(e.Row);

            if (editingElement == null) return;

            var combo = FindVisualChild<ComboBox>(editingElement);
            if (combo == null) return;

            ICollectionView view;
            if (combo.Tag is ICollectionView existingView)
            {
                view = existingView;
            }
            else
            {
                view = new ListCollectionView(vm.Products);
                combo.Tag = view;
            }

            combo.ItemsSource = view;

            if (!string.IsNullOrEmpty(line.ProductCode))
            {
                var match = vm.Products.FirstOrDefault(p => string.Equals(p.Code, line.ProductCode, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    combo.SelectedItem = match;
                    combo.Text = match.Name;
                }
                else
                {
                    combo.SelectedIndex = -1;
                    combo.Text = line.ProductName ?? string.Empty;
                }
            }
            else
            {
                combo.SelectedIndex = -1;
                combo.Text = line.ProductName ?? string.Empty;
            }

            combo.SelectionChanged -= Combo_SelectionChanged;
            combo.SelectionChanged += Combo_SelectionChanged;

            var tb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
            if (tb != null)
            {
                tb.TextChanged -= ComboEditableTextBox_TextChanged;
                tb.TextChanged += ComboEditableTextBox_TextChanged;

                combo.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tb.Focus();
                    tb.CaretIndex = tb.Text?.Length ?? 0;
                }), DispatcherPriority.Input);
            }

            combo.IsDropDownOpen = true;
        }

        // <-- AÑADIDO: doble clic para comenzar edición de celda
        private void LinesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridCell cell)
            {
                var item = cell.DataContext;
                LinesDataGrid.SelectedItem = item;
                LinesDataGrid.CurrentCell = new DataGridCellInfo(item, cell.Column);

                try
                {
                    LinesDataGrid.BeginEdit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[WholeFluidsView] BeginEdit (double-click) falló: " + ex);
                }
            }
        }

        // <-- AÑADIDO: manejador de SelectionChanged (vacío, evita error de compilación)
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // placeholder: se puede usar para habilitar botones de contexto
        }

        private void MovementFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                var sel = cb.SelectedValue?.ToString()?.Trim() ?? "All";
                _movementFilter = sel;
                try
                {
                    if (LinesDataGrid.CommitEdit(DataGridEditingUnit.Row, true))
                        LinesDataGrid.CommitEdit();
                }
                catch (Exception ex) { Debug.WriteLine(ex); }

                try { _linesView?.Refresh(); } catch (Exception ex) { Debug.WriteLine(ex); }
            }
        }

        private bool FilterByMovement(object obj)
        {
            if (obj == null) return false;
            if (string.IsNullOrWhiteSpace(_movementFilter) || _movementFilter.Equals("All", StringComparison.OrdinalIgnoreCase)) return true;

            if (obj is WholeFluidItem line)
            {
                var mt = (line.MovementType ?? string.Empty).Trim();
                return string.Equals(mt, _movementFilter.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private void RowEdit_Click(object sender, RoutedEventArgs e)
        {
            // idem al original: abrir edición en columna Fluid
            var btn = sender as Button;
            var line = btn?.Tag as WholeFluidItem ?? btn?.DataContext as WholeFluidItem;
            if (line == null) return;

            LinesDataGrid.SelectedItem = line;
            LinesDataGrid.ScrollIntoView(line);

            _ = Task.Run(async () =>
            {
                var row = await WaitForRowAsync(line, 8, 40);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        LinesDataGrid.UpdateLayout();
                        var targetColumn = LinesDataGrid.Columns.FirstOrDefault(c => (c.Header?.ToString() ?? "").Contains("Fluid")) ?? LinesDataGrid.Columns.FirstOrDefault();
                        if (targetColumn != null)
                        {
                            LinesDataGrid.CurrentCell = new DataGridCellInfo(line, targetColumn);
                            LinesDataGrid.BeginEdit();
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine(ex); }
                });
            });
        }

        private void RowDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var line = btn?.Tag as WholeFluidItem ?? btn?.DataContext as WholeFluidItem;
            if (line == null) return;

            if (DataContext is not WholeFluidsViewModel vm) return;
            vm.Remove(line);
        }

        // Resto helpers (FindVisualChild/Parent y Combo handlers) se mantienen sin cambios
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null) return null;
            DependencyObject? parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typed) return typed;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void ComboEditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var combo = FindVisualParent<ComboBox>(tb);
            if (combo == null) return;
            if (!(combo.Tag is ICollectionView view)) return;

            var text = (tb.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text)) view.Filter = null;
            else
            {
                var q = text.ToUpperInvariant();
                view.Filter = o =>
                {
                    if (o == null) return false;
                    var name = GetPropertyValue(o, "Name") as string ?? string.Empty;
                    var code = GetPropertyValue(o, "Code") as string ?? string.Empty;
                    var label = name.ToUpperInvariant();
                    return label.Contains(q) || code.ToUpperInvariant().Contains(q);
                };
            }

            view.Refresh();
            combo.IsDropDownOpen = true;
        }

        private void Combo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo) return;
            var prod = combo.SelectedItem;
            if (prod == null) return;

            WholeFluidItem? line = null;
            if (combo.DataContext is WholeFluidItem direct) line = direct;
            else
            {
                var row = FindVisualParent<DataGridRow>(combo);
                if (row != null && row.DataContext is WholeFluidItem rowLine) line = rowLine;
            }

            if (line != null)
            {
                var code = GetPropertyValue(prod, "Code") as string ?? string.Empty;
                var name = GetPropertyValue(prod, "Name") as string ?? string.Empty;
                line.ProductCode = code;
                line.ProductName = name;
            }

            combo.IsDropDownOpen = false;
            combo.Dispatcher.BeginInvoke(new Action(() =>
            {
                var tb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
                tb?.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
            }), DispatcherPriority.Input);
        }

        private static object? GetPropertyValue(object obj, string propName)
        {
            if (obj == null) return null;
            try
            {
                var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                return p?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }
    }
}
