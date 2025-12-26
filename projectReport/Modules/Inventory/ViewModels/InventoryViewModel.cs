using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;

namespace ProjectReport.Modules.Inventory.ViewModels
{
    public sealed class InventoryViewModel : INotifyPropertyChanged
    {
        private readonly InventoryStorageService _storage = new InventoryStorageService();

        public ObservableCollection<InventoryItem> Items { get; } = new();

        private InventoryItem? _selectedItem;
        public InventoryItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (Equals(_selectedItem, value)) return;
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public InventoryViewModel()
        {
            var loaded = _storage.Load();
            foreach (var it in loaded) Items.Add(it);
        }

        public void ImportExcelDialog()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                Title = "Selecciona el Excel (LISTA DE PRODUCTOS)"
            };

            if (dlg.ShowDialog() != true) return;
            ImportExcel(dlg.FileName);
        }

        public void ImportExcel(string path)
        {
            try
            {
                var svc = new ProjectReport.Services.Inventory.InventoryExcelImportService();
                var products = svc.LoadUniversalProducts(path);

                foreach (var p in products)
                {
                    if (Items.Any(x => string.Equals(x.ItemCode, p.Codigo, System.StringComparison.OrdinalIgnoreCase)))
                        continue;

                    Items.Add(new InventoryItem
                    {
                        ItemCode = p.Codigo,
                        Name = p.Nombre,
                        Category = p.Categoria,
                        Packaging = p.Presentacion,
                        Unit = string.IsNullOrWhiteSpace(p.Unidad) ? "N/A" : p.Unidad,

                        QuantityAvailable = 0,
                        MinStock = 0,
                        MaxStock = 0,

                        Location = "N/A",
                        Status = "Available",

                        HazardClass = "Non-Hazardous",
                        Supplier = "N/A",
                        BatchNumber = string.Empty,

                        ExpirationDate = null,
                        LastMovementDate = null
                    });
                }

                _storage.Save(Items);

                MessageBox.Show($"Importados {products.Count} productos al inventario operativo.");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Error importando Excel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
