using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class DrillStringRepository
    {
        private readonly DatabaseService _db;

        public DrillStringRepository(DatabaseService db)
        {
            _db = db;
        }

        public void SaveDrillString(int wellId, IEnumerable<DrillStringComponent> components)
        {
            if (wellId <= 0) return;

            // 1. Clear existing drill string for this well
            _db.ExecuteNonQuery("DELETE FROM DrillString WHERE idW = @idW AND idRep IS NULL", 
                new SqlParameter("@idW", wellId));

            // 2. Insert new components
            string query = @"INSERT INTO DrillString (idW, idRep, ComponentType, Description, OD, ID, Weight, Length, CumulativeLength, Displacement, Capacity)
                             VALUES (@idW, NULL, @type, @desc, @od, @id, @weight, @len, @cum, @disp, @cap)";

            foreach (var c in components)
            {
                _db.ExecuteNonQuery(query,
                    new SqlParameter("@idW", wellId),
                    new SqlParameter("@type", c.ComponentType.ToString()),
                    new SqlParameter("@desc", c.Name ?? (object)DBNull.Value),
                    new SqlParameter("@od", c.OD ?? (object)DBNull.Value),
                    new SqlParameter("@id", c.ID ?? (object)DBNull.Value),
                    new SqlParameter("@weight", c.WeightPerFoot ?? (object)DBNull.Value),
                    new SqlParameter("@len", c.Length ?? (object)DBNull.Value),
                    new SqlParameter("@cum", c.BottomMD ?? (object)DBNull.Value),
                    new SqlParameter("@disp", c.DisplacementVolume),
                    new SqlParameter("@cap", c.InternalVolume));
            }
        }

        public List<DrillStringComponent> LoadDrillString(int wellId)
        {
            string query = "SELECT * FROM DrillString WHERE idW = @idW AND idRep IS NULL ORDER BY idDS ASC";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idW", wellId));
            
            var results = new List<DrillStringComponent>();
            foreach (DataRow dr in dt.Rows)
            {
                var component = new DrillStringComponent
                {
                    Name = dr["Description"]?.ToString() ?? string.Empty,
                    OD = dr["OD"] != DBNull.Value ? Convert.ToDouble(dr["OD"]) : null,
                    ID = dr["ID"] != DBNull.Value ? Convert.ToDouble(dr["ID"]) : null,
                    WeightPerFoot = dr["Weight"] != DBNull.Value ? Convert.ToDouble(dr["Weight"]) : null,
                    TopMD = dr["CumulativeLength"] != DBNull.Value ? Convert.ToDouble(dr["CumulativeLength"]) : null,
                    BottomMD = dr["CumulativeLength"] != DBNull.Value ? Convert.ToDouble(dr["CumulativeLength"]) : null
                };

                if (Enum.TryParse(dr["ComponentType"]?.ToString(), out ComponentType type))
                {
                    component.ComponentType = type;
                }

                results.Add(component);
            }
            return results;
        }
    }
}
