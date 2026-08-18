using ProjectReport.Models;
using ProjectReport.Modules.VolumeBalance.Data;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.Services;
using System;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolumeInfoTableViewModel : IDisposable
    {
        // ============================================================
        // DEPENDENCIAS
        // ============================================================

        private readonly DatabaseService _db;

        private readonly PitSystemOptionRepository
            _pitSystemOptionRepository;

        private readonly VolumeBalanceSummaryRepository
            _summaryRepository;

        private readonly VolumeBalanceSummaryService
            _summaryService;

        // ============================================================
        // VIEWMODEL DE VOLUME SYSTEM
        // ============================================================

        private VolSystemViewModel? _volSystemViewModel;

        // ============================================================
        // TABLA
        // ============================================================

        public VolumeInfoTable VolumeTable { get; }

        // ============================================================
        // EVENT ID
        // ============================================================

        private int _volumeBalanceEventId;

        public int VolumeBalanceEventId
        {
            get => _volumeBalanceEventId;

            set
            {
                if (_volumeBalanceEventId == value)
                    return;

                _volumeBalanceEventId = value;

                // ----------------------------------------------------
                // EVENTO INVALIDO
                // ----------------------------------------------------

                if (_volumeBalanceEventId <= 0)
                {
                    ClearAllValues();
                    return;
                }

                // ----------------------------------------------------
                // EVENTO VALIDO
                // ----------------------------------------------------

                RefreshVolumeTotals();
            }
        }

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public VolumeInfoTableViewModel()
        {
            _db =
                new DatabaseService();

            _pitSystemOptionRepository =
                new PitSystemOptionRepository();

            _summaryRepository =
                new VolumeBalanceSummaryRepository(
                    _db);

            _summaryService =
                new VolumeBalanceSummaryService(
                    _summaryRepository);

            VolumeTable =
                new VolumeInfoTable();

            LoadPitSystemOptions();

            LoadDefaultRows();
        }

        // ============================================================
        // ASIGNAR EVENT ID
        // ============================================================

        public void SetEventId(
            int volumeBalanceEventId)
        {
            VolumeBalanceEventId =
                volumeBalanceEventId;
        }

        // ============================================================
        // CONECTAR VOL SYSTEM VIEWMODEL
        // ============================================================

        public void AttachVolumeSystemViewModel(
            VolSystemViewModel volSystemViewModel)
        {
            if (volSystemViewModel == null)
                return;

            // --------------------------------------------------------
            // Si ya estamos conectados al mismo ViewModel
            // no volver a suscribirnos
            // --------------------------------------------------------

            if (ReferenceEquals(
                    _volSystemViewModel,
                    volSystemViewModel))
            {
                // Asegurar que el EventId esté sincronizado
                if (VolumeBalanceEventId !=
                    volSystemViewModel.VolumeBalanceEventId)
                {
                    VolumeBalanceEventId =
                        volSystemViewModel.VolumeBalanceEventId;
                }
                else
                {
                    RefreshVolumeTotals();
                }

                return;
            }

            // --------------------------------------------------------
            // Desconectar ViewModel anterior
            // --------------------------------------------------------

            DetachVolumeSystemViewModel();

            // --------------------------------------------------------
            // Guardar nuevo ViewModel
            // --------------------------------------------------------

            _volSystemViewModel =
                volSystemViewModel;

            // --------------------------------------------------------
            // Suscribirse a cambios
            // --------------------------------------------------------

            _volSystemViewModel.VolumeChanged +=
                OnVolumeChanged;

            // --------------------------------------------------------
            // SINCRONIZAR EVENT ID
            // --------------------------------------------------------

            VolumeBalanceEventId =
                _volSystemViewModel.VolumeBalanceEventId;

            // --------------------------------------------------------
            // REFRESCO INICIAL
            // --------------------------------------------------------

            if (VolumeBalanceEventId > 0)
            {
                RefreshVolumeTotals();
            }
        }

        // ============================================================
        // DESCONECTAR VOL SYSTEM VIEWMODEL
        // ============================================================

        private void DetachVolumeSystemViewModel()
        {
            if (_volSystemViewModel == null)
                return;

            _volSystemViewModel.VolumeChanged -=
                OnVolumeChanged;

            _volSystemViewModel = null;
        }

        // ============================================================
        // CAMBIO DE VOLUMEN / CONFIGURACION
        // ============================================================

        private void OnVolumeChanged(
            object? sender,
            VolumeChangedEventArgs e)
        {
            // --------------------------------------------------------
            // Validar EventId
            // --------------------------------------------------------

            if (e.VolumeBalanceEventId <= 0)
                return;

            // --------------------------------------------------------
            // Ignorar eventos de otro evento
            // --------------------------------------------------------

            if (VolumeBalanceEventId !=
                e.VolumeBalanceEventId)
            {
                return;
            }

            // --------------------------------------------------------
            // RECALCULAR TABLA
            // --------------------------------------------------------

            RefreshVolumeTotals();
        }

        // ============================================================
        // CARGAR OPCIONES DE SISTEMA
        // ============================================================

        private void LoadPitSystemOptions()
        {
            try
            {
                _pitSystemOptionRepository.GetAll();
            }
            catch
            {
                // No impedir que cargue la tabla
            }
        }

        // ============================================================
        // FILAS INICIALES
        // ============================================================

        private void LoadDefaultRows()
        {
            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Previous Event - Final Volume"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Current Event - Total Fluid Additions"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Event - Water Additions"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Event - Oil-Based Additions"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Event - Chemical Additions"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Event - Total Fluid Losses"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Event - End Volume"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "Event - Additional Fluid Volume"
                });

            VolumeTable.VolumeInformation.Add(
                new VolumeBalanceSummaryRow
                {
                    Label =
                        "BALANCE VOLUME"
                });
        }

        // ============================================================
        // RECALCULAR RESUMEN
        // ============================================================

        public void RefreshVolumeTotals()
        {
            if (VolumeBalanceEventId <= 0)
            {
                ClearAllValues();
                return;
            }

            try
            {
                _summaryService.RefreshEventSummary(
                    VolumeBalanceEventId,
                    VolumeTable);
            }
            catch
            {
                // Evitar que un error del resumen
                // rompa toda la interfaz
            }
        }

        // ============================================================
        // LIMPIAR VALORES
        // ============================================================

        private void ClearAllValues()
        {
            if (VolumeTable == null)
                return;

            if (VolumeTable.VolumeInformation == null)
                return;

            foreach (
                var row
                in VolumeTable.VolumeInformation)
            {
                row.Active = 0;
                row.Reserve = 0;
                row.Other = 0;
            }
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            // --------------------------------------------------------
            // Desconectar evento
            // --------------------------------------------------------

            DetachVolumeSystemViewModel();

            // --------------------------------------------------------
            // Liberar DB
            // --------------------------------------------------------

            _db.Dispose();
        }
    }
}