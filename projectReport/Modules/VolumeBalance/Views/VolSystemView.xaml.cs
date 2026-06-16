using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectReport.Modules.VolumeBalance.Views
{
    /// <summary>
    /// Lógica de interacción para VolSystemView.xaml
    /// </summary>
    public partial class VolSystemView : UserControl
    {
        public VolSystemView()
        {
            InitializeComponent();
            Loaded += VolSystemView_Loaded;
            Unloaded += VolSystemView_Unloaded;
            DataContextChanged += VolSystemView_DataContextChanged;
            SizeChanged += VolSystemView_SizeChanged;
        }

        private void FluidCombo_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is ProjectReport.Modules.VolumeBalance.Models.VolSystemPit vsPit)
                {
                    if (DataContext is ProjectReport.Modules.VolumeBalance.ViewModels.VolSystemViewModel vm)
                    {
                        // Get options from VM for the well (includes WellFluids + master list)
                        var options = vm.GetFluidOptionsForPit(vsPit.SourcePit) ?? new List<string>();
                        // If no options were returned, but the pit already has a FluidType (from WellInfo), ensure it's shown
                        if (options.Count == 0 && !string.IsNullOrEmpty(vsPit.FluidType))
                        {
                            options.Add(vsPit.FluidType);
                        }

                        cb.ItemsSource = options;

                        // select current fluid if present
                        if (!string.IsNullOrEmpty(vsPit.FluidType) && options.Contains(vsPit.FluidType))
                            cb.SelectedItem = vsPit.FluidType;
                        else if (options.Count > 0)
                            cb.SelectedItem = options[0];
                    }
                }
            }
            catch { }
        }

        private void VolSystemView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProjectReport.Modules.VolumeBalance.ViewModels.VolSystemViewModel vm)
            {
                vm.Refresh();
            }
        }

        private void VolSystemView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ProjectReport.Modules.VolumeBalance.ViewModels.VolSystemViewModel vm)
            {
                // Ensure viewmodel refreshes when assigned
                vm.Refresh();
            }
        }

        private void VolSystemView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is IDisposable d)
            {
                d.Dispose();
            }
        }

        private void VolSystemView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                double width = e.NewSize.Width;
                double scale = 1.0;
                if (width < 560) scale = 0.78;
                else if (width < 720) scale = 0.88;
                else if (width < 900) scale = 0.96;
                else scale = 1.0;

                // Apply ScaleTransform to DataGrid to shrink contents proportionally
                if (FindName("GridScale") is ScaleTransform st)
                {
                    st.ScaleX = scale;
                    st.ScaleY = scale;
                }
            }
            catch { }
        }
    }
}
