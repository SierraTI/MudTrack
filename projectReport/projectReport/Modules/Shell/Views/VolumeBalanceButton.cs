using System.Windows;
using System.Windows.Controls;

namespace ProjectReport.Views
{
    public class VolumeBalanceButton : Button
    {
        public VolumeBalanceButton()
        {
            this.Content = "Volume Balance";
            this.Style = (Style)Application.Current.FindResource("TopMenuButtonStyle");
        }
    }
}
