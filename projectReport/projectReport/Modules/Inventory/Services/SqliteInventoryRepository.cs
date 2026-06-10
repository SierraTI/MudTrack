using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using ProjectReport.Models.Inventory;
using ProjectReport.Services;

namespace ProjectReport.Services.Inventory
{
    public class SqliteInventoryRepository : IInventoryRepository
    {
        private readonly DatabaseService _db;

        public SqliteInventoryRepository(DatabaseService? db = null)
        {
            _db = db ?? new DatabaseService();
            EnsureTables();
        }

        private void EnsureTables()
        {
            // Products
            _db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS InventoryProduct (
                Code TEXT PRIMARY KEY,
                Name TEXT,
                Description TEXT,
                OtherNames TEXT,
                PhysicalState TEXT,
                Presentation TEXT,
                Quantity REAL,
                Category TEXT,
                Unit TEXT,
                SG REAL,
                QtyPackage INTEGER,
                StockQty REAL,
                CurrentUnitCost REAL,
                Status INTEGER,
                IsSelectedForReport INTEGER
            );");

            // Movements
            _db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS InventoryMovement (
                MovementId TEXT PRIMARY KEY,
                TicketId TEXT,
                Date TEXT,
                ProductCode TEXT,
                ProductName TEXT,
                Type INTEGER,
                Quantity REAL,
                UnitPrice REAL,
                OriginOrUse TEXT,
                ShipmentReference TEXT,
                SupplierName TEXT,
                ShipmentMethod TEXT,
                UserName TEXT,
                Observations TEXT,
                IsAddedToFluid INTEGER,
                StockBefore REAL,
                StockAfter REAL,
                Requisition TEXT
            );");

            // Meta
            _db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS InventoryMeta (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );");

            // Init meta LastRequisition if missing
            var dt = _db.ExecuteQuery("SELECT Value FROM InventoryMeta WHERE Key = @k", new SqliteParameter("@k", "LastRequisition"));
            if (dt.Rows.Count == 0)
            {
                _db.ExecuteNonQuery("INSERT INTO InventoryMeta (Key, Value) VALUES (@k, @v)", new SqliteParameter("@k", "LastRequisition"), new SqliteParameter("@v", "0"));
            }
        }

        public List<Product> LoadProducts()
        {
            var dt = _db.ExecuteQuery("SELECT * FROM InventoryProduct ORDER BY Name");
            var list = new List<Product>();
            foreach (DataRow r in dt.Rows)
            {
                var p = new Product
                {
                    Code = r["Code"]?.ToString() ?? string.Empty,
                    Name = r["Name"]?.ToString() ?? string.Empty,
                    Description = r["Description"]?.ToString() ?? string.Empty,
                    OtherNames = r["OtherNames"]?.ToString() ?? string.Empty,
                    PhysicalState = r["PhysicalState"]?.ToString() ?? string.Empty,
                    Presentation = r["Presentation"]?.ToString() ?? string.Empty,
                    Quantity = r["Quantity"] != DBNull.Value ? Convert.ToDouble(r["Quantity"]) : 0,
                    Category = r["Category"]?.ToString() ?? string.Empty,
                    Unit = r["Unit"]?.ToString() ?? string.Empty,
                    SG = r["SG"] != DBNull.Value ? Convert.ToDouble(r["SG"]) : 1.0,
                    QtyPackage = r["QtyPackage"] != DBNull.Value ? Convert.ToInt32(r["QtyPackage"]) : 1,
                    StockQty = r["StockQty"] != DBNull.Value ? Convert.ToDouble(r["StockQty"]) : 0,
                    CurrentUnitCost = r["CurrentUnitCost"] != DBNull.Value ? Convert.ToDouble(r["CurrentUnitCost"]) : 0,
                    Status = r["Status"] != DBNull.Value ? (ProductStatus)Convert.ToInt32(r["Status"]) : ProductStatus.Active,
                    IsSelectedForReport = r["IsSelectedForReport"] != DBNull.Value && Convert.ToInt32(r["IsSelectedForReport"]) != 0
                };
                list.Add(p);
            }
            return list;
        }

        public void SaveProducts(List<Product> products)
        {
            
            // Simpler: upsert each product using DatabaseService
            foreach (var p in products)
            {
                _db.ExecuteNonQuery(@"INSERT OR REPLACE INTO InventoryProduct (Code, Name, Description, OtherNames, PhysicalState, Presentation, Quantity, Category, Unit, SG, QtyPackage, StockQty, CurrentUnitCost, Status, IsSelectedForReport)
                    VALUES (@Code,@Name,@Description,@OtherNames,@PhysicalState,@Presentation,@Quantity,@Category,@Unit,@SG,@QtyPackage,@StockQty,@CurrentUnitCost,@Status,@IsSelectedForReport)",
                    new SqliteParameter("@Code", p.Code),
                    new SqliteParameter("@Name", p.Name),
                    new SqliteParameter("@Description", p.Description),
                    new SqliteParameter("@OtherNames", p.OtherNames),
                    new SqliteParameter("@PhysicalState", p.PhysicalState),
                    new SqliteParameter("@Presentation", p.Presentation),
                    new SqliteParameter("@Quantity", p.Quantity),
                    new SqliteParameter("@Category", p.Category),
                    new SqliteParameter("@Unit", p.Unit),
                    new SqliteParameter("@SG", p.SG),
                    new SqliteParameter("@QtyPackage", p.QtyPackage),
                    new SqliteParameter("@StockQty", p.StockQty),
                    new SqliteParameter("@CurrentUnitCost", p.CurrentUnitCost),
                    new SqliteParameter("@Status", (int)p.Status),
                    new SqliteParameter("@IsSelectedForReport", p.IsSelectedForReport ? 1 : 0)
                );
            }
        }

        public List<InventoryMovement> LoadMovements()
        {
            var dt = _db.ExecuteQuery("SELECT * FROM InventoryMovement ORDER BY Date DESC");
            var list = new List<InventoryMovement>();
            foreach (DataRow r in dt.Rows)
            {
                var m = new InventoryMovement
                {
                    MovementId = r["MovementId"]?.ToString() ?? Guid.NewGuid().ToString("N"),
                    TicketId = r["TicketId"]?.ToString() ?? string.Empty,
                    Date = r["Date"] != DBNull.Value ? DateTime.Parse(r["Date"].ToString()!) : DateTime.Now,
                    ProductCode = r["ProductCode"]?.ToString() ?? string.Empty,
                    ProductName = r["ProductName"]?.ToString() ?? string.Empty,
                    Type = r["Type"] != DBNull.Value ? (TicketType)Convert.ToInt32(r["Type"]) : TicketType.Consumed,
                    Quantity = r["Quantity"] != DBNull.Value ? Convert.ToDouble(r["Quantity"]) : 0,
                    UnitPrice = r["UnitPrice"] != DBNull.Value ? Convert.ToDouble(r["UnitPrice"]) : 0,
                    OriginOrUse = r["OriginOrUse"]?.ToString() ?? string.Empty,
                    ShipmentReference = r["ShipmentReference"]?.ToString() ?? string.Empty,
                    SupplierName = r["SupplierName"]?.ToString() ?? string.Empty,
                    ShipmentMethod = r["ShipmentMethod"]?.ToString() ?? string.Empty,
                    User = r["UserName"]?.ToString() ?? string.Empty,
                    Observations = r["Observations"]?.ToString() ?? string.Empty,
                    IsAddedToFluid = r["IsAddedToFluid"] != DBNull.Value && Convert.ToInt32(r["IsAddedToFluid"]) != 0,
                    StockBefore = r["StockBefore"] != DBNull.Value ? Convert.ToDouble(r["StockBefore"]) : 0,
                    StockAfter = r["StockAfter"] != DBNull.Value ? Convert.ToDouble(r["StockAfter"]) : 0,
                    Requisition = r["Requisition"]?.ToString() ?? string.Empty
                };
                list.Add(m);
            }
            return list;
        }

        public void SaveMovements(List<InventoryMovement> movements)
        {
            // For simplicity, delete all and reinsert (small dataset)
            _db.ExecuteNonQuery("DELETE FROM InventoryMovement");
            foreach (var m in movements)
            {
                _db.ExecuteNonQuery(@"INSERT INTO InventoryMovement (MovementId, TicketId, Date, ProductCode, ProductName, Type, Quantity, UnitPrice, OriginOrUse, ShipmentReference, SupplierName, ShipmentMethod, UserName, Observations, IsAddedToFluid, StockBefore, StockAfter, Requisition)
                    VALUES (@MovementId,@TicketId,@Date,@ProductCode,@ProductName,@Type,@Quantity,@UnitPrice,@OriginOrUse,@ShipmentReference,@SupplierName,@ShipmentMethod,@UserName,@Observations,@IsAddedToFluid,@StockBefore,@StockAfter,@Requisition)",
                    new SqliteParameter("@MovementId", m.MovementId),
                    new SqliteParameter("@TicketId", m.TicketId),
                    new SqliteParameter("@Date", m.Date.ToString("o")),
                    new SqliteParameter("@ProductCode", m.ProductCode),
                    new SqliteParameter("@ProductName", m.ProductName),
                    new SqliteParameter("@Type", (int)m.Type),
                    new SqliteParameter("@Quantity", m.Quantity),
                    new SqliteParameter("@UnitPrice", m.UnitPrice),
                    new SqliteParameter("@OriginOrUse", m.OriginOrUse),
                    new SqliteParameter("@ShipmentReference", m.ShipmentReference),
                    new SqliteParameter("@SupplierName", m.SupplierName),
                    new SqliteParameter("@ShipmentMethod", m.ShipmentMethod),
                    new SqliteParameter("@UserName", m.User),
                    new SqliteParameter("@Observations", m.Observations),
                    new SqliteParameter("@IsAddedToFluid", m.IsAddedToFluid ? 1 : 0),
                    new SqliteParameter("@StockBefore", m.StockBefore),
                    new SqliteParameter("@StockAfter", m.StockAfter),
                    new SqliteParameter("@Requisition", m.Requisition)
                );
            }
        }

        public string GetNextRequisition()
        {
            try
            {
                var dt = _db.ExecuteQuery("SELECT Value FROM InventoryMeta WHERE Key = @k", new SqliteParameter("@k", "LastRequisition"));
                int current = 0;
                if (dt.Rows.Count > 0 && int.TryParse(dt.Rows[0][0]?.ToString(), out var v)) current = v;
                current++;
                _db.ExecuteNonQuery("UPDATE InventoryMeta SET Value = @v WHERE Key = @k", new SqliteParameter("@v", current.ToString()), new SqliteParameter("@k", "LastRequisition"));
                return current.ToString();
            }
            catch
            {
                // fallback
                var movements = LoadMovements();
                int max = 0;
                foreach (var m in movements)
                {
                    if (int.TryParse(m.Requisition, out var v) && v > max) max = v;
                }
                max++;
                _db.ExecuteNonQuery("UPDATE InventoryMeta SET Value = @v WHERE Key = @k", new SqliteParameter("@v", max.ToString()), new SqliteParameter("@k", "LastRequisition"));
                return max.ToString();
            }
        }

        public void CompactRequisitions()
        {
            try
            {
                var movements = LoadMovements();
                var groups = movements.Where(m => !string.IsNullOrWhiteSpace(m.Requisition))
                    .GroupBy(m => m.Requisition)
                    .Select(g => new { Key = g.Key, FirstDate = g.Min(x => x.Date), Numeric = int.TryParse(g.Key, out var n) ? (int?)n : null })
                    .ToList();
                if (groups.Count == 0)
                {
                    _db.ExecuteNonQuery("UPDATE InventoryMeta SET Value = @v WHERE Key = @k", new SqliteParameter("@v", "0"), new SqliteParameter("@k", "LastRequisition"));
                    return;
                }

                bool anyNonNumeric = groups.Any(x => !x.Numeric.HasValue);
                var ordered = anyNonNumeric ? groups.OrderBy(x => x.FirstDate).ToList() : groups.OrderBy(x => x.Numeric).ThenBy(x => x.FirstDate).ToList();
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int counter = 0;
                foreach (var item in ordered)
                {
                    counter++;
                    map[item.Key] = counter.ToString();
                }

                bool changed = false;
                foreach (var mv in movements)
                {
                    if (!string.IsNullOrWhiteSpace(mv.Requisition) && map.TryGetValue(mv.Requisition, out var newReq))
                    {
                        if (mv.Requisition != newReq)
                        {
                            mv.Requisition = newReq;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    SaveMovements(movements);
                }

                _db.ExecuteNonQuery("UPDATE InventoryMeta SET Value = @v WHERE Key = @k", new SqliteParameter("@v", counter.ToString()), new SqliteParameter("@k", "LastRequisition"));
            }
            catch { }
        }
    }
}
