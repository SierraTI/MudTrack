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

            // Si tu host inyecta VM, comenta/ajusta esta línea para no sobrescribir
            var service = ServiceLocator.InventoryService;
            var vm = new TicketReceivedViewModel(service);
            DataContext = vm;
        }

        // Handlers para eventos definidos en XAML
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateResponsiveLayout(ActualWidth);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        }

        private void UpdateResponsiveLayout(double width)
        {
            if (TopSectionGrid == null) return;

            if (width < NarrowThreshold)
            {
                // Apilar verticalmente
                TopSectionGrid.ColumnDefinitions.Clear();
                TopSectionGrid.RowDefinitions.Clear();

                TopSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                TopSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                TopSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetRow(RequisitionPanel, 0);
                Grid.SetColumn(RequisitionPanel, 0);

                Grid.SetRow(OriginPanel, 1);
                Grid.SetColumn(OriginPanel, 0);

                Grid.SetRow(AddButtonPanel, 2);
                Grid.SetColumn(AddButtonPanel, 0);
            }
            else
            {
                // Disposición en 3 columnas
                TopSectionGrid.RowDefinitions.Clear();
                TopSectionGrid.ColumnDefinitions.Clear();

                TopSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                TopSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                TopSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Grid.SetColumn(RequisitionPanel, 0);
                Grid.SetRow(RequisitionPanel, 0);

                Grid.SetColumn(OriginPanel, 1);
                Grid.SetRow(OriginPanel, 0);

                Grid.SetColumn(AddButtonPanel, 2);
                Grid.SetRow(AddButtonPanel, 0);
            }
        }

        // Se llama cuando una celda entra en modo edición.
        private void LinesDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            // Solo interesa columna Product
            if (e.Column.Header?.ToString() != "Product") return;

            var vm = DataContext as TicketReceivedViewModel;
            if (vm == null) return;

            // Obtener la línea (TicketLine) editada
            var line = e.Row.Item as TicketLine;

            // Buscar ComboBox dentro del editor
            FrameworkElement editingElement = e.EditingElement as FrameworkElement;
            if (editingElement == null)
                editingElement = FindVisualChild<ContentPresenter>(e.Row);

            if (editingElement == null) return;

            var combo = FindVisualChild<ComboBox>(editingElement);
            if (combo == null) return;

            // Crear o reutilizar ListCollectionView asociada a este combo
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

            // Si la línea ya tiene producto, seleccionar el objeto Product correspondiente
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
                    // Asegurar que el SelectedValue/Text reflejen la selección
                    combo.Text = match.SearchLabel;
                }
                else
                {
                    // si no hay match, dejar el texto actual (posible texto libre)
                    combo.SelectedIndex = -1;
                    combo.Text = name;
                }
            }

            // Suscribir eventos (asegurando no duplicar)
            combo.SelectionChanged -= Combo_SelectionChanged;
            combo.SelectionChanged += Combo_SelectionChanged;

            var tb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
            if (tb != null)
            {
                tb.TextChanged -= ComboEditableTextBox_TextChanged;
                tb.TextChanged += ComboEditableTextBox_TextChanged;

                // foco y caret al final
                combo.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    tb.Focus();
                    tb.CaretIndex = tb.Text?.Length ?? 0;
                }));
            }

            combo.IsDropDownOpen = true;
        }

        // Handler que filtra la ListCollectionView asociada al ComboBox
        private void ComboEditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            // ubicar ComboBox ancestro
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

        // Nuevo: cuando se selecciona un item en el ComboBox asignamos ProductName/ProductCode a la TicketLine
        private void Combo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo) return;

            // Si no hay selección, nada que hacer
            if (combo.SelectedItem is not Product prod)
            {
                // Si el usuario anuló la selección por escribir, mantener el texto
                return;
            }

            // Intentar obtener la TicketLine asociada a la fila
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
                // Asignar código/nombre del producto seleccionado a la línea
                line.ProductCode = prod.Code;
                line.ProductName = prod.Name;
            }

            // Cerrar dropdown y mover foco fuera para seguir edición
            combo.IsDropDownOpen = false;
            combo.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                var tb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
                if (tb != null)
                {
                    // mover foco a siguiente control dentro de la fila
                    tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }));
        }

        // Helpers para recorrer el árbol visual
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
