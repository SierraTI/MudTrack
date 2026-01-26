using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using ProjectReport.Modules.RigProfile.ViewModels;
using ProjectReport.Models.Rig;
using System.ComponentModel;

namespace ProjectReport.Modules.RigProfile.Views
{
    public partial class RigProfileView : UserControl
    {
        public RigProfileView()
        {
            InitializeComponent();
            DataContext = new RigProfileViewModel();
        }

        private void ModelComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is RigSolidsControl item)
            {
                var vm = DataContext as RigProfileViewModel;
                if (vm != null)
                {
                    // Update available models based on selected type and manufacturer
                    vm.UpdateAvailableModels(item.Type, item.Manufacturer);
                    
                    // Auto-fill capacity if model matches catalog
                    vm.UpdateSolidsControlSpecs(item);
                }
            }
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is RigSolidsControl item)
            {
                var vm = DataContext as RigProfileViewModel;
                if (vm != null && !string.IsNullOrEmpty(item.Type))
                {
                    vm.FilterManufacturers(item.Type);
                    
                    if (!string.IsNullOrEmpty(item.Manufacturer))
                    {
                        vm.UpdateAvailableModels(item.Type, item.Manufacturer);
                    }
                }
            }
        }

        private void ManufacturerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is RigSolidsControl item)
            {
                var vm = DataContext as RigProfileViewModel;
                if (vm != null && !string.IsNullOrEmpty(item.Type) && !string.IsNullOrEmpty(item.Manufacturer))
                {
                    vm.UpdateAvailableModels(item.Type, item.Manufacturer);
                }
            }
        }

        // Inicia edición en la fila seleccionada (invocado por el botón Edit en la fila)
        private void EditSurface_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && SurfaceDataGrid != null)
            {
                var item = btn.DataContext;
                if (item == null) return;

                SurfaceDataGrid.SelectedItem = item;
                SurfaceDataGrid.ScrollIntoView(item);

                // Seleccionar la celda ID (columna index 2 en la vista actual) para empezar edición
                if (SurfaceDataGrid.Columns.Count > 2)
                {
                    var cellInfo = new DataGridCellInfo(item, SurfaceDataGrid.Columns[2]);
                    SurfaceDataGrid.CurrentCell = cellInfo;
                }

                SurfaceDataGrid.BeginEdit();
            }
        }

        // Restringe entrada a dígitos y separador decimal según la cultura
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var text = e.Text;
            var decimalSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c.ToString() != decimalSep)
                {
                    e.Handled = true;
                    return;
                }
            }

            if (sender is TextBox tb && text.Contains(decimalSep))
            {
                if (tb.Text.Contains(decimalSep))
                {
                    e.Handled = true;
                }
            }
        }
    }
}
