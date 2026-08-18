using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectReport.Modules.VolumeBalance.Views.Additions
{
    /// <summary>
    /// Lógica de interacción para TransfersView.xaml
    /// </summary>
    public partial class TransfersView : UserControl
    {
        public TransfersView()
        {
            InitializeComponent();

            DataObject.AddPastingHandler(this, OnPaste);
        }

        private static readonly Regex _regex =
            new Regex(@"^\d*\.?\d*$");

        private void TransferVolume_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            // Si escribe coma, convertirla en punto
            string input = e.Text == "," ? "." : e.Text;

            string futureText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            futureText = futureText.Insert(textBox.SelectionStart, input);

            // Solo números y un punto decimal
            if (!_regex.IsMatch(futureText))
            {
                e.Handled = true;
                return;
            }

            // Reemplazar la coma por punto automáticamente
            if (e.Text == ",")
            {
                textBox.Text = futureText;
                textBox.CaretIndex = futureText.Length;
                e.Handled = true;
            }
        }

        private void TransferVolume_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Permitir teclas de edición
            if (e.Key == Key.Back ||
                e.Key == Key.Delete ||
                e.Key == Key.Tab ||
                e.Key == Key.Left ||
                e.Key == Key.Right ||
                e.Key == Key.Home ||
                e.Key == Key.End)
            {
                return;
            }
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string text = e.DataObject.GetData(DataFormats.Text)?.ToString() ?? "";

            text = text.Replace(",", ".");

            if (!_regex.IsMatch(text))
            {
                e.CancelCommand();
            }
        }
    }
}
