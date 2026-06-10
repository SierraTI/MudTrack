using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Media;
using ProjectReport.Models.Inventory;
using ProjectReport.Models;
using ProjectReport.Services;
using ProjectReport.ViewModels.Inventory;
using System.Windows.Controls.Primitives;

namespace ProjectReport.Views.Inventory
{
    public partial class TicketReturnedView : UserControl
    {
        public TicketReturnedView()
        {
            InitializeComponent();

            if (DataContext == null)
            {
                var service = ProjectReport.Services.ServiceLocator.InventoryService;
                var vm = new TicketReturnedViewModel(service);
                DataContext = vm;
            }
        }


        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is TicketLine line)
            {
                if (combo.SelectedItem is Product prod)
                {
                    line.ProductCode = prod.Code ?? string.Empty;
                    line.ProductName = prod.Name ?? string.Empty;
                    line.Unit = string.IsNullOrWhiteSpace(prod.Unit) ? "Each" : prod.Unit;

                    double priceToUse = 0;

                    if (this.DataContext is TicketReturnedViewModel vm)
                    {
                        var requisition = vm.Requisition ?? string.Empty;
                        var svc = ServiceLocator.InventoryService;
                        var lastReceived = svc.GetMovements()
                            .Where(m => string.Equals(m.ProductCode, prod.Code, StringComparison.OrdinalIgnoreCase)
                                        && m.Type == TicketType.Received
                                        && m.UnitPrice > 0)
                            .OrderByDescending(m => m.Date)
                            .ToList();

                        InventoryMovement? match = null;
                        if (!string.IsNullOrWhiteSpace(requisition))
                        {
                            match = lastReceived.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Requisition) &&
                                                                     string.Equals(m.Requisition, requisition, StringComparison.OrdinalIgnoreCase));
                        }

                        if (match == null)
                            match = lastReceived.FirstOrDefault();

                        if (match != null && match.UnitPrice > 0)
                            priceToUse = match.UnitPrice;
                    }

                    if (priceToUse <= 0)
                        priceToUse = prod.CurrentUnitCost;

                    line.UnitPrice = priceToUse;

                    if (string.IsNullOrWhiteSpace(line.Context) && this.DataContext is TicketReturnedViewModel vm2)
                    {
                        line.Context = vm2.Destination ?? string.Empty;
                    }
                }
            }
        }

        private void LinesDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.Column.Header?.ToString() != "Product") return;

            var vm = DataContext as TicketReturnedViewModel;
            if (vm == null) return;

            var line = e.Row.Item as TicketLine;

            FrameworkElement? editingElement = e.EditingElement as FrameworkElement;
            if (editingElement == null)
                editingElement = FindVisualChild<ContentPresenter>(e.Row);

            if (editingElement == null) return;

            var combo = FindVisualChild<ComboBox>(editingElement);
            if (combo == null) return;

            ListCollectionView view;
            if (combo.Tag is ListCollectionView existingView)
            {
                view = existingView;
            }
            else
            {
                view = new ListCollectionView(vm.Products);
                combo.Tag = view;
            }

            combo.ItemsSource = view;

            if (line != null)
            {
                Product? match = null;
                var code = (line.ProductCode ?? "").Trim();
                var name = (line.ProductName ?? "").Trim();

                if (!string.IsNullOrEmpty(code))
                    match = vm.Products.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));

                if (match == null && !string.IsNullOrEmpty(name))
                    match = vm.Products.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    combo.SelectedItem = match;
                    combo.Text = match.SearchLabel;
                }
                else
                {
                    combo.SelectedIndex = -1;
                    combo.Text = name;
                }
            }

            combo.SelectionChanged -= ProductCombo_SelectionChanged;
            combo.SelectionChanged += ProductCombo_SelectionChanged;

            var tb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
            if (tb != null)
            {
                tb.TextChanged -= ComboEditableTextBox_TextChanged;
                tb.TextChanged += ComboEditableTextBox_TextChanged;

                combo.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    tb.Focus();
                    tb.CaretIndex = tb.Text?.Length ?? 0;
                }));
            }

            combo.IsDropDownOpen = true;
        }

        private void ComboEditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var combo = FindVisualParent<ComboBox>(tb);
            if (combo == null) return;

            if (!(combo.Tag is ListCollectionView view)) return;

            var text = (tb.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text))
            {
                view.Filter = null;
            }
            else
            {
                var q = text.ToUpperInvariant();
                view.Filter = o =>
                {
                    if (o is not Product p) return false;
                    var label = (p.SearchLabel ?? string.Empty).ToUpperInvariant();
                    return label.Contains(q);
                };
            }

            view.Refresh();
            combo.IsDropDownOpen = true;
        }

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

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LinesDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                LinesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }

            if (sender is not Button btn) return;
            var line = btn.CommandParameter as TicketLine ?? btn.DataContext as TicketLine;
            if (line == null) return;

            if (DataContext is TicketReturnedViewModel vm && vm.RemoveLineCommand != null && vm.RemoveLineCommand.CanExecute(line))
            {
                vm.RemoveLineCommand.Execute(line);
            }
            else if (DataContext is TicketReturnedViewModel vm2)
            {
                vm2.Lines.Remove(line);
            }
        }

        // Monitor quantity changes to validate return limits
        private void LinesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header?.ToString() != "Qty Return") return;

            try
            {
                var line = e.Row.Item as TicketLine;
                if (line != null && DataContext is TicketReturnedViewModel vm)
                {
                    if (e.EditingElement is TextBox tb)
                    {
                        var raw = (tb.Text ?? string.Empty).Trim();
                        if (double.TryParse(raw, out var editedQty))
                        {
                            if (editedQty < 0)
                            {
                                line.Quantity = 0;
                                vm.Error = "Qty Return cannot be negative.";
                            }
                            else if (editedQty > line.CurrentStock)
                            {
                                // Do not auto-correct silently; keep user value and force save validation to block.
                                line.Quantity = editedQty;
                                vm.Error = $"Qty Return ({editedQty}) cannot be greater than Current Stock ({line.CurrentStock}).";
                            }
                            else
                            {
                                line.Quantity = editedQty;
                                vm.Error = string.Empty;
                            }
                        }
                    }

                    // Call validation through reflection to trigger ValidateReturnQuantity
                    var method = vm.GetType().GetMethod("ValidateReturnQuantity", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(vm, new object[] { line });
                }
            }
            catch { }
        }
    }
}
