using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Services;

namespace ProjectReport.Views.Inventory
{
    public partial class UsageView : UserControl
    {
        public UsageView()
        {
            InitializeComponent();

            var service = ServiceLocator.InventoryService;
            var viewModel = new UsageViewModel(service);
            
            // Subscribe to specification dialog requests
            viewModel.RequestUsageSpecification = item =>
            {
                var specVm = new UsageSpecificationViewModel(service, item);
                var dialog = new UsageSpecificationDialog(specVm);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            };

            DataContext = viewModel;
        }
    }
}
