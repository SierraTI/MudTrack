using System.Windows;
using ProjectReport.ViewModels.Geometry.Config;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore; // 👈 IMPORTANTE

namespace ProjectReport.Views.Geometry
{
    public partial class ToolJointConfigWindow : Window
    {
        public ToolJointConfig Config => ((ToolJointConfigViewModel)DataContext).Model;

        public ToolJointConfigWindow(
            ToolJointConfig? model,
            ComponentType componentType = ComponentType.DrillPipe,
            WellboreComponent? currentWellboreComponent = null // 👈 NUEVO
        )
        {
            InitializeComponent();

            var vm = new ToolJointConfigViewModel(
                model ?? new ToolJointConfig(),
                componentType,
                currentWellboreComponent // 👈 PASAMOS EL WELLBORE
            );

            vm.RequestClose += result =>
            {
                DialogResult = result;
                Close();
            };

            DataContext = vm;
        }
    }
}
