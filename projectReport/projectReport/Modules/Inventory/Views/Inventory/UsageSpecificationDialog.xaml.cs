using System.Windows;
using ProjectReport.ViewModels.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class UsageSpecificationDialog : Window
    {
        public UsageSpecificationDialog(UsageSpecificationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsageSpecificationViewModel viewModel)
            {
                try
                {
                    viewModel.Save();
                    DialogResult = true;
                    Close();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "Invalid Usage", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
