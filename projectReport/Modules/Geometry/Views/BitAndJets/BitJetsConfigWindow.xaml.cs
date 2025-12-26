using System.Windows;
using ProjectReport.Models.Geometry.BitAndJets;
using ProjectReport.ViewModels.Geometry.BitAndJets;

namespace ProjectReport.Views.Geometry.BitAndJets
{
    public partial class BitJetsConfigWindow : Window
    {
        public MultiBitJetsConfig Config => ((MultiBitJetsConfigViewModel)DataContext).Model;

        public BitJetsConfigWindow(MultiBitJetsConfig? model)
        {
            InitializeComponent();
            var vm = new MultiBitJetsConfigViewModel(model ?? new MultiBitJetsConfig());
            vm.RequestClose += result =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }
    }
}
