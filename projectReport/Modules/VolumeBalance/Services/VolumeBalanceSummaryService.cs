using ProjectReport.Modules.VolumeBalance.Data;
using ProjectReport.Modules.VolumeBalance.Models;
using System;
using System.Linq;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class VolumeBalanceSummaryService
    {
        private readonly VolumeBalanceSummaryRepository _repository;

        public VolumeBalanceSummaryService(
            VolumeBalanceSummaryRepository repository)
        {
            _repository = repository;
        }

        // ============================================================
        // ACTUALIZAR RESUMEN DEL EVENTO
        // ============================================================

        public void RefreshEventSummary(
            int eventId,
            VolumeInfoTable volumeTable)
        {
            if (eventId <= 0)
                return;

            if (volumeTable == null)
                return;

            // ========================================================
            // PREVIOUS EVENT - FINAL VOLUME
            // ========================================================

            var previous =
                _repository.GetPreviousVolumesByEvent(
                    eventId);

            SetRow(
                volumeTable,
                "Previous Event - Final Volume",
                previous);

            // ========================================================
            // EVENT - END VOLUME
            // ========================================================

            var current =
                _repository.GetCurrentVolumesByEvent(
                    eventId);

            SetRow(
                volumeTable,
                "Event - End Volume",
                current);

            // ========================================================
            // LOS DEMÁS CÁLCULOS TODAVÍA NO ESTÁN IMPLEMENTADOS
            // ========================================================
            //
            // Current Event - Total Fluid Additions
            // Event - Water Additions
            // Event - Oil-Based Additions
            // Event - Chemical Additions
            // Event - Total Fluid Losses
            // Event - Additional Fluid Volume
            // BALANCE VOLUME
            //
            // Se implementarán posteriormente.
        }

        // ============================================================
        // ASIGNAR VALORES A UNA FILA
        // ============================================================

        private void SetRow(
            VolumeInfoTable volumeTable,
            string label,
            (
                double Active,
                double Reserve,
                double Other
            ) values)
        {
            var row =
                volumeTable
                    .VolumeInformation
                    .FirstOrDefault(
                        x =>
                            string.Equals(
                                x.Label,
                                label,
                                StringComparison.Ordinal));

            if (row == null)
                return;

            row.Active = values.Active;
            row.Reserve = values.Reserve;
            row.Other = values.Other;
        }
    }
}