namespace ProjectReport.Models.Inventory
{
    public class Product
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string OtherNames { get; set; } = "";
        public string PhysicalState { get; set; } = "";
        public string Presentation { get; set; } = "";
        public double Quantity { get; set; } = 0;
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "kg";
        public double SG { get; set; } = 0;

        public double StockQty { get; set; }
        public double CurrentUnitCost { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        // Nuevo: etiqueta combinada para b�squeda (no rompe serializaci�n simple)
        public string SearchLabel => $"{Code} {Name} {Category} {Unit}".Trim();
    }
}