namespace ProjectReport.Models.Inventory
{
    public class TicketLine
    {
        public string ProductCode { get; set; } = "";
        public double Quantity { get; set; }

        // Optional product name when creating a product from a ticket
        public string ProductName { get; set; } = "";

        // Solo aplica fuerte en Received (histórico)
        public double UnitPrice { get; set; }

        // Contexto (origen o uso)
        public string Context { get; set; } = ""; // proveedor/área/proyecto o "pozo/fluid/proyecto/otro"
    }
}
