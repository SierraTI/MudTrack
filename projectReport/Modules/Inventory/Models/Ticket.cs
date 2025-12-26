using System;

namespace ProjectReport.Models.Inventory
{
    public class Ticket
    {
        public string TicketId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Date { get; set; } = DateTime.Now;

        public string User { get; set; } = "";
        public TicketType Type { get; set; }

        public string Observations { get; set; } = "";

        // Para empezar simple: 1 línea por ticket.
        // Si mañana quieres multi-línea: lo cambias a List<TicketLine>.
        public TicketLine Line { get; set; } = new TicketLine();
    }
}
