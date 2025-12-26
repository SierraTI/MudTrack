using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Services.Inventory
{
    public class JsonInventoryRepository : IInventoryRepository
    {
        private readonly string _basePath;
        private readonly string _productsFile;
        private readonly string _movementsFile;

        public JsonInventoryRepository(string? basePath = null)
        {
            _basePath = basePath ??
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                     "ProjectReport");

            Directory.CreateDirectory(_basePath);

            _productsFile = Path.Combine(_basePath, "products.json");
            _movementsFile = Path.Combine(_basePath, "movements.json");

            // Do not seed sample products: start with empty products list so inventory is filled only via tickets.

            // Ensure movements file exists
            if (!File.Exists(_movementsFile))
            {
                Write(_movementsFile, new List<InventoryMovement>());
            }
        }

        public List<Product> LoadProducts()
            => Read<List<Product>>(_productsFile) ?? new List<Product>();

        public void SaveProducts(List<Product> products)
            => Write(_productsFile, products);

        public List<InventoryMovement> LoadMovements()
            => Read<List<InventoryMovement>>(_movementsFile) ?? new List<InventoryMovement>();

        public void SaveMovements(List<InventoryMovement> movements)
            => Write(_movementsFile, movements);

        private static T? Read<T>(string path)
        {
            if (!File.Exists(path)) return default;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }

        private static void Write<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
