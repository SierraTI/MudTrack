using System.Windows.Controls;
using ProjectReport.Modules.VolumeBalance.Models;

namespace ProjectReport.Modules.VolumeBalance.Views
{
    public partial class VolumeBalanceView : UserControl
    {
        public VolumeBalanceView()
        {
            InitializeComponent();
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (DataContext is ViewModels.VolumeBalanceViewModel vm &&
                e.Row.Item is VolumeBalanceEvent item)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    vm.UpdateEvent(item);
                });
            }
        }
    }
}