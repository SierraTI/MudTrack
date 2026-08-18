using Microsoft.Data.Sqlite;
using ProjectReport.Models;
using ProjectReport.Services;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProjectReport.Modules.VolumeBalance.Data
{
    public class EventFluidSystemRepository
    {
        private readonly DatabaseService _db;

        public EventFluidSystemRepository()
        {
            _db = new DatabaseService();
        }


        // ============================================================
        // OBTENER CONFIGURACIONES DE UN EVENTO
        // ============================================================

        public List<EventFluidSystem> GetByEvent(
            int volumeBalanceEventId)
        {
            var result =
                new List<EventFluidSystem>();


            DataTable table =
                _db.ExecuteQuery(@"
                    SELECT
                        event_fluid_system_id,
                        volume_balance_event_id,
                        pit_name_id,
                        pit_system_id,
                        fluid_type_id,
                        fluid_sub_type
                    FROM event_fluid_system
                    WHERE volume_balance_event_id =
                          @volume_balance_event_id
                    ORDER BY event_fluid_system_id ASC",
                    new SqliteParameter(
                        "@volume_balance_event_id",
                        volumeBalanceEventId));


            foreach (DataRow row in table.Rows)
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
                            Convert.ToInt32(
                                row["fluid_type_id"]),

                        FluidSubType =
                            row["fluid_sub_type"]?.ToString()
                            ?? string.Empty
                    });
            }


            return result;
        }


        // ============================================================
        // INSERTAR CONFIGURACIÓN DE PIT
        // ============================================================

        public int Insert(
            EventFluidSystem item)
        {
            return _db.ExecuteInsertAndGetId(@"
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
                    @volume_balance_event_id,
                    @pit_name_id,
                    @pit_system_id,
                    @fluid_type_id,
                    @fluid_sub_type
                )",

                new SqliteParameter(
                    "@volume_balance_event_id",
                    item.VolumeBalanceEventId),

                new SqliteParameter(
                    "@pit_name_id",
                    item.PitNameId),

                new SqliteParameter(
                    "@pit_system_id",
                    item.PitSystemId),

                new SqliteParameter(
                    "@fluid_type_id",
                    item.FluidTypeId),

                new SqliteParameter(
                    "@fluid_sub_type",
                    item.FluidSubType ?? string.Empty)
            );
        }


        // ============================================================
        // ACTUALIZAR CONFIGURACIÓN DE PIT
        // ============================================================

        public void Update(
            EventFluidSystem item)
        {
            _db.ExecuteNonQuery(@"
                UPDATE event_fluid_system
                SET
                    pit_system_id =
                        @pit_system_id,

                    fluid_type_id =
                        @fluid_type_id,

                    fluid_sub_type =
                        @fluid_sub_type

                WHERE event_fluid_system_id =
                      @event_fluid_system_id",

                new SqliteParameter(
                    "@pit_system_id",
                    item.PitSystemId),

                new SqliteParameter(
                    "@fluid_type_id",
                    item.FluidTypeId),

                new SqliteParameter(
                    "@fluid_sub_type",
                    item.FluidSubType ?? string.Empty),

                new SqliteParameter(
                    "@event_fluid_system_id",
                    item.EventFluidSystemId)
            );
        }


        // ============================================================
        // BUSCAR CONFIGURACIÓN DE UN PIT EN UN EVENTO
        // ============================================================

        public EventFluidSystem?
            GetByEventAndPit(
                int volumeBalanceEventId,
                int pitNameId)
        {
            DataTable table =
                _db.ExecuteQuery(@"
                    SELECT
                        event_fluid_system_id,
                        volume_balance_event_id,
                        pit_name_id,
                        pit_system_id,
                        fluid_type_id,
                        fluid_sub_type
                    FROM event_fluid_system
                    WHERE volume_balance_event_id =
                          @volume_balance_event_id

                    AND pit_name_id =
                        @pit_name_id

                    LIMIT 1",

                    new SqliteParameter(
                        "@volume_balance_event_id",
                        volumeBalanceEventId),

                    new SqliteParameter(
                        "@pit_name_id",
                        pitNameId));


            if (table.Rows.Count == 0)
            {
                return null;
            }


            DataRow row =
                table.Rows[0];


            return new EventFluidSystem
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
                    Convert.ToInt32(
                        row["fluid_type_id"]),

                FluidSubType =
                    row["fluid_sub_type"]?.ToString()
                    ?? string.Empty
            };
        }
    }
}