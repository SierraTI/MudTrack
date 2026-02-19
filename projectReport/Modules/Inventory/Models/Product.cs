using ProjectReport.Models.Inventory;

namespace ProjectReport.Modules.Inventory.Models
{
    public class Product
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string OtherNames { get; set; } = ""; // "Descripción - Otros nombres"
        public string PhysicalState { get; set; } = ""; // "Estado físico"
        public string Presentation { get; set; } = ""; // "Presentación"
        public double Quantity { get; set; } = 0; // "Cantidad"
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "kg"; // kg, L, pza...
        public double SG { get; set; } = 0; // Specific Gravity

        public double StockQty { get; set; } // NO editar a mano: solo InventoryService
        public double CurrentUnitCost { get; set; } // último costo (referencia), histórico va en movimientos
        public ProductStatus Status { get; set; } = ProductStatus.Active;
    }
}