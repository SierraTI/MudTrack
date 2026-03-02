using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Views.Inventory;
using ProjectReport.Services;
using ProjectReport.Services.Inventory; // <-- añadido

namespace ProjectReport.Views.Inventory
{
    public partial class InventoryDashboardView : UserControl
    {
        public InventoryDashboardView()
        {
            InitializeComponent();
            Loaded += InventoryDashboardView_Loaded;
        }

        private void InventoryDashboardView_Loaded(object? sender, RoutedEventArgs e)
        {
            var typeName = this.DataContext?.GetType().FullName ?? "<null>";
            var hash = this.DataContext?.GetHashCode() ?? 0;
            Debug.WriteLine($"InventoryDashboardView loaded. DataContext type: {typeName} Hash: {hash}");

            // Bind embedded history view to a VM that uses the shared InventoryService
            try
            {
                if (InventoryHistoryPanel != null)
                {
                    // Reusar la instancia global del servicio (ServiceLocator)
                    var historyVm = new ProjectReport.ViewModels.Inventory.InventoryHistoryViewModel(ServiceLocator.InventoryService);
                    InventoryHistoryPanel.DataContext = historyVm;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al inicializar InventoryHistoryPanel: " + ex);
            }

#if DEBUG
            // Sólo mostrar el MessageBox durante depuración local si realmente lo necesitas.
            // Descomenta la siguiente línea para ver el pop-up en builds DEBUG.
            // MessageBox.Show($"DEBUG: Inventory view loaded.\nDataContext: {typeName}\nHash: {hash}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
        }

        // Abre el ContextMenu definido en el bot?n Used cuando el usuario hace click.
        private void UsedButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("UsedButton_Click invoked.");
            MessageBox.Show("DEBUG: Used button clicked (InventoryDashboardView).", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);

            if (!(sender is Button btn))
            {
                Debug.WriteLine("UsedButton_Click: sender is not Button.");
                return;
            }

            Debug.WriteLine($"Button.DataContext type: {btn.DataContext?.GetType().FullName ?? "<null>"} Hash: {btn.DataContext?.GetHashCode() ?? 0}");
            MessageBox.Show($"DEBUG: Button.DataContext: {btn.DataContext?.GetType().FullName ?? "<null>"}\nHash: {btn.DataContext?.GetHashCode() ?? 0}",
                "Debug", MessageBoxButton.OK, MessageBoxImage.Information);

            if (btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.DataContext = this.DataContext;

                // Intento directo y seguro: ejecutar comando VM si existe, con manejo de excepciones.
                var vm = this.DataContext as InventoryProductsDashboardViewModel;
                try
                {
                    // Obtener la fila desde btn.DataContext
                    var row = btn.DataContext;

                    if (vm != null)
                    {
                        Debug.WriteLine($"VM Hash: {vm.GetHashCode()} - trying to execute UsedAsFluidCommand");
                        if (vm.UsedAsFluidCommand != null && vm.UsedAsFluidCommand.CanExecute(row))
                        {
                            vm.UsedAsFluidCommand.Execute(row);
                            return;
                        }
                    }

                    // Fallback: abrir di?logo directamente para probar que UI del di?logo est? OK
                    Debug.WriteLine("Fallback: opening ReportConsumedDialog directly from InventoryDashboardView.");
                    var dlg = new ReportConsumedDialog();
                    var vmc = new ProjectReport.ViewModels.Inventory.ReportConsumedViewModel(ServiceLocator.InventoryService);
                    // intentamos preseleccionar producto si la fila contiene ProductCode
                    var productProp = row?.GetType().GetProperty("ProductCode");
                    if (productProp != null)
                    {
                        var code = productProp.GetValue(row) as string;
                        var prod = ServiceLocator.InventoryService.GetProducts()
                            .FirstOrDefault(p => string.Equals(p.Code, code, System.StringComparison.OrdinalIgnoreCase));
                        if (prod != null) vmc.SelectedProduct = prod;
                    }

                    dlg.DataContext = vmc;
                    dlg.Owner = Window.GetWindow(this);
                    dlg.ShowDialog();
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine("Error opening dialog/firing command: " + ex);
                    MessageBox.Show("ERROR al ejecutar acci?n Used/abrir di?logo:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                btn.ContextMenu.IsOpen = true;
            }
        }

        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OpenInventoryHistory_Click(object sender, RoutedEventArgs e)
        {
            // Usar la instancia de servicio centralizada
            var svc = ServiceLocator.InventoryService;
            var vm = new InventoryHistoryViewModel(svc);
            var view = new ProjectReport.Views.Inventory.InventoryHistoryView { DataContext = vm };
            var win = new System.Windows.Window
            {
                Title = "Tickets",
                Content = view,
                Width = 1000,
                Height = 600
            };
            win.Show();
        }
    }
}


