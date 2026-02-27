using ProjectReport.Modules.Geometry.Views; // Para usar PdfModalWindow
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ProjectReport.Views.Geometry
{
    public partial class ThermalGradientView : UserControl
    {
        public ThermalGradientView()
        {
            InitializeComponent();
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            // Ruta del PDF
            string pdfPath = Path.Combine(AppContext.BaseDirectory, "Data", "PosterMapa.pdf");

            if (!File.Exists(pdfPath))
            {
                MessageBox.Show($"No se encontró el PDF en:\n{pdfPath}");
                return;
            }

            // Abrir la modal PDF
            var pdfWindow = new PdfModalWindow(pdfPath)
            {
                Owner = Window.GetWindow(this) // Establece la ventana principal como dueño
            };
            pdfWindow.ShowDialog();
        }
    }
}
