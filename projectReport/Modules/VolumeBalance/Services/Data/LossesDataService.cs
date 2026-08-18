using System;
using System.Collections.Generic;
using System.Data;
using ProjectReport.Models;
using ProjectReport.Services;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class LossesDataService
    {
        private readonly DatabaseService _database;

        public LossesDataService(DatabaseService database)
        {
            _database = database;
        }

        //===================================
        // LOSS TYPE
        //===================================

        public List<LossesType> GetLossesTypes()
        {
            var result = new List<LossesType>();

            string query =
            @"
            SELECT
                losses_type_id,
                name,
                description,
                is_active
            FROM losses_type;
            ";

            DataTable table = _database.ExecuteQuery(query);

            System.Diagnostics.Debug.WriteLine(
                $"LossType encontrados: {table.Rows.Count}");

            foreach (DataRow row in table.Rows)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ID {row["losses_type_id"]} - {row["name"]}");

                result.Add(new LossesType
                {
                    Id = Convert.ToInt32(row["losses_type_id"]),

                    Name = row["name"]?.ToString() ?? "",

                    Description =
                        row["description"] == DBNull.Value
                        ? null
                        : row["description"]?.ToString(),

                    IsActive =
                        Convert.ToBoolean(row["is_active"])
                });
            }

            return result;
        }


        //===================================
        // LOSS SUB TYPE
        //===================================

        public List<LossesSubType> GetLossesSubTypes()
        {
            var result = new List<LossesSubType>();

            string query =
            @"
            SELECT
                losses_subtype_id,
                losses_type_id,
                name,
                description,
                is_active
            FROM losses_subtype
            WHERE is_active = 1;
            ";

            DataTable table = _database.ExecuteQuery(query);

            foreach (DataRow row in table.Rows)
            {
                result.Add(new LossesSubType
                {
                    Id =
                        Convert.ToInt32(row["losses_subtype_id"]),

                    LossesTypeId =
                        Convert.ToInt32(row["losses_type_id"]),

                    Name =
                        row["name"]?.ToString() ?? "",

                    Description =
                        row["description"] == DBNull.Value
                        ? null
                        : row["description"]?.ToString(),

                    IsActive =
                        Convert.ToBoolean(row["is_active"])
                });
            }

            return result;
        }
    }
}