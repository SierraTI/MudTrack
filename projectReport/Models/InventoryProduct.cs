using System;

namespace ProjectReport.Models
{
    public class InventoryProduct
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string PhysicalState { get; set; } = string.Empty;

        public string Presentation { get; set; } = string.Empty;

        public double PackageQuantity { get; set; }

        public string PackageUnit { get; set; } = string.Empty;

        public double? SG { get; set; }

        public string Category { get; set; } = string.Empty;

        public bool Status { get; set; } = true;

        public bool IsSelectedForReport { get; set; } = false;
    }
}