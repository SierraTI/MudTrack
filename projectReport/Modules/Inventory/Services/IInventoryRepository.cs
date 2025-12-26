using System.Collections.Generic;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Services.Inventory
{
    public interface IInventoryRepository
    {
        List<Product> LoadProducts();
        void SaveProducts(List<Product> products);

        List<InventoryMovement> LoadMovements();
        void SaveMovements(List<InventoryMovement> movements);
    }
}
