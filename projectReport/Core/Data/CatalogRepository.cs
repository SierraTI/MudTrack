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

        public List<string> GetFluidsByWell(int wellId)
        {
            var results = new List<string>();
            var dt = _db.ExecuteQuery("SELECT FluidName FROM WellFluids WHERE idW=@id ORDER BY FluidName", new SqlParameter("@id", wellId));
            foreach (DataRow r in dt.Rows)
            {
                results.Add(r["FluidName"]?.ToString() ?? string.Empty);
            }
            return results;
        }

        public void AddFluidToWell(int wellId, string fluidName)
        {
            if (string.IsNullOrWhiteSpace(fluidName)) return;
            _db.ExecuteNonQuery("INSERT INTO WellFluids (idW, FluidName) VALUES (@id, @f)", new SqlParameter("@id", wellId), new SqlParameter("@f", fluidName));
        }

        public void EnsureFluidExists(string fluidName)
        {
            if (string.IsNullOrWhiteSpace(fluidName)) return;
            var dt = _db.ExecuteQuery("SELECT 1 FROM FluidCatalog WHERE FluidName=@f", new SqlParameter("@f", fluidName));
            if (dt.Rows.Count == 0)
                _db.ExecuteNonQuery("INSERT INTO FluidCatalog (FluidName) VALUES (@f)", new SqlParameter("@f", fluidName));
        }

        public void AddFluidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string query = "IF NOT EXISTS (SELECT 1 FROM FluidCatalog WHERE FluidName = @name) INSERT INTO FluidCatalog (FluidName) VALUES (@name)";
            _db.ExecuteNonQuery(query, new SqlParameter("@name", name));
        }
    }
}
