using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using ProjectReport.Models;
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

        // ============================================================
        // OBTENER TODOS LOS EVENTOS DE UN BALANCE
        // ============================================================
        public List<VolumeBalanceEvent> GetAllByVolumeBalance(int volumeBalanceId)
        {
            var events = new List<VolumeBalanceEvent>();

            DataTable table = _db.ExecuteQuery(@"
                SELECT
                    volume_balance_event_id,
                    volume_balance_id,
                    event_no,
                    event_date_time,
                    activity,
                    current_depth,
                    description,
                    remarks,
                    created_by,
                    created_date,
                    modified_by,
                    modified_date
                FROM volume_balance_event
                WHERE volume_balance_id = @volume_balance_id
                ORDER BY event_no ASC",
                new SqliteParameter("@volume_balance_id", volumeBalanceId)
            );

            foreach (DataRow row in table.Rows)
            {
                events.Add(new VolumeBalanceEvent
                {
                    VolumeBalanceEventId = Convert.ToInt32(
                        row["volume_balance_event_id"]),

                    VolumeBalanceId = Convert.ToInt32(
                        row["volume_balance_id"]),

                    EventNo = Convert.ToInt32(
                        row["event_no"]),

                    EventDateTime = Convert.ToDateTime(
                        row["event_date_time"]),

                    Activity = row["activity"]?.ToString() ?? "",

                    CurrentDepth = row["current_depth"] == DBNull.Value
                        ? null
                        : Convert.ToDouble(row["current_depth"]),

                    Description = row["description"]?.ToString() ?? "",

                    Remarks = row["remarks"]?.ToString() ?? "",

                    CreatedBy = row["created_by"]?.ToString() ?? "",

                    CreatedDate = Convert.ToDateTime(
                        row["created_date"]),

                    ModifiedBy = row["modified_by"]?.ToString(),

                    ModifiedDate = row["modified_date"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["modified_date"])
                });
            }

            return events;
        }


        // ============================================================
        // INSERTAR EVENTO
        // ============================================================
        public int Insert(VolumeBalanceEvent evento)
        {
            return _db.ExecuteInsertAndGetId(@"
                INSERT INTO volume_balance_event
                (
                    volume_balance_id,
                    event_no,
                    event_date_time,
                    activity,
                    current_depth,
                    description,
                    remarks,
                    created_by,
                    created_date,
                    modified_by,
                    modified_date
                )
                VALUES
                (
                    @volume_balance_id,
                    @event_no,
                    @event_date_time,
                    @activity,
                    @current_depth,
                    @description,
                    @remarks,
                    @created_by,
                    @created_date,
                    @modified_by,
                    @modified_date
                )",

                new SqliteParameter(
                    "@volume_balance_id",
                    evento.VolumeBalanceId),

                new SqliteParameter(
                    "@event_no",
                    evento.EventNo),

                new SqliteParameter(
                    "@event_date_time",
                    evento.EventDateTime),

                new SqliteParameter(
                    "@activity",
                    evento.Activity ?? ""),

                new SqliteParameter(
                    "@current_depth",
                    evento.CurrentDepth.HasValue
                        ? evento.CurrentDepth.Value
                        : DBNull.Value),

                new SqliteParameter(
                    "@description",
                    evento.Description ?? ""),

                new SqliteParameter(
                    "@remarks",
                    evento.Remarks ?? ""),

                new SqliteParameter(
                    "@created_by",
                    evento.CreatedBy ?? ""),

                new SqliteParameter(
                    "@created_date",
                    evento.CreatedDate),

                new SqliteParameter(
                    "@modified_by",
                    string.IsNullOrEmpty(evento.ModifiedBy)
                        ? DBNull.Value
                        : evento.ModifiedBy),

                new SqliteParameter(
                    "@modified_date",
                    evento.ModifiedDate.HasValue
                        ? evento.ModifiedDate.Value
                        : DBNull.Value)
            );
        }


        // ============================================================
        // ACTUALIZAR EVENTO
        // ============================================================
        public void Update(VolumeBalanceEvent evento)
        {
            _db.ExecuteNonQuery(@"
                UPDATE volume_balance_event
                SET
                    event_no = @event_no,
                    event_date_time = @event_date_time,
                    activity = @activity,
                    current_depth = @current_depth,
                    description = @description,
                    remarks = @remarks,
                    modified_by = @modified_by,
                    modified_date = @modified_date
                WHERE volume_balance_event_id = @volume_balance_event_id
                  AND volume_balance_id = @volume_balance_id",

                new SqliteParameter(
                    "@event_no",
                    evento.EventNo),

                new SqliteParameter(
                    "@event_date_time",
                    evento.EventDateTime),

                new SqliteParameter(
                    "@activity",
                    evento.Activity ?? ""),

                new SqliteParameter(
                    "@current_depth",
                    evento.CurrentDepth.HasValue
                        ? evento.CurrentDepth.Value
                        : DBNull.Value),

                new SqliteParameter(
                    "@description",
                    evento.Description ?? ""),

                new SqliteParameter(
                    "@remarks",
                    evento.Remarks ?? ""),

                new SqliteParameter(
                    "@modified_by",
                    string.IsNullOrEmpty(evento.ModifiedBy)
                        ? DBNull.Value
                        : evento.ModifiedBy),

                new SqliteParameter(
                    "@modified_date",
                    evento.ModifiedDate.HasValue
                        ? evento.ModifiedDate.Value
                        : DBNull.Value),

                new SqliteParameter(
                    "@volume_balance_event_id",
                    evento.VolumeBalanceEventId),

                new SqliteParameter(
                    "@volume_balance_id",
                    evento.VolumeBalanceId)
            );
        }


        // ============================================================
        // ELIMINAR EVENTO
        // ============================================================
        public void Delete(
            int volumeBalanceEventId,
            int volumeBalanceId)
        {
            _db.ExecuteNonQuery(@"
                DELETE FROM volume_balance_event
                WHERE volume_balance_event_id = @volume_balance_event_id
                  AND volume_balance_id = @volume_balance_id",

                new SqliteParameter(
                    "@volume_balance_event_id",
                    volumeBalanceEventId),

                new SqliteParameter(
                    "@volume_balance_id",
                    volumeBalanceId)
            );
        }
     
    }
}
