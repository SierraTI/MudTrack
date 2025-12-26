using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class InventoryHistoryViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<InventoryMovement> Movements { get; }

        public InventoryHistoryViewModel(InventoryService service)
        {
            _service = service;
            var list = _service.GetMovements()
                .OrderByDescending(m => m.Date)
                .ToList();

            Movements = new ObservableCollection<InventoryMovement>(list);
        }
    }
}
