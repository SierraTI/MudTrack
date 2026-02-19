using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
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

        /// <summary>
        /// Handles clicking on a ticket number hyperlink to view the original ticket
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Extract ticket ID from the hyperlink
            if (sender is System.Windows.Documents.Hyperlink hyperlink && hyperlink.NavigateUri != null)
            {
                string ticketId = hyperlink.NavigateUri.ToString();
                // TODO: Implement navigation to open the original ticket in a modal/dialog
                // For now, just prevent the default browser navigation
                e.Handled = true;
            }
        }
    }
}
