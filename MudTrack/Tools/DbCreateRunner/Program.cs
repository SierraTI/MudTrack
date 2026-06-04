using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static int Main()
    {
        try
        {
            var dbPath = Path.GetFullPath(Path.Combine("..","..","projectReport","projectReport.db"));
            Console.WriteLine($"Using DB: {dbPath}");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? ".");
            var connString = $"Data Source={dbPath};Cache=Shared";
            using var conn = new SqliteConnection(connString);
            conn.Open();

            // Ensure LastRequisition meta exists
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT OR IGNORE INTO InventoryMeta (Key, Value) VALUES (@k, @v);";
                cmd.Parameters.AddWithValue("@k", "LastRequisition");
                cmd.Parameters.AddWithValue("@v", "0");
                cmd.ExecuteNonQuery();
            }

            // Insert a test product
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT OR IGNORE INTO InventoryProduct (Code, Name, Description, Category, Unit, StockQty) VALUES (@code,@name,@desc,@cat,@unit,@stock);";
                cmd.Parameters.AddWithValue("@code", "P-TEST");
                cmd.Parameters.AddWithValue("@name", "Smoke Product");
                cmd.Parameters.AddWithValue("@desc", "Created by smoke test");
                cmd.Parameters.AddWithValue("@cat", "Test");
                cmd.Parameters.AddWithValue("@unit", "kg");
                cmd.Parameters.AddWithValue("@stock", 0.0);
                cmd.ExecuteNonQuery();
            }

            long nextReq = 0;
            // Atomically get and increment LastRequisition
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "SELECT Value FROM InventoryMeta WHERE Key = @k;";
                    cmd.Parameters.AddWithValue("@k", "LastRequisition");
                    var val = cmd.ExecuteScalar()?.ToString() ?? "0";
                    if (!long.TryParse(val, out long cur)) cur = 0;
                    nextReq = cur + 1;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "UPDATE InventoryMeta SET Value = @v WHERE Key = @k;";
                    cmd.Parameters.AddWithValue("@v", nextReq.ToString());
                    cmd.Parameters.AddWithValue("@k", "LastRequisition");
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }

            // Insert a movement tied to the requisition
            var movementId = Guid.NewGuid().ToString();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO InventoryMovement (MovementId, TicketId, Date, ProductCode, ProductName, Type, Quantity, UnitPrice, OriginOrUse, ShipmentReference, SupplierName, ShipmentMethod, UserName, Observations, IsAddedToFluid, StockBefore, StockAfter, Requisition)
                                     VALUES (@mid,@tid,@date,@pcode,@pname,1,@qty,0,NULL,NULL,NULL,NULL,NULL,NULL,0,0,0,@req);";
                cmd.Parameters.AddWithValue("@mid", movementId);
                cmd.Parameters.AddWithValue("@tid", "T-" + nextReq);
                cmd.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("@pcode", "P-TEST");
                cmd.Parameters.AddWithValue("@pname", "Smoke Product");
                cmd.Parameters.AddWithValue("@qty", 10.0);
                cmd.Parameters.AddWithValue("@req", nextReq.ToString());
                cmd.ExecuteNonQuery();
            }

            // Verify counts and meta
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM InventoryProduct;";
                Console.WriteLine("InventoryProduct count: " + cmd.ExecuteScalar());
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM InventoryMovement;";
                Console.WriteLine("InventoryMovement count: " + cmd.ExecuteScalar());
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Value FROM InventoryMeta WHERE Key='LastRequisition';";
                Console.WriteLine("LastRequisition: " + cmd.ExecuteScalar());
            }

            conn.Close();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
