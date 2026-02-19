using System.Collections.ObjectModel;

namespace ProjectReport.Models.Inventory
{
    /// <summary>
    /// Represents a storage/warehouse base where products can be received from or returned to
    /// </summary>
    public class StorageBase
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public string Contact { get; set; } = "";
    }

    /// <summary>
    /// Default storage bases and supplier information
    /// </summary>
    public static class InventoryConstants
    {
        public static ObservableCollection<StorageBase> GetStorageBases()
        {
            return new ObservableCollection<StorageBase>
            {
                new StorageBase { Code = "WH-001", Name = "Main Warehouse", Location = "Houston, TX", Contact = "warehouse@company.com" },
                new StorageBase { Code = "WH-002", Name = "Regional Base", Location = "Denver, CO", Contact = "base@company.com" },
                new StorageBase { Code = "SIT-001", Name = "Site Base", Location = "On-site", Contact = "site@company.com" },
                new StorageBase { Code = "SUP-001", Name = "Supplier - Halliburton", Location = "Multiple", Contact = "supplier@halliburton.com" },
                new StorageBase { Code = "SUP-002", Name = "Supplier - SLB", Location = "Multiple", Contact = "supplier@slb.com" },
            };
        }

        public static ObservableCollection<string> GetProductConditions()
        {
            return new ObservableCollection<string>
            {
                "Sealed",
                "Open",
                "Damaged",
                "Expired",
                "Partial Use"
            };
        }

        public static ObservableCollection<string> GetShipmentMethods()
        {
            return new ObservableCollection<string>
            {
                "Truck",
                "Air",
                "Rail",
                "Sea",
                "Pipeline",
                "On-site Transfer"
            };
        }
    }
}
