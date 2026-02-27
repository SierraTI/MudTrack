using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProjectReport.Models.Rig;
using ProjectReport.Modules.RigProfile.ViewModels;

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
                if (DataContext is RigProfileViewModel vm)
                {
                    vm.ApplySolidControlModelSelection(item);
                }
            }
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is RigSolidsControl item)
            {
                if (DataContext is RigProfileViewModel vm)
                {
                    vm.ApplySolidControlStyleSelection(item);
                }
            }
        }

        private void ManufacturerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is RigSolidsControl item)
            {
                if (DataContext is RigProfileViewModel vm)
                {
                    vm.ApplySolidControlManufacturerSelection(item);
                }
            }
        }

        private void StyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is RigSolidsControl item)
            {
                if (DataContext is RigProfileViewModel vm)
                {
                    vm.ApplySolidControlStyleSelection(item);
                }
            }
        }

        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is RigSolidsControl item)
            {
                if (DataContext is RigProfileViewModel vm)
                {
                    vm.ApplySolidControlModelSelection(item);
                }
            }
        }

        // Starts editing the selected surface row.
        private void EditSurface_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && SurfaceDataGrid != null)
            {
                var item = btn.DataContext;
                if (item == null) return;

                SurfaceDataGrid.SelectedItem = item;
                SurfaceDataGrid.ScrollIntoView(item);

                if (SurfaceDataGrid.Columns.Count > 2)
                {
                    var cellInfo = new DataGridCellInfo(item, SurfaceDataGrid.Columns[2]);
                    SurfaceDataGrid.CurrentCell = cellInfo;
                }

                SurfaceDataGrid.BeginEdit();
            }
        }

        // Restricts input to digits and one decimal separator.
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
