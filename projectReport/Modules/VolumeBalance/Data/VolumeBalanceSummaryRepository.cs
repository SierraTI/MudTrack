using Microsoft.Data.Sqlite;
using ProjectReport.Services;
using System;
using System.Data;

namespace ProjectReport.Modules.VolumeBalance.Data
{
    public class VolumeBalanceSummaryRepository
    {
        private readonly DatabaseService _db;

        public VolumeBalanceSummaryRepository(
            DatabaseService db)
        {
            _db = db;
        }

        // ============================================================
        // PREVIOUS VOLUME
        // ============================================================

        public (double Active, double Reserve, double Other)
            GetPreviousVolumesByEvent(
                int volumeBalanceEventId)
        {
            return GetVolumesByPitSystem(
                volumeBalanceEventId,
                "previous_volume");
        }

        // ============================================================
        // CURRENT VOLUME
        // ============================================================

        public (double Active, double Reserve, double Other)
            GetCurrentVolumesByEvent(
                int volumeBalanceEventId)
        {
            return GetVolumesByPitSystem(
                volumeBalanceEventId,
                "current_volume");
        }

        // ============================================================
        // CONSULTA BASE
        // ============================================================

        private (double Active, double Reserve, double Other)
            GetVolumesByPitSystem(
                int volumeBalanceEventId,
                string volumeColumn)
        {
            double active = 0;
            double reserve = 0;
            double other = 0;

            if (volumeBalanceEventId <= 0)
                return (active, reserve, other);

            if (volumeColumn != "previous_volume" &&
                volumeColumn != "current_volume")
            {
                throw new ArgumentException(
                    $"Columna de volumen no válida: {volumeColumn}",
                    nameof(volumeColumn));
            }

            string sql = $@"
                SELECT
                    ps.name AS pit_system,

                    COALESCE(
                        SUM(v.{volumeColumn}),
                        0
                    ) AS total_volume

                FROM vol_system v

                INNER JOIN event_fluid_system efs
                    ON efs.event_fluid_system_id =
                       v.event_fluid_system_id

                INNER JOIN pit_system_options ps
                    ON ps.pit_system_id =
                       efs.pit_system_id

                WHERE efs.volume_balance_event_id =
                      @eventId

                GROUP BY
                    ps.pit_system_id,
                    ps.name
            ";

            var dt =
                _db.ExecuteQuery(
                    sql,
                    new SqliteParameter(
                        "@eventId",
                        volumeBalanceEventId));

            foreach (DataRow row in dt.Rows)
            {
                string system =
                    row["pit_system"]?.ToString()
                    ?? string.Empty;

                double volume =
                    row["total_volume"] == DBNull.Value
                        ? 0
                        : Convert.ToDouble(
                            row["total_volume"]);

                switch (system)
                {
                    case "Active":
                        active += volume;
                        break;

                    case "Reserve":
                        reserve += volume;
                        break;

                    case "Other":
                        other += volume;
                        break;
                }
            }

            return (
                active,
                reserve,
                other);
        }
    }
}