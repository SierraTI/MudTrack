using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using ProjectReport.Models;
using ProjectReport.Services;

namespace ProjectReport.Modules.VolumeBalance.Data
{
    public class PitSystemOptionRepository
    {
        private readonly DatabaseService _db;

        public PitSystemOptionRepository()
        {
            _db = new DatabaseService();
        }

        // ============================================================
        // OBTENER TODAS LAS OPCIONES DE SISTEMA DE PITS
        // ============================================================

        public List<PitSystemOption> GetAll()
        {
            var options = new List<PitSystemOption>();

            DataTable table = _db.ExecuteQuery(@"
                SELECT
                    pit_system_id,
                    name
                FROM pit_system_options
                ORDER BY pit_system_id;
            ");

            foreach (DataRow row in table.Rows)
            {
                options.Add(new PitSystemOption
                {
                    PitSystemId = Convert.ToInt32(
                        row["pit_system_id"]
                    ),

                    Name = row["name"]?.ToString()
                           ?? string.Empty
                });
            }

            return options;
        }
    }
}