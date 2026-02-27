using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ProjectReport.Services.Inventory;
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
                var repo = new JsonInventoryRepository();
                var svc  = new InventoryService(repo);
                DataContext = new InventoryHistoryViewModel(svc);
            }
        }
    }
}
