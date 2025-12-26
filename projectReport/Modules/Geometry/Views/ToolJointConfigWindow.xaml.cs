using System.Windows;
using ProjectReport.ViewModels.Geometry.Config;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Views.Geometry
{
    public partial class ToolJointConfigWindow : Window
    {
        public ToolJointConfig Config => ((ToolJointConfigViewModel)DataContext).Model;

        public ToolJointConfigWindow(ToolJointConfig? model, ComponentType componentType = ComponentType.DrillPipe)
        {
            InitializeComponent();
            var vm = new ToolJointConfigViewModel(model ?? new ToolJointConfig(), componentType);
            vm.RequestClose += result =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }
    }
}
