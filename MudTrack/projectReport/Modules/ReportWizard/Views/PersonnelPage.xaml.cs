using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels;

namespace ProjectReport.Views.ReportWizard
{
    public partial class PersonnelPage : UserControl
    {
        public PersonnelPage()
        {
            InitializeComponent();
        }

        private ReportWizardViewModel? ViewModel => DataContext as ReportWizardViewModel;

        private void AddOperator_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtNewOperator.Text) && ViewModel != null)
            {
                ViewModel.Report.OperatorReps.Add(TxtNewOperator.Text.Trim());
                TxtNewOperator.Text = "";
            }
        }

        private void RemoveOperator_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string name && ViewModel != null)
            {
                ViewModel.Report.OperatorReps.Remove(name);
            }
        }
        
        // Fix for Operator Remove
        // Since I bound to Command in XAML, I need to either add Command to VM or change XAML.
        // I will change XAML in next step to consistent Click handler.

        private void AddContractor_Click(object sender, RoutedEventArgs e)
        {
             if (!string.IsNullOrWhiteSpace(TxtNewContractor.Text) && ViewModel != null)
            {
                ViewModel.Report.ContractorReps.Add(TxtNewContractor.Text.Trim());
                TxtNewContractor.Text = "";
            }
        }

        private void RemoveContractor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string name && ViewModel != null)
            {
                ViewModel.Report.ContractorReps.Remove(name);
            }
        }

        private void AddBaroid_Click(object sender, RoutedEventArgs e)
        {
             if (!string.IsNullOrWhiteSpace(TxtNewBaroid.Text) && ViewModel != null)
            {
                ViewModel.Report.BaroidReps.Add(TxtNewBaroid.Text.Trim());
                TxtNewBaroid.Text = "";
            }
        }

        private void RemoveBaroid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string name && ViewModel != null)
            {
                ViewModel.Report.BaroidReps.Remove(name);
            }
        }
    }
}
