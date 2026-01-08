using System.Windows;
using System.Windows.Controls;
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
    }
}
