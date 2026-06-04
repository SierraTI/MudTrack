using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ProjectReport.Models;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class WellRepository
    {
        private readonly DatabaseService _db;

        public WellRepository(DatabaseService db)
        {
            _db = db;
        }

        public void SaveWell(Well well)
        {
            if (well == null) return;

            if (well.Id <= 0)
            {
                InsertWell(well);
            }
            else
            {
                UpdateWell(well);
            }
        }

        private void InsertWell(Well well)
        {
            // 1. Insert into Well table
            string wellQuery = "INSERT INTO Well (wellName) OUTPUT INSERTED.idW VALUES (@name)";
            var insertedId = _db.ExecuteScalar(wellQuery, new SqlParameter("@name", well.WellName));
            if (insertedId == null || insertedId == DBNull.Value)
                throw new InvalidOperationException("Failed to insert well id.");
            well.Id = Convert.ToInt32(insertedId);

            // 2. Insert into WellInfo
            string infoQuery = @"INSERT INTO WellInfo (idW, Operator, FluidType, Spud_date) 
                               VALUES (@idW, @operator, @fluid, @spud)";
            _db.ExecuteNonQuery(infoQuery, 
                new SqlParameter("@idW", well.Id),
                new SqlParameter("@operator", well.Operator ?? (object)DBNull.Value),
                new SqlParameter("@fluid", well.LoadFluid ?? (object)DBNull.Value),
                new SqlParameter("@spud", well.SpudDate ?? (object)DBNull.Value));

            // 3. Insert into WellDesign
            string designQuery = @"INSERT INTO WellDesign (idW, Trajectory, Welltype, RigName, Rigtype, Contractor) 
                                 VALUES (@idW, @traj, @type, @rig, @rigType, @contractor)";
            _db.ExecuteNonQuery(designQuery,
                new SqlParameter("@idW", well.Id),
                new SqlParameter("@traj", well.Trajectory ?? (object)DBNull.Value),
                new SqlParameter("@type", well.WellType ?? (object)DBNull.Value),
                new SqlParameter("@rig", well.RigName ?? (object)DBNull.Value),
                new SqlParameter("@rigType", well.RigType ?? (object)DBNull.Value),
                new SqlParameter("@contractor", well.Contractor ?? (object)DBNull.Value));

            // 4. Insert into WellLocation
            string locQuery = @"INSERT INTO WellLocation (idW, Location, Country, Basin, State, Block, Latitud, Longitud) 
                              VALUES (@idW, @loc, @country, @basin, @state, @block, @lat, @lon)";
            _db.ExecuteNonQuery(locQuery,
                new SqlParameter("@idW", well.Id),
                new SqlParameter("@loc", well.Location ?? (object)DBNull.Value),
                new SqlParameter("@country", well.Country ?? (object)DBNull.Value),
                new SqlParameter("@basin", well.Basin ?? (object)DBNull.Value),
                new SqlParameter("@state", well.State ?? (object)DBNull.Value),
                new SqlParameter("@block", well.Field ?? (object)DBNull.Value),
                new SqlParameter("@lat", well.Latitude?.ToString() ?? (object)DBNull.Value),
                new SqlParameter("@lon", well.Longitude?.ToString() ?? (object)DBNull.Value));
        }

        private void UpdateWell(Well well)
        {
            // 1. Update Well table
            _db.ExecuteNonQuery("UPDATE Well SET wellName = @name WHERE idW = @id",
                new SqlParameter("@name", well.WellName),
                new SqlParameter("@id", well.Id));

            // 2. Update WellInfo
            string infoQuery = @"UPDATE WellInfo SET Operator = @operator, FluidType = @fluid, Spud_date = @spud 
                               WHERE idW = @idW";
            _db.ExecuteNonQuery(infoQuery,
                new SqlParameter("@idW", well.Id),
                new SqlParameter("@operator", well.Operator ?? (object)DBNull.Value),
                new SqlParameter("@fluid", well.LoadFluid ?? (object)DBNull.Value),
                new SqlParameter("@spud", well.SpudDate ?? (object)DBNull.Value));

            // 3. Update WellDesign
            string designQuery = @"UPDATE WellDesign SET Trajectory = @traj, Welltype = @type, 
                                 RigName = @rig, Rigtype = @rigType, Contractor = @contractor 
                                 WHERE idW = @idW";
            _db.ExecuteNonQuery(designQuery,
                new SqlParameter("@idW", well.Id),
                new SqlParameter("@traj", well.Trajectory ?? (object)DBNull.Value),
                new SqlParameter("@type", well.WellType ?? (object)DBNull.Value),
                new SqlParameter("@rig", well.RigName ?? (object)DBNull.Value),
                new SqlParameter("@rigType", well.RigType ?? (object)DBNull.Value),
                new SqlParameter("@contractor", well.Contractor ?? (object)DBNull.Value));

            // 4. Update WellLocation
            string locQuery = @"UPDATE WellLocation SET Location = @loc, Country = @country, Basin = @basin, 
                              State = @state, Block = @block, Latitud = @lat, Longitud = @lon 
                              WHERE idW = @idW";
            _db.ExecuteNonQuery(locQuery,
                new SqlParameter("@idW", well.Id),
                new SqlParameter("@loc", well.Location ?? (object)DBNull.Value),
                new SqlParameter("@country", well.Country ?? (object)DBNull.Value),
                new SqlParameter("@basin", well.Basin ?? (object)DBNull.Value),
                new SqlParameter("@state", well.State ?? (object)DBNull.Value),
                new SqlParameter("@block", well.Field ?? (object)DBNull.Value),
                new SqlParameter("@lat", well.Latitude?.ToString() ?? (object)DBNull.Value),
                new SqlParameter("@lon", well.Longitude?.ToString() ?? (object)DBNull.Value));
        }

        public Well? GetWellById(int id)
        {
            string query = @"SELECT w.wellName, i.Operator, i.FluidType, i.Spud_date, 
                                   d.Trajectory, d.Welltype, d.RigName, d.Rigtype, d.Contractor,
                                   l.Location, l.Country, l.Basin, l.State, l.Block, l.Latitud, l.Longitud
                            FROM Well w
                            LEFT JOIN WellInfo i ON w.idW = i.idW
                            LEFT JOIN WellDesign d ON w.idW = d.idW
                            LEFT JOIN WellLocation l ON w.idW = l.idW
                            WHERE w.idW = @id";

            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@id", id));
            if (dt.Rows.Count == 0) return null;

            DataRow dr = dt.Rows[0];
            var well = new Well
            {
                Id = id,
                WellName = dr["wellName"]?.ToString() ?? string.Empty,
                Operator = dr["Operator"]?.ToString() ?? string.Empty,
                LoadFluid = dr["FluidType"]?.ToString() ?? string.Empty,
                SpudDate = dr["Spud_date"] != DBNull.Value ? (DateTime)dr["Spud_date"] : null,
                Trajectory = dr["Trajectory"]?.ToString() ?? string.Empty,
                WellType = dr["Welltype"]?.ToString() ?? string.Empty,
                RigName = dr["RigName"]?.ToString() ?? string.Empty,
                RigType = dr["Rigtype"]?.ToString() ?? string.Empty,
                Contractor = dr["Contractor"]?.ToString() ?? string.Empty,
                Location = dr["Location"]?.ToString() ?? string.Empty,
                Country = dr["Country"]?.ToString() ?? string.Empty,
                Basin = dr["Basin"]?.ToString() ?? string.Empty,
                State = dr["State"]?.ToString() ?? string.Empty,
                Field = dr["Block"]?.ToString() ?? string.Empty
            };

            if (double.TryParse(dr["Latitud"]?.ToString(), out double lat)) well.Latitude = lat;
            if (double.TryParse(dr["Longitud"]?.ToString(), out double lon)) well.Longitude = lon;

            return well;
        }

        public List<Well> GetAllWells()
        {
            string query = @"SELECT w.idW, w.wellName, i.Operator, i.FluidType, i.Spud_date, 
                                   l.Location, l.Basin
                            FROM Well w
                            LEFT JOIN WellInfo i ON w.idW = i.idW
                            LEFT JOIN WellLocation l ON w.idW = l.idW";

            DataTable dt = _db.ExecuteQuery(query);
            var results = new List<Well>();

            foreach (DataRow dr in dt.Rows)
            {
                var well = new Well
                {
                    Id = (int)dr["idW"],
                    WellName = dr["wellName"]?.ToString() ?? string.Empty,
                    Operator = dr["Operator"]?.ToString() ?? string.Empty,
                    LoadFluid = dr["FluidType"]?.ToString() ?? string.Empty,
                    SpudDate = dr["Spud_date"] != DBNull.Value ? (DateTime)dr["Spud_date"] : null,
                    Location = dr["Location"]?.ToString() ?? string.Empty,
                    Basin = dr["Basin"]?.ToString() ?? string.Empty
                };
                results.Add(well);
            }
            return results;
        }

        public void DeleteWell(int id)
        {
            // Delete from all tables (cascade deletes should handle this if configured, but explicit is safer)
            string[] queries = {
                "DELETE FROM WellLocation WHERE idW = @id",
                "DELETE FROM WellDesign WHERE idW = @id",
                "DELETE FROM WellInfo WHERE idW = @id",
                "DELETE FROM Personnel WHERE idRep IN (SELECT idRep FROM Report WHERE idW = @id)",
                "DELETE FROM OperationalDetail WHERE idRep IN (SELECT idRep FROM Report WHERE idW = @id)",
                "DELETE FROM Report WHERE idW = @id",
                "DELETE FROM Well WHERE idW = @id"
            };

            foreach (var q in queries)
            {
                _db.ExecuteNonQuery(q, new SqlParameter("@id", id));
            }
        }
    }
}
