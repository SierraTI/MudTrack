using System;
using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Services.Inventory;
using System.IO;

namespace ProjectReport.Views.Inventory
{
    public partial class ChemicalListView : UserControl
    {
        public ChemicalListView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize with injected service or use default
            var dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProjectReport"
            );
            
            var inventoryService = new InventoryService(new JsonInventoryRepository(dataPath));
            DataContext = new ChemicalListViewModel(inventoryService);
        }

        private void SelectedProductsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Check if we are editing the SG column
            if (e.Column.Header.ToString() == "SG")
            {
                var editedElement = e.EditingElement as TextBox;
                if (editedElement != null)
                {
                    string newValue = editedElement.Text;
                    
                    // Show confirmation advice
                    var result = MessageBox.Show(
                        $"Are you sure you want to change the Specific Gravity (SG) to {newValue}?\n\nThis will affect volume calculations in other modules.",
                        "Confirm SG Change",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        // Cancel the edit
                        e.Cancel = true;
                        editedElement.Undo();
                    }
                }
            }
        }
    }
}
