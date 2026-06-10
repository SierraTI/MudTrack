using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows;

namespace ProjectReport.Modules.Geometry.Views
{
    public partial class PdfModalWindow : Window
    {
        public PdfModalWindow(string pdfFilePath)
        {
            InitializeComponent();
            LoadPdf(pdfFilePath);
        }

        private async void LoadPdf(string pdfFilePath)
        {
            if (!File.Exists(pdfFilePath))
            {
                MessageBox.Show($"No se encontró el PDF en:\n{pdfFilePath}");
                this.Close();
                return;
            }

            try
            {
                await PdfViewer.EnsureCoreWebView2Async();
                PdfViewer.CoreWebView2.Navigate(pdfFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar PDF: {ex.Message}");
                this.Close();
            }
        }

        //private void CloseButton_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Close();
        //}
    }
}
