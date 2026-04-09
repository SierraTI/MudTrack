using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class WellboreGeometryRepository
    {
        private readonly DatabaseService _db;

        public WellboreGeometryRepository(DatabaseService db)
        {
            _db = db;
        }

        public void SaveGeometry(int reportId, IEnumerable<WellboreComponent> components)
        {
            if (reportId <= 0) return;

            // 1. Clear existing geometry for this report
            _db.ExecuteNonQuery("DELETE FROM WellboreGeometry WHERE idRep = @idRep", 
                new SqlParameter("@idRep", reportId));

            // 2. Insert new components
            string query = @"INSERT INTO WellboreGeometry (idRep, Component, Description, TopMD, BottomMD, ID, OD, Washout)
                             VALUES (@idRep, @comp, @desc, @top, @bottom, @id, @od, @wash)";

            foreach (var c in components)
            {
                _db.ExecuteNonQuery(query,
                    new SqlParameter("@idRep", reportId),
                    new SqlParameter("@comp", c.Component.ToString()),
                    new SqlParameter("@desc", c.Name ?? (object)DBNull.Value),
                    new SqlParameter("@top", c.TopMD ?? (object)DBNull.Value),
                    new SqlParameter("@bottom", c.BottomMD ?? (object)DBNull.Value),
                    new SqlParameter("@id", c.ID ?? (object)DBNull.Value),
                    new SqlParameter("@od", c.OD ?? (object)DBNull.Value),
                    new SqlParameter("@wash", c.Washout ?? (object)DBNull.Value));
            }
        }

        public List<WellboreComponent> LoadGeometry(int reportId)
        {
            string query = "SELECT * FROM WellboreGeometry WHERE idRep = @idRep ORDER BY TopMD ASC";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idRep", reportId));
            
            var results = new List<WellboreComponent>();
            foreach (DataRow dr in dt.Rows)
            {
                var component = new WellboreComponent
                {
                    Name = dr["Description"]?.ToString() ?? string.Empty,
                    TopMD = dr["TopMD"] != DBNull.Value ? Convert.ToDouble(dr["TopMD"]) : null,
                    BottomMD = dr["BottomMD"] != DBNull.Value ? Convert.ToDouble(dr["BottomMD"]) : null,
                    ID = dr["ID"] != DBNull.Value ? Convert.ToDouble(dr["ID"]) : null,
                    OD = dr["OD"] != DBNull.Value ? Convert.ToDouble(dr["OD"]) : null,
                    Washout = dr["Washout"] != DBNull.Value ? Convert.ToDouble(dr["Washout"]) : null
                };

                if (Enum.TryParse(dr["Component"]?.ToString(), out ComponentType type))
                {
                    component.Component = type;
                }

                results.Add(component);
            }
            return results;
        }
    }
}
