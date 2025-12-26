namespace ProjectReport.Models.Inventory
{
    public class Product
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "kg";

        public double StockQty { get; set; }
        public double CurrentUnitCost { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;
    }
}
