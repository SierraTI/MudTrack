using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ProjectReport.Models.Geometry.ThermalGradient;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class EngineeringRepository
    {
        private readonly DatabaseService _db;

        public EngineeringRepository(DatabaseService db)
        {
            _db = db;
        }

        #region Thermal Gradient

        public void SaveThermalGradient(int wellId, IEnumerable<ThermalGradientPoint> points)
        {
            if (wellId <= 0) return;

            _db.ExecuteNonQuery("DELETE FROM ThermalGradient WHERE idW = @idW", 
                new SqlParameter("@idW", wellId));

            string query = "INSERT INTO ThermalGradient (idW, MD, Temperature) VALUES (@idW, @md, @temp)";
            foreach (var p in points)
            {
                _db.ExecuteNonQuery(query,
                    new SqlParameter("@idW", wellId),
                    new SqlParameter("@md", p.TVD),
                    new SqlParameter("@temp", p.Temperature));
            }
        }

        public List<ThermalGradientPoint> LoadThermalGradient(int wellId)
        {
            string query = "SELECT * FROM ThermalGradient WHERE idW = @idW ORDER BY MD ASC";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idW", wellId));
            var results = new List<ThermalGradientPoint>();
            foreach (DataRow dr in dt.Rows)
            {
                results.Add(new ThermalGradientPoint
                {
                    TVD = Convert.ToDouble(dr["MD"]),
                    Temperature = Convert.ToDouble(dr["Temperature"])
                });
            }
            return results;
        }

        #endregion

        #region Well Tests

        public void SaveWellTests(int wellId, IEnumerable<WellTest> tests)
        {
            if (wellId <= 0) return;

            _db.ExecuteNonQuery("DELETE FROM WellTest WHERE idW = @idW", 
                new SqlParameter("@idW", wellId));

            string query = @"INSERT INTO WellTest (idW, Section, TestType, MD, TVD, TestValue, TestPressurePsi)
                             VALUES (@idW, @section, @type, @md, @tvd, @val, @psi)";

            foreach (var t in tests)
            {
                _db.ExecuteNonQuery(query,
                    new SqlParameter("@idW", wellId),
                    new SqlParameter("@section", t.Section ?? (object)DBNull.Value),
                    new SqlParameter("@type", t.Type.ToString()),
                    new SqlParameter("@md", t.MD),
                    new SqlParameter("@tvd", t.TVD),
                    new SqlParameter("@val", t.TestValue),
                    new SqlParameter("@psi", t.TestPressurePsi));
            }
        }

        public List<WellTest> LoadWellTests(int wellId)
        {
            string query = "SELECT * FROM WellTest WHERE idW = @idW ORDER BY TestDate ASC";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idW", wellId));
            var results = new List<WellTest>();
            foreach (DataRow dr in dt.Rows)
            {
                var test = new WellTest
                {
                    Section = dr["Section"]?.ToString(),
                    MD = Convert.ToDouble(dr["MD"]),
                    TVD = Convert.ToDouble(dr["TVD"]),
                    TestValue = Convert.ToDouble(dr["TestValue"]),
                    TestPressurePsi = Convert.ToDouble(dr["TestPressurePsi"])
                };

                if (Enum.TryParse(dr["TestType"]?.ToString(), out WellTestType type))
                {
                    test.Type = type;
                }

                results.Add(test);
            }
            return results;
        }

        #endregion
    }
}
