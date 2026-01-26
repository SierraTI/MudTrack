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

            // Si no hay DataContext (y no estamos en tiempo de diseño) creamos una instancia ligera
            // para poder probar la vista. En producción inyecta el InventoryService compartido.
            if (!DesignerProperties.GetIsInDesignMode(this) && DataContext == null)
            {
                var repo = new JsonInventoryRepository();
                var svc = new InventoryService(repo);
                DataContext = new InventoryHistoryViewModel(svc);
            }
        }

        private void OpenInventoryHistory_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir el historial de inventario
        }
    }
}
