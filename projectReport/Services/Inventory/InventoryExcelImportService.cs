using System.Collections.Generic;

namespace ProjectReport.Services.Inventory
{
    public class UniversalProduct
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Presentacion { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
    }

    public class InventoryExcelImportService
    {
        // Minimal stub to satisfy existing callers. Real implementation may use ClosedXML.
        public List<UniversalProduct> LoadUniversalProducts(string path)
        {
            // Return empty list for now to keep compatibility; user can replace with real importer.
            return new List<UniversalProduct>();
        }
    }
}
