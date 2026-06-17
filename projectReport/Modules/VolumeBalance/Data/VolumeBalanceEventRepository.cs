using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Services;

namespace ProjectReport.Modules.VolumeBalance.Data
{
    public class VolumeBalanceEventRepository
    {
        private readonly DatabaseService _db;

        public VolumeBalanceEventRepository()
        {
            _db = new DatabaseService();
        }

        public List<VolumeBalanceEvent> GetAllByWell(int idW)
        {
            var events = new List<VolumeBalanceEvent>();

            DataTable table = _db.ExecuteQuery(@"
                SELECT
                    id,
                    event_time,
                    description,
                    current_depth,
                    activity,
                    idW
                FROM VolumeBalanceEvent
                WHERE idW = @idW
                ORDER BY id DESC",
                new SqliteParameter("@idW", idW)
            );

            foreach (DataRow row in table.Rows)
            {
                events.Add(new VolumeBalanceEvent
                {
                    Id = Convert.ToInt32(row["id"]),
                    EventTime = row["event_time"]?.ToString(),
                    Description = row["description"]?.ToString(),
                    CurrentDepth = row["current_depth"] == DBNull.Value
                        ? 0
                        : Convert.ToDouble(row["current_depth"]),
                    Activity = row["activity"]?.ToString(),
                    IdW = Convert.ToInt32(row["idW"])
                });
            }

            return events;
        }

        // 🔹 INSERT (SIEMPRE asociado al well)
        public int Insert(VolumeBalanceEvent evento)
        {
            return _db.ExecuteInsertAndGetId(@"
                INSERT INTO VolumeBalanceEvent
                (
                    event_time,
                    description,
                    current_depth,
                    activity,
                    idW
                )
                VALUES
                (
                    @event_time,
                    @description,
                    @current_depth,
                    @activity,
                    @idW
                )",
                new SqliteParameter("@event_time", evento.EventTime),
                new SqliteParameter("@description", evento.Description ?? ""),
                new SqliteParameter("@current_depth", evento.CurrentDepth),
                new SqliteParameter("@activity", evento.Activity ?? ""),
                new SqliteParameter("@idW", evento.IdW)
            );
        }

        public void Update(VolumeBalanceEvent evento)
        {
            _db.ExecuteNonQuery(@"
                UPDATE VolumeBalanceEvent
                SET
                    description = @description,
                    current_depth = @current_depth,
                    activity = @activity
                WHERE id = @id AND idW = @idW",
                new SqliteParameter("@description", evento.Description ?? ""),
                new SqliteParameter("@current_depth", evento.CurrentDepth),
                new SqliteParameter("@activity", evento.Activity ?? ""),
                new SqliteParameter("@id", evento.Id),
                new SqliteParameter("@idW", evento.IdW)
            );
        }

        public void Delete(int id, int idW)
        {
            _db.ExecuteNonQuery(@"
                DELETE FROM VolumeBalanceEvent
                WHERE id = @id AND idW = @idW",
                new SqliteParameter("@id", id),
                new SqliteParameter("@idW", idW)
            );
        }
    }
}