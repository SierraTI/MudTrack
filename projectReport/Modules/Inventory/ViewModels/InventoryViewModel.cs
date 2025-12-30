using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using System.Text;
using System.IO;
using System.Windows.Input;
using ProjectReport.ViewModels;

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

        // Nuevo comando para exportar productos
        public ICommand ExportProductsCommand => new RelayCommand(ExportProductsCsv);

        private void ExportProductsCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"ProductList_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() != true) return;

                var sb = new StringBuilder();
                // Cabecera
                sb.AppendLine("ItemCode,Name,Category,Packaging,Unit,QuantityAvailable,MinStock,MaxStock,Location,Status,Supplier,HazardClass,BatchNumber,ExpirationDate,LastMovementDate");

                foreach (var item in Items)
                {
                    string expiration = item.ExpirationDate.HasValue ? item.ExpirationDate.Value.ToString("yyyy-MM-dd") : "";
                    string lastMovement = item.LastMovementDate.HasValue ? item.LastMovementDate.Value.ToString("yyyy-MM-dd") : "";

                    // Asegurar comillas para campos que pueden contener comas
                    string Escape(string s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";

                    sb.AppendLine(
                        $"{Escape(item.ItemCode)}," +
                        $"{Escape(item.Name)}," +
                        $"{Escape(item.Category)}," +
                        $"{Escape(item.Packaging)}," +
                        $"{Escape(item.Unit)}," +
                        $"{item.QuantityAvailable:F3}," +
                        $"{item.MinStock:F3}," +
                        $"{item.MaxStock:F3}," +
                        $"{Escape(item.Location)}," +
                        $"{Escape(item.Status)}," +
                        $"{Escape(item.Supplier)}," +
                        $"{Escape(item.HazardClass)}," +
                        $"{Escape(item.BatchNumber)}," +
                        $"{Escape(expiration)}," +
                        $"{Escape(lastMovement)}"
                    );
                }

                File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);

                MessageBox.Show($"Exportado {Items.Count} productos a:\n{saveFileDialog.FileName}", "Exportación completada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Error exportando productos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ICommand ImportExcelCommand => new RelayCommand(_ => ImportExcelDialog());

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
