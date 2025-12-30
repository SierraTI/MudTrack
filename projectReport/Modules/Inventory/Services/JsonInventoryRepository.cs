using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

            // Seed products.json desde recurso embebido o archivo externo si no existe
            SeedProductsIfMissing();

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

        private void SeedProductsIfMissing()
        {
            if (File.Exists(_productsFile))
                return;

            // 1) Intentar recurso embebido
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // Ajusta el nombre si cambias la carpeta/namespace. Ej: ProjectReport.Resources.default_products.json
                var resourceName = "ProjectReport.Resources.default_products.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var sr = new StreamReader(stream);
                    var json = sr.ReadToEnd();
                    var products = JsonSerializer.Deserialize<List<Product>>(json);
                    if (products != null)
                    {
                        Write(_productsFile, products);
                        return;
                    }
                }
            }
            catch
            {
                // ignore and try fallback
            }

            // 2) Fallback: buscar archivo en el directorio de salida (por si no se embebió)
            try
            {
                var exeDir = AppContext.BaseDirectory;
                var fallbackPath = Path.Combine(exeDir, "default_products.json");
                if (File.Exists(fallbackPath))
                {
                    var json = File.ReadAllText(fallbackPath);
                    var products = JsonSerializer.Deserialize<List<Product>>(json);
                    if (products != null)
                    {
                        Write(_productsFile, products);
                        return;
                    }
                }
            }
            catch
            {
                // ignore
            }

            // 3) Si todo falla, crear vacío
            Write(_productsFile, new List<Product>());
        }

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
