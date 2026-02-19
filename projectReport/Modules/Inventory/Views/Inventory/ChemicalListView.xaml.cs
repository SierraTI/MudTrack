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
    }
}
