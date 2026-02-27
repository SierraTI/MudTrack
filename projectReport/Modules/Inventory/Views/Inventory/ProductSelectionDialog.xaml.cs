using System;
using System.Windows;
using ProjectReport.Services;

namespace ProjectReport.Views.Inventory
{
    public partial class ProductSelectionDialog : Window
    {
        public ProductSelectionDialog(bool filterOnlySelected = false, System.Collections.Generic.List<string>? allowedCodes = null)
        {
            InitializeComponent();
 
            if (SelectionView.DataContext is ProjectReport.ViewModels.Inventory.ChemicalListViewModel vm)
            {
                // Apply filter if requested
                if (filterOnlySelected)
                {
                    vm.IsFilterBySelected = true;
                }

                // Apply context-specific filtering if allowed codes provided
                if (allowedCodes != null)
                {
                    vm.AllowedProductCodes = allowedCodes;
                }
            }
            
            // Close dialog when products are selected and saved in ChemicalListView
            WellContextService.Instance.ChemicalSelectionUpdated += OnChemicalSelectionUpdated;
        }

        private void OnChemicalSelectionUpdated(object sender, ChemicalSelectionUpdatedEventArgs e)
        {
            // Detach event handler and close dialog
            WellContextService.Instance.ChemicalSelectionUpdated -= OnChemicalSelectionUpdated;
            this.DialogResult = true;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Ensure event is detached if closed manually
            WellContextService.Instance.ChemicalSelectionUpdated -= OnChemicalSelectionUpdated;
        }
    }
}
