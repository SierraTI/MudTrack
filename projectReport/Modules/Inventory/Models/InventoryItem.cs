using System;

namespace ProjectReport.Models.Inventory
{
    public class InventoryItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string Packaging { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public int QuantityAvailable { get; set; }
        public int MinStock { get; set; }
        public int MaxStock { get; set; }

        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string HazardClass { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }

        public DateTime? LastMovementDate { get; set; }
    }
}
    