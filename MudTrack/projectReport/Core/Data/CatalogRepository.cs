using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using SqlParameter = Microsoft.Data.Sqlite.SqliteParameter;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class CatalogRepository
    {
        private readonly DatabaseService _db;

        public CatalogRepository(DatabaseService db)
        {
            _db = db;
        }

        public List<string> GetFluidNames()
        {
            string query = "SELECT FluidName FROM FluidCatalog ORDER BY FluidName";
            DataTable dt = _db.ExecuteQuery(query);
            var results = new List<string>();
            foreach (DataRow dr in dt.Rows)
            {
                results.Add(dr["FluidName"]?.ToString() ?? string.Empty);
            }
            return results;
        }

        public void AddFluidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string query = "IF NOT EXISTS (SELECT 1 FROM FluidCatalog WHERE FluidName = @name) INSERT INTO FluidCatalog (FluidName) VALUES (@name)";
            _db.ExecuteNonQuery(query, new SqlParameter("@name", name));
        }
    }
}
