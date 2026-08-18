using Microsoft.Data.Sqlite;
using ProjectReport.Services;

namespace ProjectReport.Core.Seeders
{
    public static class PitSystemSeeder
    {
        public static void Seed(DatabaseService db)
        {
            // ============================================================
            // PIT SYSTEM OPTIONS
            // ============================================================

            InsertPitSystemOption(db, 1, "Active");
            InsertPitSystemOption(db, 2, "Reserve");
            InsertPitSystemOption(db, 3, "Other");
        }

        // ================================================================
        // MÉTODO PARA INSERTAR OPCIÓN DE PIT SYSTEM
        // ================================================================

        private static void InsertPitSystemOption(
            DatabaseService db,
            int pitSystemId,
            string name)
        {
            db.ExecuteNonQuery(@"
                INSERT OR IGNORE INTO pit_system_options
                (
                    pit_system_id,
                    name
                )
                VALUES
                (
                    @pit_system_id,
                    @name
                );
            ",
                new SqliteParameter("@pit_system_id", pitSystemId),
                new SqliteParameter("@name", name)
            );
        }
    }
}