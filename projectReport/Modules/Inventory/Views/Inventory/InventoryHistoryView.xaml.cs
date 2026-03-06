using System.ComponentModel;
using System.Windows.Controls;
using ProjectReport.Services;
using ProjectReport.ViewModels.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class InventoryHistoryView : UserControl
    {
        public InventoryHistoryView()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this) && DataContext == null)
            {
                DataContext = new InventoryHistoryViewModel(ServiceLocator.InventoryService);
            }
        }
    }
}
