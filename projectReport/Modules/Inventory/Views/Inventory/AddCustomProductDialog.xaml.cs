using System.Windows;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class AddCustomProductDialog : Window
    {
        /// <summary>
        /// The newly created item (null if user cancelled).
        /// </summary>
        public ChemicalItem? Result { get; private set; }

        public AddCustomProductDialog()
        {
            InitializeComponent();
            CodeTextBox.Focus();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var code = CodeTextBox.Text.Trim();
            var name = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("Code is required.");
                CodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Name is required.");
                NameTextBox.Focus();
                return;
            }

            double sg = 1.0;
            if (!string.IsNullOrWhiteSpace(SgTextBox.Text) &&
                !double.TryParse(SgTextBox.Text.Trim(), System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture, out sg))
            {
                ShowError("SG must be a valid number (e.g. 1.0).");
                SgTextBox.Focus();
                return;
            }

            Result = new ChemicalItem
            {
                Code       = code,
                Nombre     = name,
                Unidad     = UnitTextBox.Text.Trim(),
                SG         = sg,
                Categoria  = CategoryTextBox.Text.Trim(),
                IsSelected = true
            };

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
