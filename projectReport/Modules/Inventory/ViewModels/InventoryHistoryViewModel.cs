using System;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Inventory
{
    public class InventoryHistoryViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<InventoryMovement> Movements { get; } = new();

        public RelayCommand RefreshCommand { get; }

        public InventoryHistoryViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            RefreshCommand = new RelayCommand(_ => LoadMovements());

            // Suscribirse para refrescar automáticamente cuando cambie el inventario
            _service.InventoryUpdated += OnInventoryUpdated;

            LoadMovements();
        }

        private void OnInventoryUpdated()
        {
            // Si la llamada viene de un thread no UI deberías hacer Dispatcher.Invoke.
            // Aquí asumimos que InventoryService dispara en el hilo UI o que el caller lo maneja.
            LoadMovements();
        }

        private void LoadMovements()
        {
            Movements.Clear();
            var list = _service.GetMovements()
                               .OrderByDescending(m => m.Date)
                               .ToList();

            foreach (var m in list) Movements.Add(m);
        }

        // Llamar cuando se destruya la vista / VM para evitar fugas
        public void Dispose()
        {
            try
            {
                _service.InventoryUpdated -= OnInventoryUpdated;
            }
            catch { }
        }
    }
}