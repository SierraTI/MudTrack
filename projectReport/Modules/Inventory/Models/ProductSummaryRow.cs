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
        public double RemainingStock { get; set; }  // normalmente igual a stock actual

        public double UnitCostAvg { get; set; }     // opcional: promedio del día
        public double DailyCost { get; set; }       // suma del costo del día
    }
}
