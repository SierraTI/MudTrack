using ProjectReport.Services.Inventory;

namespace ProjectReport.Services
{
    // Service locator light: expone una �nica instancia de InventoryService
    // Idealmente sustituir por DI en el futuro.
    public static class ServiceLocator
    {
        private static InventoryService? _inventoryService;

        public static InventoryService InventoryService =>
            _inventoryService ??= new InventoryService(new ProjectReport.Services.Inventory.SqliteInventoryRepository());

        // Permite inyectar una instancia en pruebas o inicializaci�n
        public static void SetInventoryService(InventoryService svc) => _inventoryService = svc;
    }
}