using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class SurveyRepository
    {
        private readonly DatabaseService _db;

        public SurveyRepository(DatabaseService db)
        {
            _db = db;
        }

        public void SaveSurvey(int wellId, IEnumerable<SurveyPoint> points)
        {
            if (wellId <= 0) return;

            // 1. Clear existing survey for this well
            _db.ExecuteNonQuery("DELETE FROM WellSurvey WHERE idW = @idW", 
                new SqlParameter("@idW", wellId));

            // 2. Insert new points
            string query = @"INSERT INTO WellSurvey (idW, MD, Inclination, Azimuth, TVD, NS_Coord, EW_Coord, Dogleg)
                             VALUES (@idW, @md, @inc, @azi, @tvd, @ns, @ew, @dls)";

            foreach (var p in points)
            {
                _db.ExecuteNonQuery(query,
                    new SqlParameter("@idW", wellId),
                    new SqlParameter("@md", p.MD),
                    new SqlParameter("@inc", p.HoleAngle),
                    new SqlParameter("@azi", p.Azimuth),
                    new SqlParameter("@tvd", p.TVD),
                    new SqlParameter("@ns", p.Northing),
                    new SqlParameter("@ew", p.Easting),
                    new SqlParameter("@dls", p.DoglegSeverity));
            }
        }

        public List<SurveyPoint> LoadSurvey(int wellId)
        {
            string query = "SELECT * FROM WellSurvey WHERE idW = @idW ORDER BY MD ASC";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idW", wellId));
            
            var results = new List<SurveyPoint>();
            foreach (DataRow dr in dt.Rows)
            {
                var p = new SurveyPoint
                {
                    MD = Convert.ToDouble(dr["MD"]),
                    HoleAngle = Convert.ToDouble(dr["Inclination"]),
                    Azimuth = Convert.ToDouble(dr["Azimuth"]),
                };

                // Use internal method to set calculated fields
                p.SetCalculatedValues(
                    Convert.ToDouble(dr["TVD"]),
                    Convert.ToDouble(dr["NS_Coord"]),
                    Convert.ToDouble(dr["EW_Coord"]),
                    0, // VerticalSection - not in DB currently
                    Convert.ToDouble(dr["Dogleg"]),
                    0, // BuildRate
                    0  // TurnRate
                );

                results.Add(p);
            }
            return results;
        }
    }
}
