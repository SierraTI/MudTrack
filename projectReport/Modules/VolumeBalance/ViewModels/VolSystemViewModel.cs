using ProjectReport.Core.Data;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Modules.VolumeBalance.Data;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.Services;
using ProjectReport.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolSystemViewModel :
        BaseViewModel,
        IDisposable
    {
        // ============================================================
        // SERVICIOS
        // ============================================================

        private readonly WellContextService _context;

        private readonly VolSystemService _service =
            VolSystemService.Instance;

        private readonly DatabaseService _db;

        private readonly PitSystemOptionRepository
            _pitSystemOptionRepository;

        private readonly VolConfigSystemRepository
            _volConfigSystemRepository;

        private readonly VolSystemRepository
            _volSystemRepository;


        // ============================================================
        // ESTADOS
        // ============================================================

        private bool _isLoading;

        private bool _isAdjustingPitSystems;

        private bool _isUpdatingVolume;

        private bool _isUpdatingDensity;

        private bool _isDisposed;


        // ============================================================
        // SISTEMA ANTERIOR DE CADA PIT
        // ============================================================

        private readonly Dictionary<int, int?>
            _previousPitSystemIds =
                new Dictionary<int, int?>();


        // ============================================================
        // EVENTO DE CAMBIO DE VOLUMEN
        // ============================================================

        public event EventHandler<VolumeChangedEventArgs>?
            VolumeChanged;


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

                OnPropertyChanged(
                    nameof(VolumeBalanceEventId));

                if (_volumeBalanceEventId > 0)
                {
                    LoadPits();
                }
            }
        }


        // ============================================================
        // PITS
        // ============================================================

        private ObservableCollection<VolSystemPit> _pits =
            new ObservableCollection<VolSystemPit>();

        public ObservableCollection<VolSystemPit> Pits
        {
            get => _pits;

            private set
            {
                UnsubscribeFromPits();

                _pits =
                    value ??
                    new ObservableCollection<VolSystemPit>();

                SubscribeToPits();

                OnPropertyChanged(
                    nameof(Pits));
            }
        }


        // ============================================================
        // OPCIONES DE SISTEMA
        // ============================================================

        private List<PitSystemOption> _pitSystemOptions =
            new List<PitSystemOption>();

        public List<PitSystemOption> PitSystemOptions
        {
            get => _pitSystemOptions;

            private set
            {
                _pitSystemOptions =
                    value ??
                    new List<PitSystemOption>();

                OnPropertyChanged(
                    nameof(PitSystemOptions));
            }
        }


        // ============================================================
        // OPCIONES DE FLUIDO
        // ============================================================

        private List<FluidOption> _fluidTypeOptions =
            new List<FluidOption>();

        public List<FluidOption> FluidTypeOptions
        {
            get => _fluidTypeOptions;

            private set
            {
                _fluidTypeOptions =
                    value ??
                    new List<FluidOption>();

                OnPropertyChanged(
                    nameof(FluidTypeOptions));
            }
        }


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public VolSystemViewModel()
        {
            _context =
                WellContextService.Instance;

            _db =
                new DatabaseService();

            _pitSystemOptionRepository =
                new PitSystemOptionRepository();

            _volConfigSystemRepository =
                new VolConfigSystemRepository(
                    _db);

            _volSystemRepository =
                new VolSystemRepository(
                    _db);


            // --------------------------------------------------------
            // EVENTOS DE CONTEXTO
            // --------------------------------------------------------

            _context.WellChanged +=
                OnWellChanged;

            _context.RigProfileUpdated +=
                OnRigProfileUpdated;


            // --------------------------------------------------------
            // CARGAS INICIALES
            // --------------------------------------------------------

            LoadPitSystemOptions();

            LoadPits();
        }


        // ============================================================
        // SUSCRIBIR PITS
        // ============================================================

        private void SubscribeToPits()
        {
            foreach (var pit in _pits)
            {
                if (pit == null)
                    continue;

                pit.PropertyChanged +=
                    OnPitPropertyChanged;
            }
        }


        // ============================================================
        // DESUSCRIBIR PITS
        // ============================================================

        private void UnsubscribeFromPits()
        {
            foreach (var pit in _pits)
            {
                if (pit == null)
                    continue;

                pit.PropertyChanged -=
                    OnPitPropertyChanged;
            }
        }


        // ============================================================
        // CAMBIO DE PROPIEDAD
        // ============================================================

        private void OnPitPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (_isDisposed)
                return;

            if (_isLoading)
                return;

            if (_isAdjustingPitSystems)
                return;

            if (!(sender is VolSystemPit pit))
                return;

            if (VolumeBalanceEventId <= 0)
                return;


            // ========================================================
            // PIT SYSTEM
            // ========================================================

            if (e.PropertyName ==
                nameof(VolSystemPit.PitSystemId))
            {
                HandlePitSystemChanged(
                    pit);

                return;
            }


            // ========================================================
            // FLUID TYPE
            // ========================================================

            if (e.PropertyName ==
                nameof(VolSystemPit.FluidTypeId))
            {
                if (SavePitConfiguration(pit))
                {
                    RaiseVolumeChanged(pit);
                }

                return;
            }


            // ========================================================
            // FLUID SUBTYPE
            // ========================================================

            if (e.PropertyName ==
                nameof(VolSystemPit.FluidSubtype))
            {
                if (SavePitConfiguration(pit))
                {
                    RaiseVolumeChanged(pit);
                }

                return;
            }


            // ========================================================
            // CURRENT VOLUME
            // ========================================================

            if (e.PropertyName ==
                nameof(VolSystemPit.CurrentVolume))
            {
                HandleCurrentVolumeChanged(
                    pit);

                return;
            }


            // ========================================================
            // CURRENT VOLUME TEXT
            // ========================================================

            if (e.PropertyName ==
                nameof(VolSystemPit.CurrentVolumeText))
            {
                // No guardar aquí.
                //
                // CurrentVolumeText solamente convierte
                // el texto a CurrentVolume.
                //
                // El guardado ocurre cuando cambia CurrentVolume.

                return;
            }


            // ========================================================
            // DENSITY
            // ========================================================

            if (e.PropertyName ==
                nameof(VolSystemPit.Density))
            {
                HandleDensityChanged(
                    pit);

                return;
            }


            // ========================================================
            // DENSITY TEXT
            // ========================================================

            if (e.PropertyName ==
                nameof(VolSystemPit.DensityText))
            {
                // El guardado ocurre mediante Density.

                return;
            }
        }


        // ============================================================
        // TRY CHANGE PIT SYSTEM
        //
        // IMPORTANTE:
        //
        // Este método NO modifica el Pit.
        //
        // Solamente valida si el cambio solicitado es permitido.
        //
        // Después:
        //
        //     binding.UpdateSource();
        //
        // modificará PitSystemId.
        //
        // Esto evita la doble modificación y evita ciclos.
        // ============================================================

        public bool TryChangePitSystem(
            VolSystemPit pit,
            int? newSystemId)
        {
            if (pit == null)
                return false;

            if (VolumeBalanceEventId <= 0)
                return false;

            if (!newSystemId.HasValue)
                return false;

            if (newSystemId.Value <= 0)
                return false;


            int? previousSystemId =
                pit.PitSystemId;


            // --------------------------------------------------------
            // No hay cambio
            // --------------------------------------------------------

            if (previousSystemId ==
                newSystemId)
            {
                return true;
            }


            // --------------------------------------------------------
            // Si no había sistema anterior
            //
            // Permitimos la selección.
            //
            // HandlePitSystemChanged() se encargará de registrarlo.
            // --------------------------------------------------------

            if (!previousSystemId.HasValue)
            {
                return true;
            }


            // --------------------------------------------------------
            // Validar configuración resultante
            // --------------------------------------------------------

            bool allowed =
                CanChangePitSystem(
                    pit,
                    previousSystemId.Value,
                    newSystemId.Value);


            if (!allowed)
            {
                ShowMinimumPitSystemsMessage();

                return false;
            }


            return true;
        }


        // ============================================================
        // CURRENT VOLUME CAMBIÓ
        // ============================================================

        private void HandleCurrentVolumeChanged(
            VolSystemPit pit)
        {
            if (pit == null)
                return;

            if (_isUpdatingVolume)
                return;


            _isUpdatingVolume = true;

            try
            {
                // ----------------------------------------------------
                // GUARDAR EVENTO ACTUAL
                // ----------------------------------------------------

                bool saved =
                    SavePitVolumeDataInternal(
                        pit);

                if (!saved)
                    return;


                // ----------------------------------------------------
                // PROPAGAR AL SIGUIENTE EVENTO
                // ----------------------------------------------------

                UpdateNextEventPreviousVolume(
                    pit);


                // ----------------------------------------------------
                // NOTIFICAR EN TIEMPO REAL
                // ----------------------------------------------------

                RaiseVolumeChanged(
                    pit);
            }
            finally
            {
                _isUpdatingVolume = false;
            }
        }


        // ============================================================
        // DENSITY CAMBIÓ
        // ============================================================

        private void HandleDensityChanged(
            VolSystemPit pit)
        {
            if (pit == null)
                return;

            if (_isUpdatingDensity)
                return;


            _isUpdatingDensity = true;

            try
            {
                bool saved =
                    SavePitVolumeDataInternal(
                        pit);

                if (!saved)
                    return;

                RaiseVolumeChanged(
                    pit);
            }
            finally
            {
                _isUpdatingDensity = false;
            }
        }


        // ============================================================
        // PIT SYSTEM CAMBIÓ
        // ============================================================

        private void HandlePitSystemChanged(
            VolSystemPit changedPit)
        {
            if (changedPit == null)
                return;

            if (_isAdjustingPitSystems)
                return;

            if (!changedPit.PitSystemId.HasValue)
                return;

            if (VolumeBalanceEventId <= 0)
                return;


            int newSystemId =
                changedPit.PitSystemId.Value;


            // ========================================================
            // SISTEMA ANTERIOR
            // ========================================================

            int? previousSystemId =
                GetPreviousPitSystemId(
                    changedPit);


            // ========================================================
            // PRIMER REGISTRO
            // ========================================================

            if (!previousSystemId.HasValue)
            {
                bool savedFirst =
                    SavePitConfiguration(
                        changedPit);

                if (savedFirst)
                {
                    TrackPitSystem(
                        changedPit);

                    RaiseVolumeChanged(
                        changedPit);
                }

                return;
            }


            // ========================================================
            // NO CAMBIÓ
            // ========================================================

            if (previousSystemId.Value ==
                newSystemId)
            {
                return;
            }


            // ========================================================
            // VALIDAR
            // ========================================================

            bool canChange =
                CanChangePitSystem(
                    changedPit,
                    previousSystemId.Value,
                    newSystemId);


            if (!canChange)
            {
                RestorePitSystem(
                    changedPit,
                    previousSystemId.Value);

                ShowMinimumPitSystemsMessage();

                return;
            }


            // ========================================================
            // GUARDAR
            // ========================================================

            bool saved =
                SavePitConfiguration(
                    changedPit);


            if (!saved)
            {
                RestorePitSystem(
                    changedPit,
                    previousSystemId.Value);

                MessageBox.Show(
                    "No fue posible guardar el cambio de sistema.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }


            // ========================================================
            // ACTUALIZAR TRACKING
            // ========================================================

            TrackPitSystem(
                changedPit);


            // ========================================================
            // NOTIFICAR
            // ========================================================

            RaiseVolumeChanged(
                changedPit);
        }


        // ============================================================
        // VALIDAR SISTEMA
        // ============================================================

        private bool CanChangePitSystem(
            VolSystemPit changedPit,
            int previousSystemId,
            int newSystemId)
        {
            if (changedPit == null)
                return false;


            if (previousSystemId ==
                newSystemId)
            {
                return true;
            }


            int? activeId =
                GetPitSystemId("Active");

            int? reserveId =
                GetPitSystemId("Reserve");

            int? otherId =
                GetPitSystemId("Other");


            if (!activeId.HasValue ||
                !reserveId.HasValue ||
                !otherId.HasValue)
            {
                return false;
            }


            int activeCount = 0;

            int reserveCount = 0;

            int otherCount = 0;


            foreach (var pit in Pits)
            {
                if (pit == null)
                    continue;


                int? systemId;


                // ----------------------------------------------------
                // Para el Pit que se está modificando utilizamos
                // el nuevo ID hipotético.
                // ----------------------------------------------------

                if (ReferenceEquals(
                    pit,
                    changedPit))
                {
                    systemId =
                        newSystemId;
                }
                else
                {
                    systemId =
                        pit.PitSystemId;
                }


                if (!systemId.HasValue)
                    continue;


                if (systemId.Value ==
                    activeId.Value)
                {
                    activeCount++;
                }
                else if (systemId.Value ==
                         reserveId.Value)
                {
                    reserveCount++;
                }
                else if (systemId.Value ==
                         otherId.Value)
                {
                    otherCount++;
                }
            }


            return
                activeCount >= 1 &&
                reserveCount >= 1 &&
                otherCount >= 1;
        }


        // ============================================================
        // RESTAURAR PIT SYSTEM
        // ============================================================

        private void RestorePitSystem(
            VolSystemPit pit,
            int previousSystemId)
        {
            if (pit == null)
                return;


            _isAdjustingPitSystems = true;

            try
            {
                pit.RestorePitSystemId(
                    previousSystemId);
            }
            finally
            {
                _isAdjustingPitSystems = false;
            }


            _previousPitSystemIds[
                pit.PitId] =
                previousSystemId;
        }


        // ============================================================
        // MENSAJE
        // ============================================================

        private void ShowMinimumPitSystemsMessage()
        {
            MessageBox.Show(
                "No se puede realizar este cambio.\n\n" +
                "Debe existir al menos un Pit de cada sistema:\n\n" +
                "• Active\n" +
                "• Reserve\n" +
                "• Other\n\n" +
                "El cambio dejaría uno de estos sistemas sin ningún Pit.",
                "Configuración de sistemas",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }


        // ============================================================
        // TRACK PIT SYSTEM
        // ============================================================

        private void TrackPitSystem(
            VolSystemPit pit)
        {
            if (pit == null)
                return;


            _previousPitSystemIds[
                pit.PitId] =
                pit.PitSystemId;
        }


        // ============================================================
        // OBTENER SISTEMA ANTERIOR
        // ============================================================

        private int? GetPreviousPitSystemId(
            VolSystemPit pit)
        {
            if (pit == null)
                return null;


            if (_previousPitSystemIds.TryGetValue(
                pit.PitId,
                out int? previous))
            {
                return previous;
            }


            return null;
        }


        // ============================================================
        // OBTENER ID SISTEMA
        // ============================================================

        private int? GetPitSystemId(
            string systemName)
        {
            if (string.IsNullOrWhiteSpace(
                systemName))
            {
                return null;
            }


            return PitSystemOptions
                .FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Name,
                            systemName,
                            StringComparison.OrdinalIgnoreCase))
                ?.PitSystemId;
        }


        // ============================================================
        // CARGAR OPCIONES
        // ============================================================

        private void LoadPitSystemOptions()
        {
            try
            {
                PitSystemOptions =
                    _pitSystemOptionRepository
                        .GetAll();
            }
            catch
            {
                PitSystemOptions =
                    new List<PitSystemOption>();
            }
        }


        // ============================================================
        // WELL CAMBIÓ
        // ============================================================

        private void OnWellChanged(
            object? sender,
            Well? well)
        {
            if (_isDisposed)
                return;

            LoadPits();
        }


        // ============================================================
        // RIG PROFILE CAMBIÓ
        // ============================================================

        private void OnRigProfileUpdated(
            object? sender,
            RigProfileUpdatedEventArgs e)
        {
            if (_isDisposed)
                return;

            LoadPits();
        }


        // ============================================================
        // CARGAR PITS
        // ============================================================

        private void LoadPits()
        {
            if (_isDisposed)
                return;


            try
            {
                _isLoading = true;

                _previousPitSystemIds.Clear();


                var well =
                    _context.CurrentWell;


                if (well == null ||
                    well.Id <= 0)
                {
                    Pits =
                        new ObservableCollection<VolSystemPit>();

                    return;
                }


                var rigRepository =
                    new RigProfileRepository(
                        _db);


                var rigProfile =
                    rigRepository.LoadRigProfile(
                        well.Id);


                if (rigProfile?.Pits == null ||
                    rigProfile.Pits.Count == 0)
                {
                    Pits =
                        new ObservableCollection<VolSystemPit>();

                    return;
                }


                var list =
                    new ObservableCollection<VolSystemPit>();


                foreach (var pit in rigProfile.Pits)
                {
                    list.Add(
                        new VolSystemPit
                        {
                            PitId =
                                pit.Id,

                            PitName =
                                pit.PitName ??
                                string.Empty,

                            EventFluidSystemId =
                                null,

                            PitSystemId =
                                null,

                            FluidTypeId =
                                null,

                            FluidType =
                                string.Empty,

                            FluidSubtype =
                                string.Empty,

                            PreviousVolume =
                                null,

                            CurrentVolume =
                                null,

                            Density =
                                null,

                            PreviousVolumeText =
                                string.Empty,

                            CurrentVolumeText =
                                string.Empty,

                            DensityText =
                                string.Empty,

                            SourcePit =
                                pit
                        });
                }


                Pits = list;


                LoadFluidTypeOptions(
                    well.Id);


                if (VolumeBalanceEventId > 0)
                {
                    LoadConfigurationFromDatabase();
                }
            }
            catch
            {
                Pits =
                    new ObservableCollection<VolSystemPit>();
            }
            finally
            {
                _isLoading = false;
            }
        }


        // ============================================================
        // CARGAR FLUIDOS
        // ============================================================

        private void LoadFluidTypeOptions(
            int wellId)
        {
            try
            {
                FluidTypeOptions =
                    _service
                        .GetFluidOptionsForWell(
                            wellId);
            }
            catch
            {
                FluidTypeOptions =
                    new List<FluidOption>();
            }
        }


        // ============================================================
        // COPIAR CONFIGURACIÓN EVENTO ANTERIOR
        // ============================================================

        public bool CopyConfigurationFromPreviousEvent(
            int previousVolumeBalanceEventId)
        {
            if (previousVolumeBalanceEventId <= 0)
                return false;

            if (VolumeBalanceEventId <= 0)
                return false;

            if (previousVolumeBalanceEventId ==
                VolumeBalanceEventId)
                return false;


            try
            {
                _isLoading = true;


                bool result =
                    _volConfigSystemRepository
                        .CopyConfigurationFromPreviousEvent(
                            previousVolumeBalanceEventId,
                            VolumeBalanceEventId);


                if (!result)
                    return false;


                LoadConfigurationFromDatabase();


                InitializeVolumesFromPreviousEvent(
                    previousVolumeBalanceEventId);


                LoadVolumeDataFromDatabase();


                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _isLoading = false;
            }
        }


        // ============================================================
        // CARGAR CONFIGURACIÓN
        // ============================================================

        private void LoadConfigurationFromDatabase()
        {
            if (VolumeBalanceEventId <= 0)
                return;

            if (Pits.Count == 0)
                return;


            try
            {
                var records =
                    _volConfigSystemRepository
                        .GetByVolumeBalanceEvent(
                            VolumeBalanceEventId);


                if (records == null ||
                    records.Count == 0)
                {
                    InitializeDefaultPitSystems();

                    return;
                }


                _isLoading = true;


                try
                {
                    foreach (var pit in Pits)
                    {
                        var saved =
                            records.FirstOrDefault(
                                x =>
                                    x.PitNameId ==
                                    pit.PitId);


                        if (saved == null)
                            continue;


                        pit.EventFluidSystemId =
                            saved.EventFluidSystemId;


                        pit.RestorePitSystemId(
                            saved.PitSystemId);


                        pit.FluidTypeId =
                            saved.FluidTypeId;


                        if (saved.FluidTypeId.HasValue)
                        {
                            var fluid =
                                FluidTypeOptions
                                    .FirstOrDefault(
                                        x =>
                                            x.Id ==
                                            saved.FluidTypeId.Value);


                            pit.FluidType =
                                fluid?.FluidName ??
                                string.Empty;
                        }
                        else
                        {
                            pit.FluidType =
                                string.Empty;
                        }


                        pit.FluidSubtype =
                            saved.FluidSubType ??
                            string.Empty;


                        pit.MarkDatabaseValuesAsSaved();


                        TrackPitSystem(
                            pit);
                    }
                }
                finally
                {
                    _isLoading = false;
                }


                NormalizePitSystemConfiguration();


                LoadVolumeDataFromDatabase();
            }
            catch
            {
                // No cerrar la vista.
            }
        }


        // ============================================================
        // CARGAR VOLUMEN
        // ============================================================

        private void LoadVolumeDataFromDatabase()
        {
            if (VolumeBalanceEventId <= 0)
                return;

            if (Pits.Count == 0)
                return;


            try
            {
                var records =
                    _volSystemRepository
                        .GetByVolumeBalanceEvent(
                            VolumeBalanceEventId);


                _isLoading = true;


                try
                {
                    foreach (var pit in Pits)
                    {
                        if (!pit.EventFluidSystemId.HasValue)
                            continue;


                        int id =
                            pit.EventFluidSystemId.Value;


                        var saved =
                            records?
                                .FirstOrDefault(
                                    x =>
                                        x.EventFluidSystemId ==
                                        id);


                        if (saved == null)
                        {
                            pit.LoadDatabaseValues(
                                id,
                                null,
                                null,
                                null);

                            continue;
                        }


                        pit.LoadDatabaseValues(
                            saved.EventFluidSystemId,
                            saved.PreviousVolume,
                            saved.CurrentVolume,
                            saved.Density);


                        pit.MarkDatabaseValuesAsSaved();
                    }
                }
                finally
                {
                    _isLoading = false;
                }


                OnPropertyChanged(
                    nameof(Pits));
            }
            catch
            {
            }
        }


        // ============================================================
        // INICIALIZAR VOLUMENES DESDE EVENTO ANTERIOR
        // ============================================================

        private void InitializeVolumesFromPreviousEvent(
            int previousVolumeBalanceEventId)
        {
            if (previousVolumeBalanceEventId <= 0)
                return;

            if (VolumeBalanceEventId <= 0)
                return;

            if (Pits.Count == 0)
                return;


            try
            {
                var previousConfigurations =
                    _volConfigSystemRepository
                        .GetByVolumeBalanceEvent(
                            previousVolumeBalanceEventId);


                if (previousConfigurations == null ||
                    previousConfigurations.Count == 0)
                {
                    return;
                }


                var previousVolumes =
                    _volSystemRepository
                        .GetByVolumeBalanceEvent(
                            previousVolumeBalanceEventId);


                _isLoading = true;


                try
                {
                    foreach (var pit in Pits)
                    {
                        var previousConfiguration =
                            previousConfigurations
                                .FirstOrDefault(
                                    x =>
                                        x.PitNameId ==
                                        pit.PitId);


                        if (previousConfiguration == null)
                            continue;


                        int previousEventFluidSystemId =
                            previousConfiguration
                                .EventFluidSystemId;


                        var previousVolumeRecord =
                            previousVolumes?
                                .FirstOrDefault(
                                    x =>
                                        x.EventFluidSystemId ==
                                        previousEventFluidSystemId);


                        double? previousCurrentVolume =
                            previousVolumeRecord?
                                .CurrentVolume;


                        pit.LoadDatabaseValues(
                            pit.EventFluidSystemId,
                            previousCurrentVolume,
                            null,
                            null);


                        if (pit.EventFluidSystemId.HasValue)
                        {
                            int actualId =
                                pit.EventFluidSystemId.Value;


                            var actualRecord =
                                _volSystemRepository
                                    .GetByEventFluidSystemId(
                                        actualId);


                            if (actualRecord == null)
                            {
                                _volSystemRepository
                                    .CreateEmptyVolumeRecord(
                                        actualId,
                                        previousCurrentVolume);
                            }
                            else
                            {
                                _volSystemRepository
                                    .UpdatePreviousVolume(
                                        actualId,
                                        previousCurrentVolume);
                            }
                        }


                        pit.MarkDatabaseValuesAsSaved();
                    }
                }
                finally
                {
                    _isLoading = false;
                }


                OnPropertyChanged(
                    nameof(Pits));
            }
            catch
            {
            }
        }


        // ============================================================
        // SISTEMAS POR DEFECTO
        // ============================================================

        private void InitializeDefaultPitSystems()
        {
            if (Pits.Count < 3)
                return;


            int? activeId =
                GetPitSystemId("Active");

            int? reserveId =
                GetPitSystemId("Reserve");

            int? otherId =
                GetPitSystemId("Other");


            if (!activeId.HasValue ||
                !reserveId.HasValue ||
                !otherId.HasValue)
            {
                return;
            }


            _isAdjustingPitSystems = true;


            try
            {
                for (int i = 0;
                     i < Pits.Count;
                     i++)
                {
                    var pit =
                        Pits[i];


                    if (i == 0)
                    {
                        pit.RestorePitSystemId(
                            activeId.Value);
                    }
                    else if (i == 1)
                    {
                        pit.RestorePitSystemId(
                            reserveId.Value);
                    }
                    else
                    {
                        pit.RestorePitSystemId(
                            otherId.Value);
                    }


                    pit.FluidTypeId =
                        null;

                    pit.FluidType =
                        string.Empty;

                    pit.FluidSubtype =
                        string.Empty;

                    pit.PreviousVolume =
                        null;

                    pit.CurrentVolume =
                        null;

                    pit.Density =
                        null;

                    pit.PreviousVolumeText =
                        string.Empty;

                    pit.CurrentVolumeText =
                        string.Empty;

                    pit.DensityText =
                        string.Empty;
                }


                foreach (var pit in Pits)
                {
                    SavePitConfiguration(
                        pit);

                    TrackPitSystem(
                        pit);
                }


                foreach (var pit in Pits)
                {
                    pit.MarkDatabaseValuesAsSaved();
                }
            }
            finally
            {
                _isAdjustingPitSystems = false;
            }


            OnPropertyChanged(
                nameof(Pits));
        }


        // ============================================================
        // NORMALIZAR SISTEMAS
        // ============================================================

        private void NormalizePitSystemConfiguration()
        {
            if (Pits.Count < 3)
                return;


            int? activeId =
                GetPitSystemId("Active");

            int? reserveId =
                GetPitSystemId("Reserve");

            int? otherId =
                GetPitSystemId("Other");


            if (!activeId.HasValue ||
                !reserveId.HasValue ||
                !otherId.HasValue)
            {
                return;
            }


            _isAdjustingPitSystems = true;


            try
            {
                EnsureMinimumSystem(
                    activeId.Value,
                    reserveId.Value,
                    otherId.Value);


                EnsureMinimumSystem(
                    reserveId.Value,
                    activeId.Value,
                    otherId.Value);


                EnsureMinimumSystem(
                    otherId.Value,
                    activeId.Value,
                    reserveId.Value);


                foreach (var pit in Pits)
                {
                    if (!pit.PitSystemId.HasValue)
                        continue;


                    SavePitConfiguration(
                        pit);


                    TrackPitSystem(
                        pit);


                    pit.MarkDatabaseValuesAsSaved();
                }
            }
            finally
            {
                _isAdjustingPitSystems = false;
            }


            OnPropertyChanged(
                nameof(Pits));
        }


        // ============================================================
        // GARANTIZAR SISTEMA
        // ============================================================

        private void EnsureMinimumSystem(
            int requiredSystemId,
            int protectedSystemId1,
            int protectedSystemId2)
        {
            if (Pits.Any(
                x =>
                    x.PitSystemId ==
                    requiredSystemId))
            {
                return;
            }


            var groups =
                Pits
                    .Where(
                        x =>
                            x.PitSystemId.HasValue &&
                            x.PitSystemId.Value !=
                                requiredSystemId)
                    .GroupBy(
                        x =>
                            x.PitSystemId.Value)
                    .OrderByDescending(
                        g =>
                            g.Count());


            foreach (var group in groups)
            {
                if (group.Key ==
                    protectedSystemId1 ||
                    group.Key ==
                    protectedSystemId2)
                {
                    if (group.Count() <= 1)
                        continue;
                }


                var replacement =
                    group.FirstOrDefault();


                if (replacement == null)
                    continue;


                replacement.RestorePitSystemId(
                    requiredSystemId);


                return;
            }
        }


        // ============================================================
        // MÉTODO LEGACY
        // ============================================================

        private void AdjustPitSystemConfiguration(
            VolSystemPit changedPit)
        {
            if (changedPit == null)
                return;

            if (!changedPit.PitSystemId.HasValue)
                return;

            if (VolumeBalanceEventId <= 0)
                return;


            SavePitConfiguration(
                changedPit);
        }


        // ============================================================
        // GUARDAR CONFIGURACIÓN
        // ============================================================

        public bool SavePitConfiguration(
            VolSystemPit pit)
        {
            if (pit == null)
                return false;

            if (VolumeBalanceEventId <= 0)
                return false;

            if (pit.PitId <= 0)
                return false;

            if (!pit.PitSystemId.HasValue ||
                pit.PitSystemId.Value <= 0)
            {
                return false;
            }


            bool result =
                _volConfigSystemRepository.Upsert(
                    VolumeBalanceEventId,
                    pit.PitId,
                    pit.PitSystemId.Value,
                    pit.FluidTypeId,
                    pit.FluidSubtype);


            if (!result)
                return false;


            var records =
                _volConfigSystemRepository
                    .GetByVolumeBalanceEvent(
                        VolumeBalanceEventId);


            var record =
                records?
                    .FirstOrDefault(
                        x =>
                            x.PitNameId ==
                            pit.PitId);


            if (record != null)
            {
                pit.EventFluidSystemId =
                    record.EventFluidSystemId;
            }


            return true;
        }


        // ============================================================
        // GUARDAR VOLUMEN
        // ============================================================

        private bool SavePitVolumeDataInternal(
            VolSystemPit pit)
        {
            if (pit == null)
                return false;

            if (VolumeBalanceEventId <= 0)
                return false;

            if (!pit.EventFluidSystemId.HasValue ||
                pit.EventFluidSystemId.Value <= 0)
            {
                return false;
            }


            int eventFluidSystemId =
                pit.EventFluidSystemId.Value;


            bool result =
                _volSystemRepository.Upsert(
                    eventFluidSystemId,
                    pit.PreviousVolume,
                    pit.CurrentVolume,
                    pit.Density,
                    null);


            if (!result)
                return false;


            // --------------------------------------------------------
            // MUY IMPORTANTE
            //
            // NO hacemos:
            //
            // LoadDatabaseValues(...)
            //
            // después de guardar.
            //
            // Eso puede provocar:
            //
            // CurrentVolume
            //     ↓
            // PropertyChanged
            //     ↓
            // Save
            //     ↓
            // LoadDatabaseValues
            //     ↓
            // PropertyChanged
            //     ↓
            // Save
            //
            // y terminar en StackOverflowException.
            // --------------------------------------------------------

            return true;
        }


        // ============================================================
        // MÉTODO PÚBLICO PARA GUARDAR VOLUMEN
        // ============================================================

        public bool SavePitVolumeData(
            VolSystemPit pit)
        {
            if (pit == null)
                return false;


            bool result =
                SavePitVolumeDataInternal(
                    pit);


            if (result)
            {
                RaiseVolumeChanged(
                    pit);
            }


            return result;
        }


        // ============================================================
        // GUARDAR CONFIGURACIÓN COMPLETA
        // ============================================================

        public void SaveConfiguration()
        {
            if (VolumeBalanceEventId <= 0)
                return;


            _isLoading = true;


            try
            {
                foreach (var pit in Pits)
                {
                    if (!pit.PitSystemId.HasValue)
                        continue;


                    SavePitConfiguration(
                        pit);


                    TrackPitSystem(
                        pit);
                }


                foreach (var pit in Pits)
                {
                    SavePitVolumeDataInternal(
                        pit);
                }


                foreach (var pit in Pits)
                {
                    pit.MarkDatabaseValuesAsSaved();
                }
            }
            finally
            {
                _isLoading = false;
            }
        }


        // ============================================================
        // PRIMER EVENTO
        // ============================================================

        public void InitializeFirstEventVolumes()
        {
            if (VolumeBalanceEventId <= 0)
                return;


            _isLoading = true;


            try
            {
                foreach (var pit in Pits)
                {
                    pit.LoadDatabaseValues(
                        pit.EventFluidSystemId,
                        null,
                        null,
                        null);
                }
            }
            finally
            {
                _isLoading = false;
            }


            OnPropertyChanged(
                nameof(Pits));
        }


        // ============================================================
        // PROPAGAR CURRENT AL SIGUIENTE EVENTO
        // ============================================================

        private void UpdateNextEventPreviousVolume(
            VolSystemPit currentPit)
        {
            if (currentPit == null)
                return;

            if (VolumeBalanceEventId <= 0)
                return;

            if (!currentPit.EventFluidSystemId.HasValue)
                return;


            try
            {
                int currentEventFluidSystemId =
                    currentPit.EventFluidSystemId.Value;


                var currentRecord =
                    _volSystemRepository
                        .GetByEventFluidSystemId(
                            currentEventFluidSystemId);


                if (currentRecord == null)
                    return;


                double? currentVolume =
                    currentPit.CurrentVolume;


                int? nextEventId =
                    _volConfigSystemRepository
                        .GetNextVolumeBalanceEventId(
                            VolumeBalanceEventId);


                if (!nextEventId.HasValue ||
                    nextEventId.Value <= 0)
                {
                    return;
                }


                var nextConfigurations =
                    _volConfigSystemRepository
                        .GetByVolumeBalanceEvent(
                            nextEventId.Value);


                if (nextConfigurations == null ||
                    nextConfigurations.Count == 0)
                {
                    return;
                }


                var nextConfiguration =
                    nextConfigurations
                        .FirstOrDefault(
                            x =>
                                x.PitNameId ==
                                currentPit.PitId);


                if (nextConfiguration == null)
                    return;


                int nextEventFluidSystemId =
                    nextConfiguration
                        .EventFluidSystemId;


                if (nextEventFluidSystemId <= 0)
                    return;


                var nextVolume =
                    _volSystemRepository
                        .GetByEventFluidSystemId(
                            nextEventFluidSystemId);


                if (nextVolume == null)
                {
                    _volSystemRepository
                        .CreateEmptyVolumeRecord(
                            nextEventFluidSystemId,
                            currentVolume);

                    return;
                }


                _volSystemRepository
                    .UpdatePreviousVolume(
                        nextEventFluidSystemId,
                        currentVolume);
            }
            catch
            {
                // No impedir el guardado del evento actual.
            }
        }


        // ============================================================
        // NOTIFICAR CAMBIO
        // ============================================================

        private void RaiseVolumeChanged(
            VolSystemPit pit)
        {
            if (pit == null)
                return;

            if (VolumeBalanceEventId <= 0)
                return;


            int eventFluidSystemId =
                pit.EventFluidSystemId ?? 0;


            VolumeChanged?.Invoke(
                this,
                new VolumeChangedEventArgs(
                    VolumeBalanceEventId,
                    eventFluidSystemId,
                    pit.PitId,
                    pit.PreviousVolume,
                    pit.CurrentVolume,
                    pit.Density));
        }


        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            if (_isDisposed)
                return;


            _isDisposed = true;


            UnsubscribeFromPits();


            _context.WellChanged -=
                OnWellChanged;


            _context.RigProfileUpdated -=
                OnRigProfileUpdated;


            _db.Dispose();
        }
    }
}