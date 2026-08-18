using Microsoft.Data.Sqlite;
using ProjectReport.Models;
using ProjectReport.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace ProjectReport.Modules.VolumeBalance.Data
{
    internal class VolConfigSystemRepository
    {
        private readonly DatabaseService _db;

        public VolConfigSystemRepository(DatabaseService db)
        {
            _db = db
                ?? throw new ArgumentNullException(nameof(db));
        }

        // ============================================================
        // OBTENER CONFIGURACIÓN DE UN EVENTO
        // ============================================================

        public List<EventFluidSystem> GetByVolumeBalanceEvent(
            int volumeBalanceEventId)
        {
            var result =
                new List<EventFluidSystem>();

            if (volumeBalanceEventId <= 0)
                return result;

            try
            {
                var dt =
                    _db.ExecuteQuery(
                        @"
                        SELECT
                            event_fluid_system_id,
                            volume_balance_event_id,
                            pit_name_id,
                            pit_system_id,
                            fluid_type_id,
                            fluid_sub_type

                        FROM event_fluid_system

                        WHERE volume_balance_event_id =
                              @eventId

                        ORDER BY pit_name_id
                        ",
                        new SqliteParameter(
                            "@eventId",
                            volumeBalanceEventId));

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(
                        new EventFluidSystem
                        {
                            EventFluidSystemId =
                                Convert.ToInt32(
                                    row["event_fluid_system_id"]),

                            VolumeBalanceEventId =
                                Convert.ToInt32(
                                    row["volume_balance_event_id"]),

                            PitNameId =
                                Convert.ToInt32(
                                    row["pit_name_id"]),

                            PitSystemId =
                                Convert.ToInt32(
                                    row["pit_system_id"]),

                            FluidTypeId =
                                row["fluid_type_id"] ==
                                DBNull.Value
                                    ? null
                                    : Convert.ToInt32(
                                        row["fluid_type_id"]),

                            FluidSubType =
                                row["fluid_sub_type"] ==
                                DBNull.Value
                                    ? null
                                    : row["fluid_sub_type"]
                                        ?.ToString()
                        });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"GetByVolumeBalanceEvent ERROR: {ex}");
            }

            return result;
        }

        // ============================================================
        // OBTENER ID DEL EVENTO SIGUIENTE
        // ============================================================

        public int? GetNextVolumeBalanceEventId(
            int currentVolumeBalanceEventId)
        {
            if (currentVolumeBalanceEventId <= 0)
            {
                Debug.WriteLine(
                    "GetNextVolumeBalanceEventId CANCELADO: " +
                    "CurrentVolumeBalanceEventId inválido.");

                return null;
            }

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

                        ORDER BY
                            volume_balance_event_id ASC

                        LIMIT 1
                        ",
                        new SqliteParameter(
                            "@currentEventId",
                            currentVolumeBalanceEventId));

                if (dt.Rows.Count == 0)
                {
                    Debug.WriteLine(
                        $"GetNextVolumeBalanceEventId: " +
                        $"No existe evento siguiente para " +
                        $"Event={currentVolumeBalanceEventId}");

                    return null;
                }

                if (dt.Rows[0][
                        "volume_balance_event_id"] ==
                    DBNull.Value)
                {
                    return null;
                }

                int nextEventId =
                    Convert.ToInt32(
                        dt.Rows[0][
                            "volume_balance_event_id"]);

                Debug.WriteLine(
                    $"GetNextVolumeBalanceEventId OK | " +
                    $"CurrentEvent={currentVolumeBalanceEventId} | " +
                    $"NextEvent={nextEventId}");

                return nextEventId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"GetNextVolumeBalanceEventId ERROR: {ex}");

                return null;
            }
        }

        // ============================================================
        // OBTENER EVENT_FLUID_SYSTEM_ID DEL MISMO PIT
        // EN UN EVENTO ESPECÍFICO
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
                {
                    Debug.WriteLine(
                        $"GetEventFluidSystemId: " +
                        $"No encontrado | " +
                        $"Event={volumeBalanceEventId} | " +
                        $"Pit={pitNameId}");

                    return null;
                }

                if (dt.Rows[0][
                        "event_fluid_system_id"] ==
                    DBNull.Value)
                {
                    return null;
                }

                int eventFluidSystemId =
                    Convert.ToInt32(
                        dt.Rows[0][
                            "event_fluid_system_id"]);

                Debug.WriteLine(
                    $"GetEventFluidSystemId OK | " +
                    $"Event={volumeBalanceEventId} | " +
                    $"Pit={pitNameId} | " +
                    $"EFS={eventFluidSystemId}");

                return eventFluidSystemId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"GetEventFluidSystemId ERROR: {ex}");

                return null;
            }
        }

        // ============================================================
        // GUARDAR / ACTUALIZAR CONFIGURACIÓN
        // ============================================================

        public bool Upsert(
            int volumeBalanceEventId,
            int pitNameId,
            int pitSystemId,
            int? fluidTypeId,
            string? fluidSubType)
        {
            if (volumeBalanceEventId <= 0)
            {
                Debug.WriteLine(
                    "Upsert CANCELADO: " +
                    "VolumeBalanceEventId inválido.");

                return false;
            }

            if (pitNameId <= 0)
            {
                Debug.WriteLine(
                    "Upsert CANCELADO: " +
                    "PitNameId inválido.");

                return false;
            }

            if (pitSystemId <= 0)
            {
                Debug.WriteLine(
                    "Upsert CANCELADO: " +
                    "PitSystemId inválido.");

                return false;
            }

            try
            {
                var affected =
                    _db.ExecuteNonQuery(
                        @"
                        INSERT INTO event_fluid_system
                        (
                            volume_balance_event_id,
                            pit_name_id,
                            pit_system_id,
                            fluid_type_id,
                            fluid_sub_type
                        )

                        VALUES
                        (
                            @eventId,
                            @pitId,
                            @systemId,
                            @fluidTypeId,
                            @fluidSubType
                        )

                        ON CONFLICT (
                            volume_balance_event_id,
                            pit_name_id
                        )

                        DO UPDATE SET

                            pit_system_id =
                                excluded.pit_system_id,

                            fluid_type_id =
                                excluded.fluid_type_id,

                            fluid_sub_type =
                                excluded.fluid_sub_type
                        ",
                        new SqliteParameter(
                            "@eventId",
                            volumeBalanceEventId),

                        new SqliteParameter(
                            "@pitId",
                            pitNameId),

                        new SqliteParameter(
                            "@systemId",
                            pitSystemId),

                        new SqliteParameter(
                            "@fluidTypeId",
                            fluidTypeId.HasValue
                                ? (object)
                                    fluidTypeId.Value
                                : DBNull.Value),

                        new SqliteParameter(
                            "@fluidSubType",
                            string.IsNullOrWhiteSpace(
                                fluidSubType)
                                ? (object)
                                    DBNull.Value
                                : fluidSubType));

                Debug.WriteLine(
                    $"event_fluid_system UPSERT OK | " +
                    $"Event={volumeBalanceEventId} | " +
                    $"Pit={pitNameId} | " +
                    $"System={pitSystemId} | " +
                    $"Affected={affected}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"event_fluid_system UPSERT ERROR: {ex}");

                return false;
            }
        }

        // ============================================================
        // COPIAR CONFIGURACIÓN DEL EVENTO ANTERIOR
        // ============================================================

        public bool CopyConfigurationFromPreviousEvent(
            int previousVolumeBalanceEventId,
            int newVolumeBalanceEventId)
        {
            if (previousVolumeBalanceEventId <= 0)
            {
                Debug.WriteLine(
                    "CopyConfiguration CANCELADO: " +
                    "PreviousVolumeBalanceEventId inválido.");

                return false;
            }

            if (newVolumeBalanceEventId <= 0)
            {
                Debug.WriteLine(
                    "CopyConfiguration CANCELADO: " +
                    "NewVolumeBalanceEventId inválido.");

                return false;
            }

            if (previousVolumeBalanceEventId ==
                newVolumeBalanceEventId)
            {
                Debug.WriteLine(
                    "CopyConfiguration CANCELADO: " +
                    "El evento anterior y el nuevo " +
                    "son iguales.");

                return false;
            }

            try
            {
                var previousConfiguration =
                    GetByVolumeBalanceEvent(
                        previousVolumeBalanceEventId);

                if (previousConfiguration.Count == 0)
                {
                    Debug.WriteLine(
                        $"CopyConfiguration: " +
                        $"El evento anterior " +
                        $"{previousVolumeBalanceEventId} " +
                        $"no tiene configuración para copiar.");

                    return true;
                }

                foreach (
                    var previousSystem
                    in previousConfiguration)
                {
                    var success =
                        Upsert(
                            newVolumeBalanceEventId,

                            previousSystem.PitNameId,

                            previousSystem.PitSystemId,

                            previousSystem.FluidTypeId,

                            previousSystem.FluidSubType);

                    if (!success)
                    {
                        Debug.WriteLine(
                            $"CopyConfiguration ERROR: " +
                            $"No se pudo copiar Pit=" +
                            $"{previousSystem.PitNameId} " +
                            $"desde Event=" +
                            $"{previousVolumeBalanceEventId} " +
                            $"hacia Event=" +
                            $"{newVolumeBalanceEventId}");

                        return false;
                    }
                }

                Debug.WriteLine(
                    $"CopyConfiguration OK | " +
                    $"PreviousEvent=" +
                    $"{previousVolumeBalanceEventId} | " +
                    $"NewEvent=" +
                    $"{newVolumeBalanceEventId} | " +
                    $"Records=" +
                    $"{previousConfiguration.Count}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"CopyConfiguration ERROR: {ex}");

                return false;
            }
        }

        // ============================================================
        // ELIMINAR CONFIGURACIÓN DE UN EVENTO
        // ============================================================

        public bool DeleteByEvent(
            int volumeBalanceEventId)
        {
            if (volumeBalanceEventId <= 0)
                return false;

            try
            {
                _db.ExecuteNonQuery(
                    @"
                    DELETE FROM event_fluid_system

                    WHERE volume_balance_event_id =
                          @eventId
                    ",
                    new SqliteParameter(
                        "@eventId",
                        volumeBalanceEventId));

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"DeleteByEvent ERROR: {ex}");

                return false;
            }
        }
    }
}