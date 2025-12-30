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

        // Devuelve el siguiente número de requisición (persistido). Incrementa el contador y lo guarda.
        string GetNextRequisition();

        // Compacta/renumera las requisiciones para que sean secuenciales (1..N) después de eliminaciones.
        void CompactRequisitions();
    }
}
