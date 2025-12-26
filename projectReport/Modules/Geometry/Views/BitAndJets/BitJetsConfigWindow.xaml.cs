using System.Windows;
using ProjectReport.Models.Geometry;
using ProjectReport.ViewModels.Geometry.BitAndJets;

namespace ProjectReport.Views.Geometry.BitAndJets
{
    public partial class BitJetsConfigWindow : Window
    {
        public BitJetsConfig Config => ((BitJetsConfigViewModel)DataContext).Model;

        public BitJetsConfigWindow(BitJetsConfig? model)
        {
            InitializeComponent();
            var vm = new BitJetsConfigViewModel(model ?? new BitJetsConfig());
            vm.RequestClose += result =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }
    }
}
