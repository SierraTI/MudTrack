using ProjectReport.Modules.VolumeBalance.ViewModels;
using System.Windows.Controls;

namespace ProjectReport.Modules.VolumeBalance.Views
{
    public partial class VolumeInfoTableView : UserControl
    {
        private readonly VolumeInfoTableViewModel _viewModel;

        public VolumeInfoTableView()
        {
            InitializeComponent();

            _viewModel =
                new VolumeInfoTableViewModel();

            DataContext =
                _viewModel;
        }

        // ============================================================
        // EVENT ID
        // ============================================================

        public void SetEventId(int eventId)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[VolumeInfoTableView] SetEventId = {eventId}");

            _viewModel.VolumeBalanceEventId =
                eventId;
        }

        // ============================================================
        // CONECTAR CON VOL SYSTEM
        // ============================================================

        public void AttachVolumeSystemViewModel(
            VolSystemViewModel volSystemViewModel)
        {
            if (volSystemViewModel == null)
                return;

            System.Diagnostics.Debug.WriteLine(
                "[VolumeInfoTableView] " +
                "Conectando VolSystemViewModel");

            _viewModel.AttachVolumeSystemViewModel(
                volSystemViewModel);
        }
    }
}