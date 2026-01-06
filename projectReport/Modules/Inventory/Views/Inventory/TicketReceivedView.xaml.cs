using System.Windows;
using System.Windows.Controls;
using ProjectReport.Services;
using ProjectReport.ViewModels.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class TicketReceivedView : UserControl
    {
        public TicketReceivedView()
        {
            InitializeComponent();

            // Create ViewModel (or use DI)
            var service = ServiceLocator.InventoryService;
            var vm = new TicketReceivedViewModel(service);

            DataContext = vm;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TicketReceivedViewModel vm)
            {
                // Ensure products are loaded from Data\Lista.xlsx and filtered
                vm.RefreshCommand.Execute(null);
            }
        }
    }
}
