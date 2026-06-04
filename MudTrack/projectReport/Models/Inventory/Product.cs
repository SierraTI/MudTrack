namespace ProjectReport.Models.Inventory
{
    public class Product
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string OtherNames { get; set; } = ""; // "DescripciÃ³n - Otros Names"
        public string PhysicalState { get; set; } = ""; // "Estado fÃ­sico"
        public string Presentation { get; set; } = ""; // "PresentaciÃ³n"
        public double Quantity { get; set; } = 0; // "Quantity"
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "kg"; // kg, L, pza...
        public double SG { get; set; } = 1.0; // Specific Gravity
        public int QtyPackage { get; set; } = 1;

        public double StockQty { get; set; } // NO editar a mano: solo InventoryService
        public double CurrentUnitCost { get; set; } // Ãºltimo costo (referencia), histÃ³rico va en movimientos
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public bool IsSelectedForReport { get; set; } = false;

        // Label for UI search and display
        public string SearchLabel => $"{Code} {Name} {Category} {Unit}".Trim();
    }
}
