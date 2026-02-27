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
        public string Remision { get; set; } = "";     // New from SPEC
        public string SupplierName { get; set; } = "";  // New from SPEC
        public string ShipmentMethod { get; set; } = ""; // New from SPEC
        public string User { get; set; } = "";
        public string Observations { get; set; } = "";
        public bool IsAddedToFluid { get; set; } = true;
        public double StockBefore { get; set; }
        public double StockAfter { get; set; }

        // Requisition asociada al ticket (puede quedar vacía)
        public string Requisition { get; set; } = "";
    }
}
