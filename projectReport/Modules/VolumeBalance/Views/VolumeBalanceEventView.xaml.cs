using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ProjectReport.Modules.VolumeBalance.Views
{
    public partial class VolumeBalanceEventView : UserControl
    {
        public VolumeBalanceEventView()
        {
            InitializeComponent();

            Loaded += (_, __) => SetActiveTab("VolSystem");
        }

        private void SetActiveTab(string tag)
        {
            // 1. UI state (toggle buttons)
            foreach (var child in TabsPanel.Children)
            {
                if (child is ToggleButton tb)
                    tb.IsChecked = tb.Tag?.ToString() == tag;
            }

            // 2. Content switching
            MainContent.Content = tag switch
            {
                "VolSystem" => new VolSystemView(),
                "Additions" => new AdditionsView(),
                "Losses" => new LossesView(),
                "Concentrations" => new ConcentrationsView(),
                _ => new VolSystemView()
            };
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
                SetActiveTab(button.Tag?.ToString());
        }
    }
}