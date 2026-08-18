using Microsoft.Data.Sqlite;
using ProjectReport.Models;
using ProjectReport.Services;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProjectReport.Modules.VolumeBalance.Data
{
    internal class VolSystemRepository
    {
        private readonly DatabaseService _db;

        public VolSystemRepository(DatabaseService db)
        {
            _db = db
                ?? throw new ArgumentNullException(nameof(db));
        }

        // ============================================================
        // OBTENER POR EVENT_FLUID_SYSTEM_ID
        // ============================================================

        public VolSystem? GetByEventFluidSystemId(
            int eventFluidSystemId)
        {
            if (eventFluidSystemId <= 0)
                return null;

            try
            {
                var dt =
                    _db.ExecuteQuery(
                        @"
                        SELECT
                            vol_system_id,
                            event_fluid_system_id,
                            previous_volume,
                            current_volume,
                            density,
                            remarks

                        FROM vol_system

                        WHERE event_fluid_system_id =
                              @eventFluidSystemId

                        LIMIT 1
                        ",
                        new SqliteParameter(
                            "@eventFluidSystemId",
                            eventFluidSystemId));

                if (dt.Rows.Count == 0)
                    return null;

                return MapRow(dt.Rows[0]);
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // OBTENER TODOS LOS REGISTROS DE UN EVENTO
        // ============================================================

        public List<VolSystem> GetByVolumeBalanceEvent(
            int volumeBalanceEventId)
        {
            var result =
                new List<VolSystem>();

            if (volumeBalanceEventId <= 0)
                return result;

            try
            {
                var dt =
                    _db.ExecuteQuery(
                        @"
                        SELECT
                            v.vol_system_id,
                            v.event_fluid_system_id,
                            v.previous_volume,
                            v.current_volume,
                            v.density,
                            v.remarks

                        FROM vol_system v

                        INNER JOIN event_fluid_system e
                            ON e.event_fluid_system_id =
                               v.event_fluid_system_id

                        WHERE e.volume_balance_event_id =
                              @eventId

                        ORDER BY e.pit_name_id
                        ",
                        new SqliteParameter(
                            "@eventId",
                            volumeBalanceEventId));

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(
                        MapRow(row));
                }
            }
            catch
            {
                return result;
            }

            return result;
        }

        // ============================================================
        // OBTENER EL SIGUIENTE EVENTO
        // ============================================================

        public int? GetNextVolumeBalanceEventId(
            int currentVolumeBalanceEventId)
        {
            if (currentVolumeBalanceEventId <= 0)
                return null;

            try
            {
                var dt =
                    _db.ExecuteQuery(
                        @"
                        SELECT
                            volume_balance_event_id

                        FROM volume_balance_event

                        WHERE volume_balance_event_id >
                              @currentEventId

                        ORDER BY volume_balance_event_id ASC

                        LIMIT 1
                        ",
                        new SqliteParameter(
                            "@currentEventId",
                            currentVolumeBalanceEventId));

                if (dt.Rows.Count == 0)
                    return null;

                return Convert.ToInt32(
                    dt.Rows[0][
                        "volume_balance_event_id"]);
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // OBTENER EVENT_FLUID_SYSTEM_ID
        // DEL MISMO PIT EN OTRO EVENTO
        // ============================================================

        public int? GetEventFluidSystemId(
            int volumeBalanceEventId,
            int pitNameId)
        {
            if (volumeBalanceEventId <= 0)
                return null;

            if (pitNameId <= 0)
                return null;

            try
            {
                var dt =
                    _db.ExecuteQuery(
                        @"
                        SELECT
                            event_fluid_system_id

                        FROM event_fluid_system

                        WHERE volume_balance_event_id =
                              @eventId

                          AND pit_name_id =
                              @pitNameId

                        LIMIT 1
                        ",
                        new SqliteParameter(
                            "@eventId",
                            volumeBalanceEventId),

                        new SqliteParameter(
                            "@pitNameId",
                            pitNameId));

                if (dt.Rows.Count == 0)
                    return null;

                return Convert.ToInt32(
                    dt.Rows[0][
                        "event_fluid_system_id"]);
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // OBTENER CURRENT REAL DESDE LA BASE DE DATOS
        // ============================================================
        //
        // IMPORTANTE:
        //
        // Este método NO utiliza el objeto VolSystemPit.
        //
        // Lee directamente:
        //
        // event
        // +
        // pit
        // +
        // vol_system
        //
        // Por lo tanto obtiene el valor REAL que actualmente
        // existe en SQLite.
        // ============================================================

        public double? GetCurrentVolumeFromDatabase(
            int volumeBalanceEventId,
            int pitNameId)
        {
            if (volumeBalanceEventId <= 0)
                return null;

            if (pitNameId <= 0)
                return null;

            try
            {
                var dt =
                    _db.ExecuteQuery(
                        @"
                        SELECT
                            v.current_volume

                        FROM vol_system v

                        INNER JOIN event_fluid_system e
                            ON e.event_fluid_system_id =
                               v.event_fluid_system_id

                        WHERE e.volume_balance_event_id =
                              @eventId

                          AND e.pit_name_id =
                              @pitNameId

                        LIMIT 1
                        ",
                        new SqliteParameter(
                            "@eventId",
                            volumeBalanceEventId),

                        new SqliteParameter(
                            "@pitNameId",
                            pitNameId));

                if (dt.Rows.Count == 0)
                    return null;

                if (dt.Rows[0]["current_volume"] ==
                    DBNull.Value)
                {
                    return null;
                }

                return Convert.ToDouble(
                    dt.Rows[0]["current_volume"]);
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // PROPAGAR CURRENT DEL EVENTO ACTUAL
        // AL PREVIOUS DEL SIGUIENTE EVENTO
        // ============================================================
        //
        // ESTA ES LA PARTE IMPORTANTE.
        //
        // NO recibe el CurrentVolume desde la interfaz.
        //
        // Primero consulta SQLite:
        //
        // Evento actual
        //      +
        // Pit actual
        //      ↓
        // vol_system.current_volume
        //
        // Después busca:
        //
        // siguiente evento
        //      +
        // mismo pit
        //
        // Y actualiza:
        //
        // siguiente.vol_system.previous_volume
        //
        // De esta manera siempre utiliza el valor REAL
        // almacenado en la base de datos.
        // ============================================================

        public bool PropagateCurrentToNextEventPrevious(
            int currentVolumeBalanceEventId,
            int pitNameId)
        {
            if (currentVolumeBalanceEventId <= 0)
                return false;

            if (pitNameId <= 0)
                return false;

            try
            {
                // ====================================================
                // OBTENER SIGUIENTE EVENTO
                // ====================================================

                var nextEventId =
                    GetNextVolumeBalanceEventId(
                        currentVolumeBalanceEventId);

                if (!nextEventId.HasValue ||
                    nextEventId.Value <= 0)
                {
                    return false;
                }

                // ====================================================
                // OBTENER CURRENT REAL DESDE SQLITE
                // ====================================================

                var currentVolume =
                    GetCurrentVolumeFromDatabase(
                        currentVolumeBalanceEventId,
                        pitNameId);

                // ====================================================
                // BUSCAR EVENT_FLUID_SYSTEM DEL SIGUIENTE EVENTO
                // PARA EL MISMO PIT
                // ====================================================

                var nextEventFluidSystemId =
                    GetEventFluidSystemId(
                        nextEventId.Value,
                        pitNameId);

                if (!nextEventFluidSystemId.HasValue ||
                    nextEventFluidSystemId.Value <= 0)
                {
                    return false;
                }

                // ====================================================
                // VERIFICAR SI YA EXISTE VOL_SYSTEM
                // ====================================================

                var nextVolume =
                    GetByEventFluidSystemId(
                        nextEventFluidSystemId.Value);

                // ====================================================
                // SI NO EXISTE:
                //
                // Crear:
                //
                // Previous = Current REAL
                // Current = NULL
                // Density = NULL
                // ====================================================

                if (nextVolume == null)
                {
                    return CreateEmptyVolumeRecord(
                        nextEventFluidSystemId.Value,
                        currentVolume);
                }

                // ====================================================
                // SI EXISTE:
                //
                // ACTUALIZAR SOLAMENTE PREVIOUS
                // ====================================================

                return UpdatePreviousVolume(
                    nextEventFluidSystemId.Value,
                    currentVolume);
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // CREAR REGISTRO VACÍO
        // ============================================================

        public bool CreateEmptyVolumeRecord(
            int eventFluidSystemId,
            double? previousVolume)
        {
            if (eventFluidSystemId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    INSERT INTO vol_system
                    (
                        event_fluid_system_id,
                        previous_volume,
                        current_volume,
                        density,
                        remarks
                    )

                    VALUES
                    (
                        @eventFluidSystemId,
                        @previousVolume,
                        NULL,
                        NULL,
                        NULL
                    )

                    ON CONFLICT (
                        event_fluid_system_id
                    )

                    DO UPDATE SET

                        previous_volume =
                            excluded.previous_volume
                    ",
                    new SqliteParameter(
                        "@eventFluidSystemId",
                        eventFluidSystemId),

                    new SqliteParameter(
                        "@previousVolume",
                        previousVolume.HasValue
                            ? (object)previousVolume.Value
                            : DBNull.Value));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // INICIALIZAR VOLUMENES DESDE EVENTO ANTERIOR
        // ============================================================

        public bool InitializeFromPreviousEvent(
            int previousVolumeBalanceEventId,
            int newVolumeBalanceEventId)
        {
            if (previousVolumeBalanceEventId <= 0)
                return false;

            if (newVolumeBalanceEventId <= 0)
                return false;

            if (previousVolumeBalanceEventId ==
                newVolumeBalanceEventId)
            {
                return false;
            }

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    INSERT INTO vol_system
                    (
                        event_fluid_system_id,
                        previous_volume,
                        current_volume,
                        density,
                        remarks
                    )

                    SELECT
                        newEfs.event_fluid_system_id,

                        previousVol.current_volume,

                        NULL,

                        NULL,

                        NULL

                    FROM event_fluid_system previousEfs

                    INNER JOIN event_fluid_system newEfs
                        ON newEfs.volume_balance_event_id =
                           @newEventId

                       AND newEfs.pit_name_id =
                           previousEfs.pit_name_id

                    INNER JOIN vol_system previousVol
                        ON previousVol.event_fluid_system_id =
                           previousEfs.event_fluid_system_id

                    WHERE previousEfs.volume_balance_event_id =
                          @previousEventId

                      AND previousVol.current_volume IS NOT NULL

                    ON CONFLICT (
                        event_fluid_system_id
                    )

                    DO UPDATE SET

                        previous_volume =
                            excluded.previous_volume,

                        current_volume =
                            NULL,

                        density =
                            NULL,

                        remarks =
                            NULL
                    ",
                    new SqliteParameter(
                        "@previousEventId",
                        previousVolumeBalanceEventId),

                    new SqliteParameter(
                        "@newEventId",
                        newVolumeBalanceEventId));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // CREAR / ACTUALIZAR CURRENT Y DENSITY
        // ============================================================

        public bool Upsert(
            int eventFluidSystemId,
            double? previousVolume,
            double? currentVolume,
            double? density,
            string? remarks)
        {
            if (eventFluidSystemId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    INSERT INTO vol_system
                    (
                        event_fluid_system_id,
                        previous_volume,
                        current_volume,
                        density,
                        remarks
                    )

                    VALUES
                    (
                        @eventFluidSystemId,
                        @previousVolume,
                        @currentVolume,
                        @density,
                        @remarks
                    )

                    ON CONFLICT (
                        event_fluid_system_id
                    )

                    DO UPDATE SET

                        current_volume =
                            excluded.current_volume,

                        density =
                            excluded.density,

                        remarks =
                            excluded.remarks
                    ",
                    new SqliteParameter(
                        "@eventFluidSystemId",
                        eventFluidSystemId),

                    new SqliteParameter(
                        "@previousVolume",
                        previousVolume.HasValue
                            ? (object)previousVolume.Value
                            : DBNull.Value),

                    new SqliteParameter(
                        "@currentVolume",
                        currentVolume.HasValue
                            ? (object)currentVolume.Value
                            : DBNull.Value),

                    new SqliteParameter(
                        "@density",
                        density.HasValue
                            ? (object)density.Value
                            : DBNull.Value),

                    new SqliteParameter(
                        "@remarks",
                        string.IsNullOrWhiteSpace(remarks)
                            ? (object)DBNull.Value
                            : remarks));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // ACTUALIZAR SOLAMENTE CURRENT
        // ============================================================

        public bool UpdateCurrentVolume(
            int eventFluidSystemId,
            double? currentVolume)
        {
            if (eventFluidSystemId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    UPDATE vol_system

                    SET current_volume =
                        @currentVolume

                    WHERE event_fluid_system_id =
                          @eventFluidSystemId
                    ",
                    new SqliteParameter(
                        "@eventFluidSystemId",
                        eventFluidSystemId),

                    new SqliteParameter(
                        "@currentVolume",
                        currentVolume.HasValue
                            ? (object)currentVolume.Value
                            : DBNull.Value));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // ACTUALIZAR SOLAMENTE DENSITY
        // ============================================================

        public bool UpdateDensity(
            int eventFluidSystemId,
            double? density)
        {
            if (eventFluidSystemId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    UPDATE vol_system

                    SET density =
                        @density

                    WHERE event_fluid_system_id =
                          @eventFluidSystemId
                    ",
                    new SqliteParameter(
                        "@eventFluidSystemId",
                        eventFluidSystemId),

                    new SqliteParameter(
                        "@density",
                        density.HasValue
                            ? (object)density.Value
                            : DBNull.Value));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // ACTUALIZAR SOLAMENTE PREVIOUS VOLUME
        // ============================================================

        public bool UpdatePreviousVolume(
            int eventFluidSystemId,
            double? previousVolume)
        {
            if (eventFluidSystemId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    UPDATE vol_system

                    SET previous_volume =
                        @previousVolume

                    WHERE event_fluid_system_id =
                          @eventFluidSystemId
                    ",
                    new SqliteParameter(
                        "@eventFluidSystemId",
                        eventFluidSystemId),

                    new SqliteParameter(
                        "@previousVolume",
                        previousVolume.HasValue
                            ? (object)previousVolume.Value
                            : DBNull.Value));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // ELIMINAR POR EVENT_FLUID_SYSTEM
        // ============================================================

        public bool DeleteByEventFluidSystem(
            int eventFluidSystemId)
        {
            if (eventFluidSystemId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    DELETE FROM vol_system

                    WHERE event_fluid_system_id =
                          @eventFluidSystemId
                    ",
                    new SqliteParameter(
                        "@eventFluidSystemId",
                        eventFluidSystemId));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // ELIMINAR TODOS LOS VOLUMENES DE UN EVENTO
        // ============================================================

        public bool DeleteByVolumeBalanceEvent(
            int volumeBalanceEventId)
        {
            if (volumeBalanceEventId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    DELETE FROM vol_system

                    WHERE event_fluid_system_id IN
                    (
                        SELECT
                            event_fluid_system_id

                        FROM event_fluid_system

                        WHERE volume_balance_event_id =
                              @eventId
                    )
                    ",
                    new SqliteParameter(
                        "@eventId",
                        volumeBalanceEventId));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // MAPEAR DATA ROW
        // ============================================================

        private static VolSystem MapRow(
            DataRow row)
        {
            return new VolSystem
            {
                VolSystemId =
                    Convert.ToInt32(
                        row["vol_system_id"]),

                EventFluidSystemId =
                    Convert.ToInt32(
                        row["event_fluid_system_id"]),

                PreviousVolume =
                    row["previous_volume"] == DBNull.Value
                        ? null
                        : Convert.ToDouble(
                            row["previous_volume"]),

                CurrentVolume =
                    row["current_volume"] == DBNull.Value
                        ? null
                        : Convert.ToDouble(
                            row["current_volume"]),

                Density =
                    row["density"] == DBNull.Value
                        ? null
                        : Convert.ToDouble(
                            row["density"]),

                Remarks =
                    row["remarks"] == DBNull.Value
                        ? null
                        : row["remarks"]?.ToString()
            };
        }
    }
}