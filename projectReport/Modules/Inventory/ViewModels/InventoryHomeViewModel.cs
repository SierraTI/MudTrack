using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ProjectReport.Services.Inventory;
using ProjectReport.Models.Inventory;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Inventory
{
    public class InventoryHomeViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public event Action? RequestReceived;
        public event Action? RequestConsumed;
        public event Action? RequestHistory;

        public ICommand ReceivedCommand { get; }
        public ICommand ConsumedCommand { get; }
        public ICommand HistoryCommand { get; }

        public ObservableCollection<Product> Products { get; } = new();

        public InventoryHomeViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // Load initial data (lightweight)
            var products = _service.GetProducts();
            if (products != null)
            {
                foreach (var p in products)
                    Products.Add(p);
            }

            ReceivedCommand = new RelayCommand(_ => OnRequestReceived());
            ConsumedCommand = new RelayCommand(_ => OnRequestConsumed());
            HistoryCommand = new RelayCommand(_ => OnRequestHistory());
        }

        private void OnRequestReceived() => RequestReceived?.Invoke();
        private void OnRequestConsumed() => RequestConsumed?.Invoke();
        private void OnRequestHistory() => RequestHistory?.Invoke();
    }
}
