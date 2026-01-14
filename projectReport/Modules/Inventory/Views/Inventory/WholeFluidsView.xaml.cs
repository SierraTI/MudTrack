using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Services;

namespace ProjectReport.Views.Inventory
{
    public partial class WholeFluidsView : UserControl
    {
        public WholeFluidsView()
        {
            InitializeComponent();

            // Si el host no ha inyectado DataContext, creamos el VM localmente
            // igual que hace TicketReceivedView.xaml.cs
            if (DataContext == null)
            {
                var service = ServiceLocator.InventoryService;
                var vm = new TicketReceivedViewModel(service);
                DataContext = vm;
                Debug.WriteLine("[WholeFluidsView] DataContext inicializado localmente con TicketReceivedViewModel");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Intentar obtener el VM esperado
            if (DataContext is not TicketReceivedViewModel vm)
            {
                Debug.WriteLine("[WholeFluidsView] DataContext no es TicketReceivedViewModel. Add abortado.");
                return;
            }

            // Ejecutar el comando público que agrega la línea al borrador
            if (vm.AddLineCommand != null && vm.AddLineCommand.CanExecute(null))
            {
                vm.AddLineCommand.Execute(null);
                Debug.WriteLine($"[WholeFluidsView] AddLineCommand ejecutado. Lines.Count = {vm.Lines?.Count}");
            }
            else
            {
                // Fallback: llamar directamente al método interno si el comando no existe
                try
                {
                    var addMethod = vm.GetType().GetMethod("AddLine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    addMethod?.Invoke(vm, null);
                    Debug.WriteLine("[WholeFluidsView] AddLine (reflection) invocado como fallback.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[WholeFluidsView] Error al invocar AddLine por fallback: " + ex);
                }
            }

            // Si la colección cambió, seleccionar y scrollear a la última línea
            if (vm.Lines != null && vm.Lines.Any())
            {
                var last = vm.Lines.Last();
                LinesDataGrid.SelectedItem = last;
                LinesDataGrid.ScrollIntoView(last);

                // Opcional: abrir la fila en edición para completar campos faltantes
                LinesDataGrid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LinesDataGrid.UpdateLayout();
                    var row = LinesDataGrid.ItemContainerGenerator.ContainerFromItem(last) as DataGridRow;
                    if (row != null)
                    {
                        LinesDataGrid.SelectedItem = last;
                        LinesDataGrid.CurrentCell = new DataGridCellInfo(last, LinesDataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Fluido") ?? LinesDataGrid.Columns[0]);
                        LinesDataGrid.BeginEdit();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        // Handler requerido por el XAML (SelectionChanged="DataGrid_SelectionChanged")
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Si necesitas manejar la selección coloca aquí la lógica.
        }
    }
}