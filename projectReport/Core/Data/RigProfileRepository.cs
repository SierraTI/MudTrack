using System;
using System.Data;
using Microsoft.Data.Sqlite;
using SqlParameter = Microsoft.Data.Sqlite.SqliteParameter;
using ProjectReport.Models.Rig;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class RigProfileRepository
    {
        private readonly DatabaseService _db;

        public RigProfileRepository(DatabaseService db)
        {
            _db = db;
        }

        // ─── SAVE ────────────────────────────────────────────────────────────────

        public void SaveRigProfile(int wellId, RigProfile profile)
        {
            if (wellId <= 0 || profile == null) return;

            int rigProfileId = UpsertRigProfile(wellId, profile);

            SaveSurfaceEquipment(rigProfileId, profile);
            SavePumps(rigProfileId, profile);
            SaveSolidsControl(rigProfileId, profile);
            SavePits(rigProfileId, profile);
        }

        private int UpsertRigProfile(int wellId, RigProfile profile)
        {
            var dt = _db.ExecuteQuery(
                "SELECT rig_profile_id FROM RigProfiles WHERE well_id = @wid",
                new SqlParameter("@wid", wellId));

            if (dt.Rows.Count == 0)
            {
                return _db.ExecuteInsertAndGetId(
                    @"INSERT INTO RigProfiles (well_id, rig_name, contractor, rig_type, created_at, modified_at)
                      VALUES (@wid, @name, @cont, @type, @now, @now)",
                    new SqlParameter("@wid", wellId),
                    new SqlParameter("@name", profile.RigName ?? (object)DBNull.Value),
                    new SqlParameter("@cont", profile.Contractor ?? (object)DBNull.Value),
                    new SqlParameter("@type", profile.RigType ?? (object)DBNull.Value),
                    new SqlParameter("@now", DateTime.UtcNow.ToString("o")));
            }
            else
            {
                int id = Convert.ToInt32(dt.Rows[0]["rig_profile_id"]);
                _db.ExecuteNonQuery(
                    @"UPDATE RigProfiles SET rig_name=@name, contractor=@cont, rig_type=@type, modified_at=@now
                      WHERE rig_profile_id=@id",
                    new SqlParameter("@name", profile.RigName ?? (object)DBNull.Value),
                    new SqlParameter("@cont", profile.Contractor ?? (object)DBNull.Value),
                    new SqlParameter("@type", profile.RigType ?? (object)DBNull.Value),
                    new SqlParameter("@now", DateTime.UtcNow.ToString("o")),
                    new SqlParameter("@id", id));
                return id;
            }
        }

        private void SaveSurfaceEquipment(int rigProfileId, RigProfile profile)
        {
            _db.ExecuteNonQuery("DELETE FROM RigSurfaceEquipment WHERE rig_profile_id=@id",
                new SqlParameter("@id", rigProfileId));

            // Save SurfaceEquipment (type=0) and ServiceLine (type=1) in same table
            foreach (var item in profile.SurfaceEquipment)
                InsertSurfaceEquipment(rigProfileId, item, 0);

            foreach (var item in profile.ServiceLine)
                InsertSurfaceEquipment(rigProfileId, item, 1);
        }

        private void InsertSurfaceEquipment(int rigProfileId, RigSurfaceEquipment item, int lineType)
        {
            _db.ExecuteNonQuery(
                @"INSERT INTO RigSurfaceEquipment (rig_profile_id, sequence_no, component_name, internal_diameter, length, description, created_at)
                  VALUES (@pid, @seq, @name, @id, @len, @desc, @now)",
                new SqlParameter("@pid", rigProfileId),
                new SqlParameter("@seq", lineType),        // 0=surface, 1=service line
                new SqlParameter("@name", item.Component ?? (object)DBNull.Value),
                new SqlParameter("@id", item.InternalDiameter),
                new SqlParameter("@len", item.Length),
                new SqlParameter("@desc", item.Description ?? (object)DBNull.Value),
                new SqlParameter("@now", DateTime.UtcNow.ToString("o")));
        }

        private void SavePumps(int rigProfileId, RigProfile profile)
        {
            _db.ExecuteNonQuery("DELETE FROM RigPumps WHERE rig_profile_id=@id",
                new SqlParameter("@id", rigProfileId));

            foreach (var pump in profile.Pumps)
            {
                _db.ExecuteNonQuery(
                    @"INSERT INTO RigPumps (rig_profile_id, pump_name, liner_size, stroke_length, efficiency, created_at)
                      VALUES (@pid, @name, @liner, @stroke, @eff, @now)",
                    new SqlParameter("@pid", rigProfileId),
                    new SqlParameter("@name", pump.PumpName ?? (object)DBNull.Value),
                    new SqlParameter("@liner", pump.MaxLinerSize),
                    new SqlParameter("@stroke", pump.StrokeLength),
                    new SqlParameter("@eff", pump.Efficiency),
                    new SqlParameter("@now", DateTime.UtcNow.ToString("o")));
            }
        }

        private void SaveSolidsControl(int rigProfileId, RigProfile profile)
        {
            _db.ExecuteNonQuery("DELETE FROM RigSolidsControl WHERE rig_profile_id=@id",
                new SqlParameter("@id", rigProfileId));

            foreach (var sc in profile.SolidsControl)
            {
                _db.ExecuteNonQuery(
                    @"INSERT INTO RigSolidsControl
                        (rig_profile_id, style, manufacturer, model, number_of_screens, nominal_rpm,
                         cap_flow_gpm, desilter_cones, desilter_cone_size, desander_cones, desander_cone_size)
                      VALUES (@pid,@style,@mfg,@model,@screens,@rpm,@gpm,@dcones,@dconesize,@sandcones,@sandsize)",
                    new SqlParameter("@pid", rigProfileId),
                    new SqlParameter("@style", sc.Style ?? (object)DBNull.Value),
                    new SqlParameter("@mfg", sc.Manufacturer ?? (object)DBNull.Value),
                    new SqlParameter("@model", sc.Model ?? (object)DBNull.Value),
                    new SqlParameter("@screens", sc.NumberOfScreens),
                    new SqlParameter("@rpm", sc.NominalRpm),
                    new SqlParameter("@gpm", sc.CapFlowGpm),
                    new SqlParameter("@dcones", sc.DesilterNumberOfCones),
                    new SqlParameter("@dconesize", sc.DesilterConeSize),
                    new SqlParameter("@sandcones", sc.DesanderNumberOfCones),
                    new SqlParameter("@sandsize", sc.DesanderConeSize));
            }
        }

        private void SavePits(int rigProfileId, RigProfile profile)
        {
            _db.ExecuteNonQuery("DELETE FROM RigPits WHERE rig_profile_id=@id",
                new SqlParameter("@id", rigProfileId));

            foreach (var pit in profile.Pits)
            {
                _db.ExecuteNonQuery(
                    @"INSERT INTO RigPits (rig_profile_id, pit_name, shape, dimensions, max_capacity, is_active)
                      VALUES (@pid,@name,@shape,@dim,@cap,@active)",
                    new SqlParameter("@pid", rigProfileId),
                    new SqlParameter("@name", pit.PitName ?? (object)DBNull.Value),
                    new SqlParameter("@shape", pit.Shape ?? (object)DBNull.Value),
                    new SqlParameter("@dim", pit.Dimensions ?? (object)DBNull.Value),
                    new SqlParameter("@cap", pit.MaxCapacity),
                    new SqlParameter("@active", pit.IsActive ? 1 : 0));
            }
        }

        // ─── LOAD ────────────────────────────────────────────────────────────────

        public RigProfile? LoadRigProfile(int wellId)
        {
            if (wellId <= 0) return null;

            var dt = _db.ExecuteQuery(
                "SELECT * FROM RigProfiles WHERE well_id=@wid",
                new SqlParameter("@wid", wellId));

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            int rigProfileId = Convert.ToInt32(row["rig_profile_id"]);

            var profile = new RigProfile
            {
                RigName = row["rig_name"]?.ToString() ?? string.Empty,
                Contractor = row["contractor"]?.ToString() ?? string.Empty,
                RigType = row["rig_type"]?.ToString() ?? string.Empty,
            };

            LoadSurfaceEquipment(rigProfileId, profile);
            LoadPumps(rigProfileId, profile);
            LoadSolidsControl(rigProfileId, profile);
            LoadPits(rigProfileId, profile);

            return profile;
        }

        private void LoadSurfaceEquipment(int rigProfileId, RigProfile profile)
        {
            var dt = _db.ExecuteQuery(
                "SELECT * FROM RigSurfaceEquipment WHERE rig_profile_id=@id ORDER BY equipment_id",
                new SqlParameter("@id", rigProfileId));

            int surfaceNo = 1, serviceNo = 1;
            foreach (DataRow r in dt.Rows)
            {
                int lineType = r["sequence_no"] != DBNull.Value ? Convert.ToInt32(r["sequence_no"]) : 0;
                var item = new RigSurfaceEquipment
                {
                    No = lineType == 0 ? surfaceNo++ : serviceNo++,
                    Component = r["component_name"]?.ToString() ?? string.Empty,
                    InternalDiameter = r["internal_diameter"] != DBNull.Value ? Convert.ToDouble(r["internal_diameter"]) : 0,
                    Length = r["length"] != DBNull.Value ? Convert.ToDouble(r["length"]) : 0,
                    Description = r["description"]?.ToString() ?? string.Empty,
                };

                if (lineType == 0) profile.SurfaceEquipment.Add(item);
                else profile.ServiceLine.Add(item);
            }
        }

        private void LoadPumps(int rigProfileId, RigProfile profile)
        {
            var dt = _db.ExecuteQuery(
                "SELECT * FROM RigPumps WHERE rig_profile_id=@id ORDER BY pump_id",
                new SqlParameter("@id", rigProfileId));

            int no = 1;
            foreach (DataRow r in dt.Rows)
            {
                profile.Pumps.Add(new RigPump
                {
                    No = no++,
                    PumpName = r["pump_name"]?.ToString() ?? string.Empty,
                    MaxLinerSize = r["liner_size"] != DBNull.Value ? Convert.ToDouble(r["liner_size"]) : 0,
                    StrokeLength = r["stroke_length"] != DBNull.Value ? Convert.ToDouble(r["stroke_length"]) : 0,
                    Efficiency = r["efficiency"] != DBNull.Value ? Convert.ToDouble(r["efficiency"]) : 0,
                });
            }
        }

        private void LoadSolidsControl(int rigProfileId, RigProfile profile)
        {
            var dt = _db.ExecuteQuery(
                "SELECT * FROM RigSolidsControl WHERE rig_profile_id=@id ORDER BY id",
                new SqlParameter("@id", rigProfileId));

            int no = 1;
            foreach (DataRow r in dt.Rows)
            {
                profile.SolidsControl.Add(new RigSolidsControl
                {
                    No = no++,
                    Style = r["style"]?.ToString() ?? string.Empty,
                    Manufacturer = r["manufacturer"]?.ToString() ?? string.Empty,
                    Model = r["model"]?.ToString() ?? string.Empty,
                    NumberOfScreens = r["number_of_screens"] != DBNull.Value ? Convert.ToInt32(r["number_of_screens"]) : 0,
                    NominalRpm = r["nominal_rpm"] != DBNull.Value ? Convert.ToInt32(r["nominal_rpm"]) : 0,
                    CapFlowGpm = r["cap_flow_gpm"] != DBNull.Value ? Convert.ToDouble(r["cap_flow_gpm"]) : 0,
                    DesilterNumberOfCones = r["desilter_cones"] != DBNull.Value ? Convert.ToInt32(r["desilter_cones"]) : 0,
                    DesilterConeSize = r["desilter_cone_size"] != DBNull.Value ? Convert.ToDouble(r["desilter_cone_size"]) : 0,
                    DesanderNumberOfCones = r["desander_cones"] != DBNull.Value ? Convert.ToInt32(r["desander_cones"]) : 0,
                    DesanderConeSize = r["desander_cone_size"] != DBNull.Value ? Convert.ToDouble(r["desander_cone_size"]) : 0,
                });
            }
        }

        private void LoadPits(
     int rigProfileId,
     RigProfile profile)
        {
            var dt = _db.ExecuteQuery(
                "SELECT * FROM RigPits WHERE rig_profile_id=@id ORDER BY id",
                new SqlParameter("@id", rigProfileId));

            int no = 1;

            foreach (DataRow r in dt.Rows)
            {
                profile.Pits.Add(
                    new RigPit
                    {
                        Id =
                            Convert.ToInt32(
                                r["id"]),

                        No =
                            no++,

                        PitName =
                            r["pit_name"]?.ToString()
                            ?? string.Empty,

                        Shape =
                            r["shape"]?.ToString()
                            ?? string.Empty,

                        Dimensions =
                            r["dimensions"]?.ToString()
                            ?? string.Empty,

                        MaxCapacity =
                            r["max_capacity"] != DBNull.Value
                                ? Convert.ToDouble(
                                    r["max_capacity"])
                                : 0,

                        IsActive =
                            r["is_active"] != DBNull.Value &&
                            Convert.ToInt32(
                                r["is_active"]) == 1,
                    });
            }
        }
    }
    }
