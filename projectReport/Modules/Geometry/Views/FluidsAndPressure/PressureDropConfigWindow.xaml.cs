using System.Windows;
using ProjectReport.ViewModels.Geometry.FluidsAndPressure;
using ProjectReport.Models.Geometry;

namespace ProjectReport.Views.Geometry
{
    public partial class PressureDropConfigWindow : Window
    {
        public PressureDropConfig Config => ((PressureDropConfigViewModel)DataContext).Model;

        public PressureDropConfigWindow(PressureDropConfig? model)
        {
            InitializeComponent();
            var vm = new PressureDropConfigViewModel(model ?? new PressureDropConfig());
            vm.RequestClose += result =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }
    }
}
