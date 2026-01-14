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

            // Si no hay DataContext, inyecta VM (comportamiento existente)
            if (DataContext == null)
            {
                var service = ProjectReport.Services.ServiceLocator.InventoryService;
                DataContext = new TicketReturnedViewModel(service);
            }
        }

        // Cuando el usuario selecciona un producto en la fila, actualizar la línea (nombre, precio)
        // (Este método ya existía; lo mantenemos para aplicar precio/contexto)
        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is TicketLine line)
            {
                // Si SelectedItem es Product, copiar datos
                if (combo.SelectedItem is Product prod)
                {
                    line.ProductCode = prod.Code ?? string.Empty;
                    line.ProductName = prod.Name ?? string.Empty;

                    // Intentar obtener el precio desde el último Received (preferir la misma remisión si la VM la tiene)
                    double priceToUse = 0;

                    // Obtener VM para revisar Requisition si existe
                    if (this.DataContext is TicketReturnedViewModel vm)
                    {
                        var requisition = vm.Requisition ?? string.Empty;

                        // Buscar movimientos Received en el servicio
                        var svc = ServiceLocator.InventoryService;
                        var lastReceived = svc.GetMovements()
                            .Where(m => string.Equals(m.ProductCode, prod.Code, StringComparison.OrdinalIgnoreCase)
                                        && m.Type == TicketType.Received
                                        && m.UnitPrice > 0)
                            // si hay requisition buscar primero por la misma remisión
                            .OrderByDescending(m => m.Date)
                            .ToList();

                        // Priorizar same requisition if provided
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

                    // Fallback a precio del producto si no encontramos Received con precio
                    if (priceToUse <= 0)
                        priceToUse = prod.CurrentUnitCost;

                    line.UnitPrice = priceToUse;

                    // Si Context vacío rellenar con Origin del VM
                    if (string.IsNullOrWhiteSpace(line.Context) && this.DataContext is TicketReturnedViewModel vm2)
                    {
                        line.Context = vm2.Origin ?? string.Empty;
                    }
                }
            }
        }

        // Se llama cuando una celda entra en modo edición.
        private void LinesDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            // Solo interesa columna Product (mismo header)
            if (e.Column.Header?.ToString() != "Product") return;

            var vm = DataContext as TicketReturnedViewModel;
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
                    combo.Text = match.SearchLabel;
                }
                else
                {
                    combo.SelectedIndex = -1;
                    combo.Text = name;
                }
            }

            // Suscribir eventos (asegurando no duplicar)
            combo.SelectionChanged -= ProductCombo_SelectionChanged;
            combo.SelectionChanged += ProductCombo_SelectionChanged;

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

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Commit de edición pendiente (celda/fila) para que los datos estén sincronizados
            try
            {
                LinesDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                LinesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch
            {
                // Ignorar errores de commit y continuar con la eliminación
            }

            if (sender is not Button btn) return;

            // Intentar obtener la TicketLine desde CommandParameter o DataContext del botón
            var line = btn.CommandParameter as TicketLine ?? btn.DataContext as TicketLine;
            if (line == null) return;

            if (DataContext is TicketReturnedViewModel vm && vm.RemoveLineCommand != null && vm.RemoveLineCommand.CanExecute(line))
            {
                vm.RemoveLineCommand.Execute(line);
            }
            else if (DataContext is TicketReturnedViewModel vm2)
            {
                // Fallback: eliminar directamente de la colección
                vm2.Lines.Remove(line);
            }
        }
    }
}
