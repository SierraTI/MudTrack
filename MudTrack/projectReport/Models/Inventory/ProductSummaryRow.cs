using ProjectReport.ViewModels;
using System.Windows.Media;

namespace ProjectReport.Models.Inventory
{
    /// <summary>
    /// Real-time Stock ledger row with automated calculations and validation.
    /// Formula: Final = Initial + Received - Used + Return
    /// </summary>
    public class ProductSummaryRow : BaseViewModel
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";

        // Stock quantities
        public double InitialQty { get; set; }
        public double Received { get; set; }
        public double Used { get; set; }
        public double Returned { get; set; }

        // Automated calculation
        public double FinalQty => InitialQty + Received - Used + Returned;

        // Remaining stock (can be different from FinalQty if reserved)
        public double RemainingStock { get; set; }

        // Cost tracking
        public double UnitCostAvg { get; set; }
        public double DailyCost { get; set; }

        // Audit trail
        public string TicketId { get; set; } = "";
        public string Requisition { get; set; } = "";
        public string MovementId { get; set; } = "";

        private string _usedType = "";
        public string UsedType
        {
            get => _usedType;
            set => SetProperty(ref _usedType, value);
        }

        // Minimum required threshold (default 10% of last received or user-defined)
        private double _minimumRequired = 0;
        public double MinimumRequired
        {
            get => _minimumRequired;
            set => SetProperty(ref _minimumRequired, value);
        }

        // Color coding for Final column (Red/Yellow/Green)
        private Brush _finalQtyColor = Brushes.Green;
        public Brush FinalQtyColor
        {
            get
            {
                if (FinalQty <= 0)
                    return Brushes.Red;
                if (FinalQty < MinimumRequired)
                    return new SolidColorBrush(Colors.Orange); // Yellow/Warning
                return Brushes.Green;
            }
        }

        // Ticket status (Draft, Posted)
        private TicketStatus _status = TicketStatus.Draft;
        public TicketStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        // Flag indicating whether this row can be edited (true if Draft, false if Posted)
        public bool IsEditable => Status == TicketStatus.Draft;

        // Validation error message
        private string _validationError = "";
        public string ValidationError
        {
            get => _validationError;
            set => SetProperty(ref _validationError, value);
        }

        /// <summary>
        /// Validates that the return quantity does not exceed available stock
        /// </summary>
        public bool ValidateReturn(double returnQty)
        {
            if (returnQty > FinalQty)
            {
                ValidationError = $"Error: Cannot return {returnQty}. Only {FinalQty} available in stock.";
                return false;
            }
            ValidationError = "";
            return true;
        }
    }
}
