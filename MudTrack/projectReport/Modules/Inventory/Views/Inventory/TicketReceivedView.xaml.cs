using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Services;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class TicketReceivedView : UserControl
    {
        private const double NarrowThreshold = 620.0;

        public TicketReceivedView()
        {
            InitializeComponent();

            var service = ServiceLocator.InventoryService;
            var vm = new TicketReceivedViewModel(service);
            DataContext = vm;
        }


        // Handlers para eventos definidos en XAML
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        // Se llama cuando una celda entra en modo edición.
        private void LinesDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.Column.Header?.ToString() != "Product") return;

            var vm = DataContext as TicketReceivedViewModel;
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

            combo.SelectionChanged -= Combo_SelectionChanged;
            combo.SelectionChanged += Combo_SelectionChanged;

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

        private void Combo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo) return;

            if (combo.SelectedItem is not Product prod) return;

            TicketLine? line = null;
            if (combo.DataContext is TicketLine directLine)
            {
                line = directLine;
            }
            else
            {
                var row = FindVisualParent<DataGridRow>(combo);
                if (row != null && row.DataContext is TicketLine rowLine)
                    line = rowLine;
            }

            if (line != null)
            {
                line.ProductCode = prod.Code;
                line.ProductName = prod.Name;
                line.Unit = string.IsNullOrWhiteSpace(prod.Unit) ? "Each" : prod.Unit;
                line.UnitPrice = prod.CurrentUnitCost;
            }

            combo.IsDropDownOpen = false;
            combo.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                var tb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
                if (tb != null)
                {
                    tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }));
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
    }
}
