using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.Data.Sqlite;
using SqlParameter = Microsoft.Data.Sqlite.SqliteParameter;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class ReportRepository
    {
        private readonly DatabaseService _db;

        public ReportRepository(DatabaseService db)
        {
            _db = db;
        }

        public void SaveReport(int idW, Report report)
        {
            if (report == null) return;

            if (report.Id <= 0)
            {
                InsertReport(idW, report);
            }
            else
            {
                UpdateReport(idW, report);
            }
        }

        private void InsertReport(int idW, Report report)
        {
            // 1. Insert into Report table
            string reportQuery = @"INSERT INTO Report (idW, Interval, Interval_size, ReportDate, Report_MD, Report_TVD) 
                                             VALUES (@idW, @interval, @size, @date, @md, @tvd)";
            
                        var insertedId = _db.ExecuteInsertAndGetId(reportQuery,
                            new SqlParameter("@idW", idW),
                            new SqlParameter("@interval", int.TryParse(report.IntervalNumber, out int interval) ? interval : (object)DBNull.Value),
                            new SqlParameter("@size", report.IntervalSizeIn ?? (object)DBNull.Value),
                            new SqlParameter("@date", report.ReportDateTime),
                            new SqlParameter("@md", report.MD ?? (object)DBNull.Value),
                            new SqlParameter("@tvd", report.TVD ?? (object)DBNull.Value));
                        report.Id = insertedId;

            // 2. Insert into OperationalDetail
            string detailQuery = @"INSERT INTO OperationalDetail (idRep, Well_Section, Max_BHT, Present_Activity, Fluid) 
                                 VALUES (@idRep, @section, @bht, @activity, @fluid)";
            _db.ExecuteNonQuery(detailQuery,
                new SqlParameter("@idRep", report.Id),
                new SqlParameter("@section", report.WellSection ?? (object)DBNull.Value),
                new SqlParameter("@bht", report.MaxBHT ?? (object)DBNull.Value),
                new SqlParameter("@activity", report.PresentActivity ?? (object)DBNull.Value),
                new SqlParameter("@fluid", report.PrimaryFluidSet ?? (object)DBNull.Value));

            // 3. Insert Personnel
            SavePersonnel(report.Id, report);

            // 4. Insert Pumps
            SavePumps(report.Id, report);

            // 5. Insert Screens
            SaveScreens(report.Id, report);
        }

        private void UpdateReport(int idW, Report report)
        {
            // 1. Update Report table
            string reportQuery = @"UPDATE Report SET Interval = @interval, Interval_size = @size, 
                                 ReportDate = @date, Report_MD = @md, Report_TVD = @tvd 
                                 WHERE idRep = @idRep";
            _db.ExecuteNonQuery(reportQuery,
                new SqlParameter("@idRep", report.Id),
                new SqlParameter("@interval", int.TryParse(report.IntervalNumber, out int interval) ? interval : (object)DBNull.Value),
                new SqlParameter("@size", report.IntervalSizeIn ?? (object)DBNull.Value),
                new SqlParameter("@date", report.ReportDateTime),
                new SqlParameter("@md", report.MD ?? (object)DBNull.Value),
                new SqlParameter("@tvd", report.TVD ?? (object)DBNull.Value));

            // 2. Update OperationalDetail
            string detailQuery = @"UPDATE OperationalDetail SET Well_Section = @section, Max_BHT = @bht, 
                                 Present_Activity = @activity, Fluid = @fluid 
                                 WHERE idRep = @idRep";
            _db.ExecuteNonQuery(detailQuery,
                new SqlParameter("@idRep", report.Id),
                new SqlParameter("@section", report.WellSection ?? (object)DBNull.Value),
                new SqlParameter("@bht", report.MaxBHT ?? (object)DBNull.Value),
                new SqlParameter("@activity", report.PresentActivity ?? (object)DBNull.Value),
                new SqlParameter("@fluid", report.PrimaryFluidSet ?? (object)DBNull.Value));

            // 3. Update Personnel (Delete and Re-insert)
            _db.ExecuteNonQuery("DELETE FROM Personnel WHERE idRep = @idRep", new SqlParameter("@idRep", report.Id));
            SavePersonnel(report.Id, report);

            // 4. Update Pumps
            _db.ExecuteNonQuery("DELETE FROM ReportPump WHERE idRep = @idRep", new SqlParameter("@idRep", report.Id));
            SavePumps(report.Id, report);

            // 5. Update Screens
            _db.ExecuteNonQuery("DELETE FROM ReportScreen WHERE idRep = @idRep", new SqlParameter("@idRep", report.Id));
            SaveScreens(report.Id, report);
        }

        private void SavePersonnel(int idRep, Report report)
        {
            foreach (var p in report.OperatorReps)
                InsertPerson(idRep, "Operator Representative", p);

            foreach (var p in report.ContractorReps)
                InsertPerson(idRep, "Contractor", p);

            foreach (var p in report.BaroidReps)
                InsertPerson(idRep, "Mud Engineer", p);
        }

        private void InsertPerson(int idRep, string role, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string query = "INSERT INTO Personnel (idRep, Role, PersonName) VALUES (@idRep, @role, @name)";
            _db.ExecuteNonQuery(query,
                new SqlParameter("@idRep", idRep),
                new SqlParameter("@role", role),
                new SqlParameter("@name", name));
        }

        public List<Report> GetReportsByWellId(int idW)
        {
            string query = @"SELECT r.idRep, r.Interval, r.Interval_size, r.ReportDate, r.Report_MD, r.Report_TVD,
                                   o.Well_Section, o.Max_BHT, o.Present_Activity, o.Fluid
                            FROM Report r
                            LEFT JOIN OperationalDetail o ON r.idRep = o.idRep
                            WHERE r.idW = @idW
                            ORDER BY r.ReportDate";

            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idW", idW));
            var reports = new List<Report>();

            foreach (DataRow dr in dt.Rows)
            {
                int idRep = Convert.ToInt32(dr["idRep"]);
                var r = new Report
                {
                    Id = idRep,
                    IntervalNumber = dr["Interval"]?.ToString() ?? string.Empty,
                    IntervalSizeIn = dr["Interval_size"]?.ToString() ?? string.Empty,
                    ReportDateTime = dr["ReportDate"] != DBNull.Value ? Convert.ToDateTime(dr["ReportDate"].ToString()) : DateTime.Now,
                    MD = dr["Report_MD"] != DBNull.Value ? Convert.ToDouble(dr["Report_MD"]) : null,
                    TVD = dr["Report_TVD"] != DBNull.Value ? Convert.ToDouble(dr["Report_TVD"]) : null,
                    WellSection = dr["Well_Section"]?.ToString() ?? string.Empty,
                    MaxBHT = dr["Max_BHT"] != DBNull.Value ? Convert.ToDouble(dr["Max_BHT"]) : null,
                    PresentActivity = dr["Present_Activity"]?.ToString() ?? string.Empty,
                    PrimaryFluidSet = dr["Fluid"]?.ToString() ?? string.Empty
                };

                // Load Personnel, Pumps, Screens
                LoadPersonnel(r);
                LoadPumps(r);
                LoadScreens(r);

                reports.Add(r);
            }

            return reports;
        }

        private void LoadPersonnel(Report report)
        {
            string query = "SELECT Role, PersonName FROM Personnel WHERE idRep = @idRep";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idRep", report.Id));

            foreach (DataRow dr in dt.Rows)
            {
                string role = dr["Role"]?.ToString() ?? string.Empty;
                string name = dr["PersonName"]?.ToString() ?? string.Empty;

                if (role == "Operator Representative") report.OperatorReps.Add(name);
                else if (role == "Contractor") report.ContractorReps.Add(name);
                else if (role == "Mud Engineer") report.BaroidReps.Add(name);
            }
        }

        private void SavePumps(int idRep, Report report)
        {
            string query = @"INSERT INTO ReportPump (idRep, PumpNo, PumpName, LinerSize, StrokeLength, Efficiency, SPM, Pressure)
                             VALUES (@idRep, @no, @name, @liner, @stroke, @eff, @spm, @press)";
            foreach (var p in report.Pumps)
            {
                _db.ExecuteNonQuery(query,
                    new SqlParameter("@idRep", idRep),
                    new SqlParameter("@no", p.No),
                    new SqlParameter("@name", p.PumpName ?? (object)DBNull.Value),
                    new SqlParameter("@liner", p.LinerSize),
                    new SqlParameter("@stroke", p.StrokeLength),
                    new SqlParameter("@eff", p.Efficiency),
                    new SqlParameter("@spm", p.Spm),
                    new SqlParameter("@press", p.Pressure));
            }
        }

        private void LoadPumps(Report report)
        {
            string query = "SELECT * FROM ReportPump WHERE idRep = @idRep";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idRep", report.Id));
            foreach (DataRow dr in dt.Rows)
            {
                report.Pumps.Add(new ReportPumpOperation
                {
                    No = Convert.ToInt32(dr["PumpNo"]),
                    PumpName = dr["PumpName"]?.ToString() ?? string.Empty,
                    LinerSize = Convert.ToDouble(dr["LinerSize"]),
                    StrokeLength = Convert.ToDouble(dr["StrokeLength"]),
                    Efficiency = Convert.ToDouble(dr["Efficiency"]),
                    Spm = Convert.ToDouble(dr["SPM"]),
                    Pressure = Convert.ToDouble(dr["Pressure"])
                });
            }
        }

        private void SaveScreens(int idRep, Report report)
        {
            string query = "INSERT INTO ReportScreen (idRep, ShakerName, ScreenType, Quantity) VALUES (@idRep, @name, @type, @qty)";
            foreach (var s in report.Screens)
            {
                _db.ExecuteNonQuery(query,
                    new SqlParameter("@idRep", idRep),
                    new SqlParameter("@name", s.ShakerName ?? (object)DBNull.Value),
                    new SqlParameter("@type", s.ScreenType ?? (object)DBNull.Value),
                    new SqlParameter("@qty", s.Quantity));
            }
        }

        private void LoadScreens(Report report)
        {
            string query = "SELECT * FROM ReportScreen WHERE idRep = @idRep";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idRep", report.Id));
            foreach (DataRow dr in dt.Rows)
            {
                report.Screens.Add(new ReportScreenUsage
                {
                    ShakerName = dr["ShakerName"]?.ToString() ?? string.Empty,
                    ScreenType = dr["ScreenType"]?.ToString() ?? string.Empty,
                    Quantity = Convert.ToInt32(dr["Quantity"]),
                    IsDeducted = true // Loaded from DB implies already processed/saved
                });
            }
        }
    }
}
