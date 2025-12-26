using System.Collections.Generic;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;

namespace ProjectReport.Services.Inventory
{
    public class InventoryStorageService
    {
        private readonly JsonInventoryRepository _repo = new JsonInventoryRepository();

        public List<InventoryItem> Load()
        {
            var products = _repo.LoadProducts();
            var items = new List<InventoryItem>();
            foreach (var p in products)
            {
                items.Add(new InventoryItem
                {
                    ItemCode = p.Code,
                    Name = p.Name,
                    Category = p.Category,
                    Unit = p.Unit,
                    QuantityAvailable = (int)p.StockQty,
                    LastMovementDate = null
                });
            }
            return items;
        }

        public void Save(IEnumerable<InventoryItem> items)
        {
            var products = new List<Product>();
            foreach (var it in items)
            {
                products.Add(new Product
                {
                    Code = it.ItemCode,
                    Name = it.Name,
                    Category = it.Category,
                    Unit = it.Unit ?? string.Empty,
                    StockQty = it.QuantityAvailable,
                    CurrentUnitCost = 0,
                    Status = ProductStatus.Active,
                    Description = string.Empty
                });
            }
            _repo.SaveProducts(products);
        }
    }
}
