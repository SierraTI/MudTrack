using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ProjectReport.Models;

namespace ProjectReport.Modules.VolumeBalance.Views
{
    public partial class VolumeBalanceEventView : UserControl
    {
        public VolumeBalanceEventView()
        {
            InitializeComponent();

            Loaded += VolumeBalanceEventView_Loaded;
        }

        // ============================================================
        // LOADED
        // ============================================================

        private void VolumeBalanceEventView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            var evento =
                DataContext as VolumeBalanceEvent;

            if (evento == null)
            {
                MessageBox.Show(
                    "No se encontró el evento de Volume Balance.",
                    "Volume Balance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (evento.VolumeBalanceEventId <= 0)
            {
                MessageBox.Show(
                    "El evento no tiene un VolumeBalanceEventId válido.",
                    "Volume Balance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Debug.WriteLine("========================================");
            Debug.WriteLine(
                "[VolumeBalanceEventView] LOADED");

            Debug.WriteLine(
                $"EventNo = {evento.EventNo}");

            Debug.WriteLine(
                $"EventId = {evento.VolumeBalanceEventId}");

            Debug.WriteLine(
                $"VolumeBalanceId = {evento.VolumeBalanceId}");

            Debug.WriteLine("========================================");

            // ========================================================
            // PASAR EVENT ID A LA TABLA
            // ========================================================

            Debug.WriteLine(
                "[VolumeBalanceEventView] " +
                "ASIGNANDO EVENT ID A VOLUME INFO TABLE");

            Debug.WriteLine(
                $"EventId = {evento.VolumeBalanceEventId}");

            VolumeInfoTable.SetEventId(
                evento.VolumeBalanceEventId);

            Debug.WriteLine(
                "[VolumeBalanceEventView] " +
                "EventId asignado correctamente.");

            Debug.WriteLine("========================================");

            // ========================================================
            // CARGAR TAB INICIAL
            // ========================================================

            SetActiveTab("VolSystem");
        }

        // ============================================================
        // CAMBIAR TAB
        // ============================================================

        private void SetActiveTab(
            string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var evento =
                DataContext as VolumeBalanceEvent;

            if (evento == null)
                return;

            if (evento.VolumeBalanceEventId <= 0)
                return;

            Debug.WriteLine("========================================");

            Debug.WriteLine(
                "[VolumeBalanceEventView] CAMBIANDO TAB");

            Debug.WriteLine(
                $"Tab = {tag}");

            Debug.WriteLine(
                $"EventNo = {evento.EventNo}");

            Debug.WriteLine(
                $"EventId = {evento.VolumeBalanceEventId}");

            // ========================================================
            // ESTADO DE TABS
            // ========================================================

            foreach (var child in TabsPanel.Children)
            {
                if (child is ToggleButton tb)
                {
                    tb.IsChecked =
                        tb.Tag?.ToString() == tag;
                }
            }

            // ========================================================
            // CONTENIDO
            // ========================================================

            switch (tag)
            {
                // ====================================================
                // VOL SYSTEM
                // ====================================================

                case "VolSystem":

                    Debug.WriteLine(
                        "Cargando VolSystemView...");

                    Debug.WriteLine(
                        $"VolSystem EventId = " +
                        $"{evento.VolumeBalanceEventId}");

                    // ------------------------------------------------
                    // CREAR UNA SOLA INSTANCIA
                    // ------------------------------------------------

                    var volSystemView =
                        new VolSystemView(
                            evento.VolumeBalanceEventId);

                    // ------------------------------------------------
                    // MOSTRAR VIEW
                    // ------------------------------------------------

                    MainContent.Content =
                        volSystemView;

                    // ------------------------------------------------
                    // CONECTAR VOLUME INFO TABLE
                    // CON EL MISMO VIEWMODEL
                    // ------------------------------------------------

                    if (volSystemView.ViewModel != null)
                    {
                        Debug.WriteLine(
                            "[VolumeBalanceEventView] " +
                            "Conectando VolumeInfoTable " +
                            "con VolSystemViewModel...");

                        VolumeInfoTable
                            .AttachVolumeSystemViewModel(
                                volSystemView.ViewModel);

                        Debug.WriteLine(
                            "[VolumeBalanceEventView] " +
                            "VolumeInfoTable conectado correctamente.");
                    }
                    else
                    {
                        Debug.WriteLine(
                            "[VolumeBalanceEventView] " +
                            "ERROR: VolSystemViewModel es NULL.");
                    }

                    break;


                // ====================================================
                // ADDITIONS
                // ====================================================

                case "Additions":

                    MainContent.Content =
                        new AdditionsView();

                    break;


                // ====================================================
                // LOSSES
                // ====================================================

                case "Losses":

                    MainContent.Content =
                        new LossesView();

                    break;


                // ====================================================
                // CONCENTRATIONS
                // ====================================================

                case "Concentrations":

                    MainContent.Content =
                        new ConcentrationsView();

                    break;


                // ====================================================
                // DEFAULT
                // ====================================================

                default:

                    var defaultVolSystemView =
                        new VolSystemView(
                            evento.VolumeBalanceEventId);

                    MainContent.Content =
                        defaultVolSystemView;

                    if (defaultVolSystemView.ViewModel != null)
                    {
                        VolumeInfoTable
                            .AttachVolumeSystemViewModel(
                                defaultVolSystemView.ViewModel);
                    }

                    break;
            }

            Debug.WriteLine("========================================");
        }

        // ============================================================
        // TAB CLICK
        // ============================================================

        private void Tab_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                SetActiveTab(
                    button.Tag?.ToString());
            }
        }
    }
}