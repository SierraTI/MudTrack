using System.Windows.Controls;
using ProjectReport.Services;

namespace ProjectReport.Views.Controls
{
    public partial class ToastNotificationControl : UserControl
    {
        public ToastNotificationControl()
        {
            InitializeComponent();
            DataContext = ToastNotificationService.Instance;
        }
    }
}
