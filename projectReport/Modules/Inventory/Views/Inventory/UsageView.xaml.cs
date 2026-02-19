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
            DataContext = new UsageViewModel();
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
