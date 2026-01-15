using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Models.Inventory;
using System.Diagnostics;

namespace ProjectReport.Views.Inventory
{
    public partial class AdditionalChargeView : UserControl
    {
        public AdditionalChargeView()
        {
            InitializeComponent();

            Debug.WriteLine("[AdditionalChargeView] constructor called"); // <-- traza
            if (DataContext == null)
            {
                DataContext = new AdditionalChargeViewModel();
            }
        }

        private void RowDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.Tag as AdditionalChargeItem;
            if (item == null) return;

            if (DataContext is AdditionalChargeViewModel vm)
            {
                vm.Remove(item);
            }
        }
    }
}