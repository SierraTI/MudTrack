using System.Windows;

namespace ProjectReport.Views
{
    public partial class InputDialog : Window
    {
        public string Prompt { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;

        public InputDialog() { InitializeComponent(); }

        public InputDialog(string prompt) : this()
        {
            Prompt = prompt;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
