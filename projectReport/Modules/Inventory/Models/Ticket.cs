using System;
using System.Collections.Generic;

namespace ProjectReport.Models.Inventory
{
    /// <summary>
    /// Enhanced Ticket model with Status tracking, Observations, and Shipment metadata
    /// </summary>
    public class Ticket
    {
        public string TicketId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Date { get; set; } = DateTime.Now;

        public string User { get; set; } = "";
        public TicketType Type { get; set; }

        // Observations/Commentary (up to 300 characters as per industry standard)
        public string Observations { get; set; } = "";

        // Requisition number/identifier
        public string Requisition { get; set; } = "";

        // Shipment metadata (for Ticket Received)
        public string ShipmentReference { get; set; } = "";
        public string ShipmentMethod { get; set; } = ""; // e.g., "Truck", "Air", "Rail"
        public string ReturnLocation { get; set; } = ""; // Location for empty containers

        // Ticket Status (Draft = can edit, Posted = locked and impacts stock)
        public TicketStatus Status { get; set; } = TicketStatus.Draft;

        // Single line (simplified)
        public TicketLine Line { get; set; } = new TicketLine();

        // Multi-line support for complex tickets
        public List<TicketLine> Lines { get; set; } = new List<TicketLine>();
    }

    public enum TicketStatus
    {
        Draft = 0,
        Posted = 1
    }
}
