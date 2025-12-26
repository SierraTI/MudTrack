using System;
using ProjectReport.Modules.Inventory.Models;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Modules.Inventory.Models
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

        public double UnitPrice { get; set; } // histórico (para Received, y opcional para Consumed si quieres costeo)
        public string OriginOrUse { get; set; } = "";
        public string User { get; set; } = "";
        public string Observations { get; set; } = "";

        public double StockBefore { get; set; }
        public double StockAfter { get; set; }
    }
}
