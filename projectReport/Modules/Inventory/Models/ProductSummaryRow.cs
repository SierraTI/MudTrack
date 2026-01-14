using ProjectReport.ViewModels;

namespace ProjectReport.Models.Inventory
{
    public class ProductSummaryRow : BaseViewModel
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";

        public double InitialQty { get; set; }
        public double Received { get; set; }
        public double Used { get; set; }
        public double Returned { get; set; }

        public double FinalQty => InitialQty + Received - Used - Returned;
        public double RemainingStock { get; set; }

        public double UnitCostAvg { get; set; }
        public double DailyCost { get; set; }

        public string TicketId { get; set; } = "";
        public string Requisition { get; set; } = "";

        // Nuevo: MovementId para identificar unívocamente el movimiento mostrado
        public string MovementId { get; set; } = "";

        private string _usedType = "";
        public string UsedType
        {
            get => _usedType;
            set => SetProperty(ref _usedType, value);
        }
    }
}
