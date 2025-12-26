using System.Windows.Controls;
using ProjectReport.ViewModels.Geometry.Wellbore;

namespace ProjectReport.Views.Geometry.Wellbore
{
    /// <summary>
    /// Vista específica para Wellbore Geometry.
    /// Muestra la cuadrícula de secciones de wellbore con validaciones y acciones.
    /// </summary>
    public partial class WellboreGeometryView : UserControl
    {
        private WellboreGeometryViewModel? _viewModel;

        public WellboreGeometryView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (e.NewValue is WellboreGeometryViewModel vm)
                {
                    _viewModel = vm;
                }
            };
        }
    }
}
