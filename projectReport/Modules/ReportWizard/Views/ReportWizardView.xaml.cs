using System.Windows;
using ProjectReport.Models;
using ProjectReport.ViewModels;

namespace ProjectReport.Views
{
    public partial class ReportWizardView : Window
    {
        public ReportWizardView(Well well, Project project, Report? reportToEdit = null)
        {
            InitializeComponent();
            var viewModel = new ReportWizardViewModel(well, project, reportToEdit);
            viewModel.RequestClose += () => 
            {
                DialogResult = true; 
                Close();
            };
            DataContext = viewModel;
        }

        // Overload for simply initializing (if XAML needed it for design time, but we use constructor injection here)
        // Ideally we should have a parameterless constructor if used in XAML directly, but this is a modal window.
        public ReportWizardView()
        {
            InitializeComponent();
        }
    }
}
