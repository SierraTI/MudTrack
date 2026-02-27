using System;
using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Services.Inventory;
using System.IO;

namespace ProjectReport.Views.Inventory
{
    public partial class UsageView : UserControl
    {
        public UsageView()
        {
            InitializeComponent();
            
            var dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProjectReport"
            );
            var service = new InventoryService(new JsonInventoryRepository(dataPath));
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
