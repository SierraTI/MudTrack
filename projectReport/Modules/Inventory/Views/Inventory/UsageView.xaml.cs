using System;
using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class UsageView : UserControl
    {
        public UsageView()
        {
            InitializeComponent();
            var service = ProjectReport.Services.ServiceLocator.InventoryService;
            DataContext = new UsageViewModel(service);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsageViewModel viewModel)
            {
                viewModel.AddNewUsageItem();
            }
        }
    }
}
