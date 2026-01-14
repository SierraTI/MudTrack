using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ProjectReport.ViewModels.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class WholeFluidsView : UserControl
    {
        public WholeFluidsView()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // opcional: manejo de selección si hace falta
        }

        // Ejecuta el comando del ViewModel, selecciona la última fila y entra en edición en la columna "Fluido"
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not WholeFluidsViewModel vm) return;

            if (!vm.AddLineCommand.CanExecute(null)) return;
            vm.AddLineCommand.Execute(null);

            await Dispatcher.InvokeAsync(() =>
            {
                if (LinesDataGrid.Items.Count == 0) return;
                var item = LinesDataGrid.Items[LinesDataGrid.Items.Count - 1];
                LinesDataGrid.SelectedItem = item;
                LinesDataGrid.ScrollIntoView(item);
                LinesDataGrid.UpdateLayout();

                var row = (DataGridRow)LinesDataGrid.ItemContainerGenerator.ContainerFromItem(item);
                if (row == null)
                {
                    LinesDataGrid.UpdateLayout();
                    row = (DataGridRow)LinesDataGrid.ItemContainerGenerator.ContainerFromItem(item);
                }

                if (row != null)
                {
                    var fluidoColumn = LinesDataGrid.Columns.FirstOrDefault(c => (c.Header?.ToString() ?? "").Equals("Fluido", StringComparison.OrdinalIgnoreCase))
                                      ?? LinesDataGrid.Columns.First();

                    LinesDataGrid.CurrentCell = new DataGridCellInfo(item, fluidoColumn);
                    LinesDataGrid.BeginEdit();

                    var cell = fluidoColumn.GetCellContent(item)?.Parent as DataGridCell;
                    cell?.Focus();
                }
            }, DispatcherPriority.Background);
        }
    }
}