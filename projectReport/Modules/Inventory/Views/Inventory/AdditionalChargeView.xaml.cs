using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Models.Inventory; // añadido

namespace ProjectReport.Views.Inventory
{
    public partial class AdditionalChargeView : UserControl
    {
        public AdditionalChargeView()
        {
            InitializeComponent();

            // Si el DataContext heredado no es AdditionalChargeViewModel, crear uno propio
            // (garantiza que los bindings como DailyTotalCost funcionen)
            if (!DesignerProperties.GetIsInDesignMode(this) && !(this.DataContext is AdditionalChargeViewModel))
            {
                this.DataContext = new AdditionalChargeViewModel();
            }

            // Si el DataContext se cambia desde fuera, re-inicializar (por si se asigna un VM diferente)
            this.DataContextChanged += (s, e) =>
            {
                if (!DesignerProperties.GetIsInDesignMode(this) && !(this.DataContext is AdditionalChargeViewModel))
                {
                    this.DataContext = new AdditionalChargeViewModel();
                }
            };
        }

        // Añadido: handler para el botón DELETE en cada fila
        private void RowDelete_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            // Intentar obtener el ítem desde Tag (se asignó en XAML) o desde DataContext del botón
            var item = btn.Tag as AdditionalChargeItem ?? btn.DataContext as AdditionalChargeItem;
            if (item == null) return;

            if (this.DataContext is AdditionalChargeViewModel vm)
            {
                vm.Remove(item);
            }
        }
    }
}