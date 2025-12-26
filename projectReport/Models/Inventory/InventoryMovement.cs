using System;

namespace ProjectReport.Models.Inventory
{
    public class InventoryMovement
    {
        public string MovementId { get; set; } = Guid.NewGuid().ToString("N");

        public string TicketId { get; set; } = "";
        public DateTime Date { get; set; }

        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";

        public TicketType Type { get; set; }
        public double Quantity { get; set; }

        public double UnitPrice { get; set; }
        public string OriginOrUse { get; set; } = "";
        public string User { get; set; } = "";
        public string Observations { get; set; } = "";

        public double StockBefore { get; set; }
        public double StockAfter { get; set; }
    }
}
