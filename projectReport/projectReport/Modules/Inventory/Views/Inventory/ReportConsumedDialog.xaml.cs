using System;
using System.Windows;
using ProjectReport.ViewModels.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class ReportConsumedDialog : Window
    {
        public ReportConsumedDialog()
        {
            InitializeComponent();
            DataContextChanged += ReportConsumedDialog_DataContextChanged;
            Closed += ReportConsumedDialog_Closed;
        }

        private void ReportConsumedDialog_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ReportConsumedViewModel oldVm)
                oldVm.RequestClose -= Vm_RequestClose;

            if (e.NewValue is ReportConsumedViewModel newVm)
                newVm.RequestClose += Vm_RequestClose;
        }

        private void Vm_RequestClose()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                {
                    try { Close(); } catch { /* ignore */ }
                }
            });
        }

        private void ReportConsumedDialog_Closed(object? sender, EventArgs e)
        {
            if (DataContext is ReportConsumedViewModel vm)
                vm.RequestClose -= Vm_RequestClose;
        }
    }
}