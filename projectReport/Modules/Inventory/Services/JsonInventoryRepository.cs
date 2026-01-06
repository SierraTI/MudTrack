using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly string _metaFile;

        public JsonInventoryRepository(string? basePath = null)
        {
            _basePath = basePath ??
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                     "ProjectReport");

            Directory.CreateDirectory(_basePath);

            _productsFile = Path.Combine(_basePath, "products.json");
            _movementsFile = Path.Combine(_basePath, "movements.json");
            _metaFile = Path.Combine(_basePath, "meta.json");

            SeedProductsIfMissing();

            if (!File.Exists(_movementsFile))
            {
                Write(_movementsFile, new List<InventoryMovement>());
            }

            if (!File.Exists(_metaFile))
            {
                Write(_metaFile, new Meta { LastRequisition = 0 });
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

        // Devuelve el siguiente número de requisición (persistido en meta.json)
        public string GetNextRequisition()
        {
            try
            {
                var meta = Read<Meta>(_metaFile) ?? new Meta { LastRequisition = 0 };
                meta.LastRequisition++;
                Write(_metaFile, meta);
                return meta.LastRequisition.ToString();
            }
            catch
            {
                // Fallback: calcular a partir de movimientos existentes
                var movements = LoadMovements();
                int max = 0;
                foreach (var m in movements)
                {
                    if (int.TryParse(m.Requisition, out var v) && v > max) max = v;
                }
                max++;
                try
                {
                    Write(_metaFile, new Meta { LastRequisition = max });
                }
                catch { }
                return max.ToString();
            }
        }

        // Compacta las requisiciones existentes para que sean secuenciales 1..N
        public void CompactRequisitions()
        {
            try
            {
                var movements = LoadMovements();

                // Agrupar por valor de requisición (no vacío)
                var groups = movements
                    .Where(m => !string.IsNullOrWhiteSpace(m.Requisition))
                    .GroupBy(m => m.Requisition)
                    .Select(g =>
                    {
                        var numeric = int.TryParse(g.Key, out var n) ? n : (int?)null;
                        var firstDate = g.Min(x => x.Date);
                        return new { Key = g.Key, Numeric = numeric, FirstDate = firstDate };
                    })
                    .ToList();

                if (groups.Count == 0)
                {
                    // No hay requisiciones: resetear meta
                    Write(_metaFile, new Meta { LastRequisition = 0 });
                    return;
                }

                // Si hay valores no numéricos, ordenamos por fecha; si todos son numéricos, por número
                bool anyNonNumeric = groups.Any(x => !x.Numeric.HasValue);

                var ordered = anyNonNumeric
                    ? groups.OrderBy(x => x.FirstDate).ToList()
                    : groups.OrderBy(x => x.Numeric).ThenBy(x => x.FirstDate).ToList();

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int counter = 0;
                foreach (var item in ordered)
                {
                    counter++;
                    map[item.Key] = counter.ToString();
                }

                bool changed = false;
                foreach (var mv in movements)
                {
                    if (!string.IsNullOrWhiteSpace(mv.Requisition) && map.TryGetValue(mv.Requisition, out var newReq))
                    {
                        if (mv.Requisition != newReq)
                        {
                            mv.Requisition = newReq;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    Write(_movementsFile, movements);
                }

                // Actualizar meta.json con el último valor
                Write(_metaFile, new Meta { LastRequisition = counter });
            }
            catch
            {
                // No lanzar excepción desde aquí para no romper flujo de eliminación
            }
        }

        private void SeedProductsIfMissing()
        {
            if (File.Exists(_productsFile))
                return;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
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
            }

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
            }

            Write(_productsFile, new List<Product>());
        }

        private class Meta
        {
            public int LastRequisition { get; set; }
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
