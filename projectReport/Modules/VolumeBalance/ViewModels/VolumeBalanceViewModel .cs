using ProjectReport.Models;
using ProjectReport.Modules.VolumeBalance.Data;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.ViewModels;
using ProjectReport.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolumeBalanceViewModel : INotifyPropertyChanged
    {
        private readonly VolumeBalanceNavigationService _navigation;
        private readonly VolumeBalanceRepository _volumeBalanceRepository;
        private readonly VolumeBalanceEventRepository _eventRepository;

        // ============================================================
        // DATABASE
        // ============================================================

        private readonly DatabaseService _db;

        // ============================================================
        // REPOSITORIOS
        // ============================================================

        private readonly VolConfigSystemRepository
            _volConfigSystemRepository;

        private readonly VolSystemRepository
            _volSystemRepository;

        // ============================================================
        // ID DEL BALANCE DE VOLUMEN ACTUAL
        // ============================================================

        private readonly int _currentVolumeBalanceId;

        // ============================================================
        // EVENT ID SELECCIONADO / ABIERTO
        // ============================================================

        private int _selectedEventId;

        public int SelectedEventId
        {
            get => _selectedEventId;

            private set
            {
                if (_selectedEventId == value)
                    return;

                _selectedEventId = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // TABLA DE INFORMACIÓN DE VOLUMEN
        // ============================================================

        private VolumeInfoTableViewModel _volumeInfoTableViewModel;

        public VolumeInfoTableViewModel VolumeInfoTableViewModel
        {
            get => _volumeInfoTableViewModel;

            private set
            {
                _volumeInfoTableViewModel = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // EVENTOS
        // ============================================================

        private ObservableCollection<VolumeBalanceEvent> _events =
            new ObservableCollection<VolumeBalanceEvent>();

        public ObservableCollection<VolumeBalanceEvent> Events
        {
            get => _events;

            set
            {
                _events = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // COMANDOS
        // ============================================================

        public ICommand AddEventCommand { get; }

        public ICommand ViewEventCommand { get; }

        public ICommand ExportEventCommand { get; }

        public ICommand DeleteEventCommand { get; }

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public VolumeBalanceViewModel(
            VolumeBalanceNavigationService navigation,
            int wellId,
            string reportDate,
            string shift)
        {
            _navigation =
                navigation
                ?? throw new ArgumentNullException(
                    nameof(navigation));

            _volumeBalanceRepository =
                new VolumeBalanceRepository();

            _eventRepository =
                new VolumeBalanceEventRepository();

            // ========================================================
            // DATABASE
            // ========================================================

            _db =
                new DatabaseService();

            // ========================================================
            // CONFIGURACIÓN DE PITS
            // ========================================================

            _volConfigSystemRepository =
                new VolConfigSystemRepository(
                    _db);

            // ========================================================
            // VOL_SYSTEM
            // ========================================================

            _volSystemRepository =
                new VolSystemRepository(
                    _db);

            // ========================================================
            // OBTENER / CREAR BALANCE
            // ========================================================

            _currentVolumeBalanceId =
                _volumeBalanceRepository.GetOrCreate(
                    wellId,
                    reportDate,
                    shift);

            // ========================================================
            // CARGAR EVENTOS
            // ========================================================

            LoadFromDatabase();

            // ========================================================
            // CREAR VOLUME INFO TABLE VIEWMODEL
            // ========================================================

            VolumeInfoTableViewModel =
                new VolumeInfoTableViewModel();

            // ========================================================
            // IMPORTANTE
            //
            // NO seleccionamos automáticamente el último evento.
            //
            // El evento será seleccionado cuando realmente se abra
            // desde la interfaz.
            // ========================================================

            SelectedEventId = 0;

            VolumeInfoTableViewModel.VolumeBalanceEventId = 0;

            // ========================================================
            // COMANDOS
            // ========================================================

            AddEventCommand =
                new RelayCommand(
                    _ => AddEvent());

            ViewEventCommand =
                new RelayCommand<VolumeBalanceEvent>(
                    ViewEvent);

            ExportEventCommand =
                new RelayCommand<VolumeBalanceEvent>(
                    ExportEvent);

            DeleteEventCommand =
                new RelayCommand<VolumeBalanceEvent>(
                    DeleteEvent);
        }

        // ============================================================
        // SELECCIONAR EVENTO ACTUAL
        // ============================================================
        //
        // Este método es ahora la fuente oficial del EventId que
        // está siendo visualizado.
        //
        // Si el usuario abre:
        //
        // EventNo = 1
        // EventId = 1
        //
        // entonces:
        //
        // SelectedEventId = 1
        // VolumeInfoTableViewModel.EventId = 1
        //
        // No importa que exista EventId = 11.
        //
        // ============================================================

        public void SetCurrentEvent(
            VolumeBalanceEvent evento)
        {
            if (evento == null)
                return;

            if (evento.VolumeBalanceId !=
                _currentVolumeBalanceId)
                return;

            if (evento.VolumeBalanceEventId <= 0)
                return;

            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "[VolumeBalanceVM] SELECCIONANDO EVENTO");

            Debug.WriteLine(
                $"EventNo = {evento.EventNo}");

            Debug.WriteLine(
                $"EventId = {evento.VolumeBalanceEventId}");

            Debug.WriteLine(
                $"VolumeBalanceId = {evento.VolumeBalanceId}");

            Debug.WriteLine(
                "========================================");

            // ========================================================
            // GUARDAR EVENT ID SELECCIONADO
            // ========================================================

            SelectedEventId =
                evento.VolumeBalanceEventId;

            // ========================================================
            // ENVIAR EVENT ID A VOLUME INFO TABLE
            // ========================================================

            if (VolumeInfoTableViewModel != null)
            {
                Debug.WriteLine(
                    "[VolumeBalanceVM] " +
                    "Asignando EventId a VolumeInfoTable");

                Debug.WriteLine(
                    $"EventId = {SelectedEventId}");

                VolumeInfoTableViewModel
                    .VolumeBalanceEventId =
                        SelectedEventId;
            }
        }

        // ============================================================
        // RECARGAR EVENTOS
        // ============================================================

        public void Refresh()
        {
            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "[VolumeBalanceVM] REFRESH");

            Debug.WriteLine(
                $"EventId seleccionado = {SelectedEventId}");

            Debug.WriteLine(
                "========================================");

            LoadFromDatabase();

            // ========================================================
            // IMPORTANTE
            //
            // NO buscamos el último evento.
            //
            // Intentamos mantener el evento que el usuario estaba
            // viendo.
            // ========================================================

            if (SelectedEventId > 0)
            {
                var selectedEvent =
                    Events.FirstOrDefault(
                        e =>
                            e.VolumeBalanceEventId ==
                            SelectedEventId);

                if (selectedEvent != null)
                {
                    Debug.WriteLine(
                        "[VolumeBalanceVM] " +
                        "Manteniendo evento seleccionado");

                    Debug.WriteLine(
                        $"EventNo = {selectedEvent.EventNo}");

                    Debug.WriteLine(
                        $"EventId = " +
                        $"{selectedEvent.VolumeBalanceEventId}");

                    if (VolumeInfoTableViewModel != null)
                    {
                        VolumeInfoTableViewModel
                            .VolumeBalanceEventId =
                                SelectedEventId;
                    }

                    return;
                }
            }

            // ========================================================
            // SI EL EVENTO YA NO EXISTE
            //
            // Seleccionar un evento válido solamente si es necesario.
            // ========================================================

            if (Events.Count > 0)
            {
                var fallbackEvent =
                    Events
                        .OrderByDescending(
                            e => e.EventNo)
                        .FirstOrDefault();

                if (fallbackEvent != null)
                {
                    SetCurrentEvent(
                        fallbackEvent);
                }
            }
            else
            {
                SelectedEventId = 0;

                if (VolumeInfoTableViewModel != null)
                {
                    VolumeInfoTableViewModel
                        .VolumeBalanceEventId = 0;
                }
            }
        }

        // ============================================================
        // CARGAR EVENTOS DESDE DATABASE
        // ============================================================

        private void LoadFromDatabase()
        {
            var data =
                _eventRepository.GetAllByVolumeBalance(
                    _currentVolumeBalanceId);

            Events =
                new ObservableCollection<VolumeBalanceEvent>(
                    data);

            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "[VolumeBalanceVM] EVENTOS CARGADOS");

            Debug.WriteLine(
                $"Cantidad = {Events.Count}");

            foreach (var evento in Events)
            {
                Debug.WriteLine(
                    $"EventNo = {evento.EventNo} | " +
                    $"EventId = {evento.VolumeBalanceEventId}");
            }

            Debug.WriteLine(
                "========================================");
        }

        // ============================================================
        // CREAR NUEVO EVENTO
        // ============================================================

        private void AddEvent()
        {
            int nextEventNo =
                Events.Count > 0
                    ? Events.Max(e => e.EventNo) + 1
                    : 1;

            // ========================================================
            // BUSCAR EVENTO ANTERIOR
            // ========================================================

            var previousEvent =
                Events
                    .OrderByDescending(
                        e => e.EventNo)
                    .FirstOrDefault();

            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "CREANDO NUEVO EVENTO");

            Debug.WriteLine(
                $"Nuevo EventNo = {nextEventNo}");

            if (previousEvent != null)
            {
                Debug.WriteLine(
                    $"Evento anterior = " +
                    $"{previousEvent.VolumeBalanceEventId}");
            }
            else
            {
                Debug.WriteLine(
                    "No existe evento anterior.");
            }

            Debug.WriteLine(
                "========================================");

            // ========================================================
            // CREAR EVENTO
            // ========================================================

            var evento =
                new VolumeBalanceEvent
                {
                    VolumeBalanceId =
                        _currentVolumeBalanceId,

                    EventNo =
                        nextEventNo,

                    EventDateTime =
                        DateTime.Now,

                    Description =
                        string.Empty,

                    CurrentDepth =
                        null,

                    Activity =
                        string.Empty,

                    Remarks =
                        string.Empty,

                    CreatedBy =
                        Environment.UserName,

                    CreatedDate =
                        DateTime.Now,

                    ModifiedBy =
                        null,

                    ModifiedDate =
                        null
                };

            // ========================================================
            // INSERTAR EVENTO
            // ========================================================

            evento.VolumeBalanceEventId =
                _eventRepository.Insert(
                    evento);

            if (evento.VolumeBalanceEventId <= 0)
            {
                Debug.WriteLine(
                    "ERROR: No se pudo crear el evento.");

                MessageBox.Show(
                    "No se pudo crear el evento.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "EVENTO CREADO");

            Debug.WriteLine(
                $"EventId = " +
                $"{evento.VolumeBalanceEventId}");

            Debug.WriteLine(
                $"EventNo = " +
                $"{evento.EventNo}");

            Debug.WriteLine(
                "========================================");

            // ========================================================
            // EVENTO ANTERIOR
            // ========================================================

            if (previousEvent != null &&
                previousEvent.VolumeBalanceEventId > 0)
            {
                // ====================================================
                // PASO 1
                //
                // COPIAR EVENT_FLUID_SYSTEM
                // ====================================================

                bool configurationCopied =
                    CopyPreviousPitConfiguration(
                        previousEvent.VolumeBalanceEventId,
                        evento.VolumeBalanceEventId);

                if (!configurationCopied)
                {
                    Debug.WriteLine(
                        "ERROR: No se pudo copiar " +
                        "event_fluid_system.");

                    MessageBox.Show(
                        "El evento fue creado, pero no se pudo copiar la configuración de los pits.",
                        "Advertencia",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    // =================================================
                    // PASO 2
                    //
                    // CREAR VOL_SYSTEM DEL EVENTO NUEVO
                    //
                    // Previous = Current anterior
                    // Current  = NULL
                    // Density  = NULL
                    // =================================================

                    bool volumesInitialized =
                        InitializeVolumesFromPreviousEvent(
                            previousEvent.VolumeBalanceEventId,
                            evento.VolumeBalanceEventId);

                    if (!volumesInitialized)
                    {
                        Debug.WriteLine(
                            "ADVERTENCIA: No se pudieron " +
                            "inicializar los vol_system.");
                    }
                }
            }
            else
            {
                Debug.WriteLine(
                    "Primer evento: no se copian volúmenes.");

                // ====================================================
                // PRIMER EVENTO
                //
                // No se crea vol_system todavía.
                // ====================================================
            }

            // ========================================================
            // AGREGAR A LA INTERFAZ
            // ========================================================

            Events.Insert(
                0,
                evento);

            // ========================================================
            // SELECCIONAR EL NUEVO EVENTO
            // ========================================================
            //
            // Cuando se crea un evento nuevo, sí queremos que ese
            // evento sea el actual.
            //
            // ========================================================

            SetCurrentEvent(
                evento);

            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "FINALIZÓ CREACIÓN DE EVENTO");

            Debug.WriteLine(
                $"Evento = {evento.EventNo}");

            Debug.WriteLine(
                $"EventId = {evento.VolumeBalanceEventId}");

            Debug.WriteLine(
                "========================================");
        }

        // ============================================================
        // COPIAR CONFIGURACIÓN DE PITS
        // ============================================================

        private bool CopyPreviousPitConfiguration(
            int previousEventId,
            int newEventId)
        {
            if (previousEventId <= 0)
                return false;

            if (newEventId <= 0)
                return false;

            if (previousEventId == newEventId)
                return false;

            try
            {
                Debug.WriteLine(
                    "========================================");

                Debug.WriteLine(
                    "COPIANDO CONFIGURACIÓN");

                Debug.WriteLine(
                    $"Previous Event = {previousEventId}");

                Debug.WriteLine(
                    $"New Event = {newEventId}");

                var previousConfiguration =
                    _volConfigSystemRepository
                        .GetByVolumeBalanceEvent(
                            previousEventId);

                Debug.WriteLine(
                    $"Registros anteriores = " +
                    $"{previousConfiguration.Count}");

                if (previousConfiguration.Count == 0)
                {
                    Debug.WriteLine(
                        "No existe configuración anterior.");

                    return false;
                }

                foreach (var previousPit
                    in previousConfiguration)
                {
                    Debug.WriteLine(
                        "----------------------------------------");

                    Debug.WriteLine(
                        $"Pit = {previousPit.PitNameId}");

                    Debug.WriteLine(
                        $"System = {previousPit.PitSystemId}");

                    Debug.WriteLine(
                        $"FluidType = {previousPit.FluidTypeId}");

                    Debug.WriteLine(
                        $"Subtype = {previousPit.FluidSubType}");

                    bool success =
                        _volConfigSystemRepository.Upsert(
                            newEventId,
                            previousPit.PitNameId,
                            previousPit.PitSystemId,
                            previousPit.FluidTypeId,
                            previousPit.FluidSubType);

                    if (!success)
                    {
                        Debug.WriteLine(
                            $"ERROR copiando Pit " +
                            $"{previousPit.PitNameId}");

                        return false;
                    }
                }

                var newConfiguration =
                    _volConfigSystemRepository
                        .GetByVolumeBalanceEvent(
                            newEventId);

                Debug.WriteLine(
                    $"Registros nuevos = " +
                    $"{newConfiguration.Count}");

                Debug.WriteLine(
                    "CONFIGURACIÓN COPIADA");

                Debug.WriteLine(
                    "========================================");

                return
                    newConfiguration.Count ==
                    previousConfiguration.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "ERROR CopyPreviousPitConfiguration:");

                Debug.WriteLine(ex);

                return false;
            }
        }

        // ============================================================
        // CREAR VOL_SYSTEM DEL EVENTO NUEVO
        // ============================================================

        private bool InitializeVolumesFromPreviousEvent(
            int previousEventId,
            int newEventId)
        {
            if (previousEventId <= 0)
                return false;

            if (newEventId <= 0)
                return false;

            if (previousEventId == newEventId)
                return false;

            try
            {
                Debug.WriteLine(
                    "========================================");

                Debug.WriteLine(
                    "INICIALIZANDO VOL_SYSTEM");

                Debug.WriteLine(
                    $"Evento anterior = {previousEventId}");

                Debug.WriteLine(
                    $"Evento nuevo = {newEventId}");

                // ====================================================
                // CONFIGURACIÓN EVENTO ANTERIOR
                // ====================================================

                var previousConfigurations =
                    _volConfigSystemRepository
                        .GetByVolumeBalanceEvent(
                            previousEventId);

                if (previousConfigurations.Count == 0)
                {
                    Debug.WriteLine(
                        "No existe configuración anterior.");

                    return false;
                }

                // ====================================================
                // VOL_SYSTEM EVENTO ANTERIOR
                // ====================================================

                var previousVolumes =
                    _volSystemRepository
                        .GetByVolumeBalanceEvent(
                            previousEventId);

                Debug.WriteLine(
                    $"VolSystem anteriores = " +
                    $"{previousVolumes.Count}");

                if (previousVolumes.Count == 0)
                {
                    Debug.WriteLine(
                        "El evento anterior no tiene " +
                        "vol_system.");

                    return false;
                }

                // ====================================================
                // CONFIGURACIÓN EVENTO NUEVO
                // ====================================================

                var newConfigurations =
                    _volConfigSystemRepository
                        .GetByVolumeBalanceEvent(
                            newEventId);

                Debug.WriteLine(
                    $"Configuraciones nuevas = " +
                    $"{newConfigurations.Count}");

                if (newConfigurations.Count == 0)
                {
                    Debug.WriteLine(
                        "ERROR: El evento nuevo no tiene " +
                        "event_fluid_system.");

                    return false;
                }

                int created = 0;

                // ====================================================
                // RECORRER PITS ANTERIORES
                // ====================================================

                foreach (var previousConfiguration
                    in previousConfigurations)
                {
                    var previousVolume =
                        previousVolumes.FirstOrDefault(
                            x =>
                                x.EventFluidSystemId ==
                                previousConfiguration
                                    .EventFluidSystemId);

                    if (previousVolume == null)
                    {
                        Debug.WriteLine(
                            $"Pit {previousConfiguration.PitNameId}: " +
                            "no tiene vol_system.");

                        continue;
                    }

                    // =================================================
                    // CURRENT ANTERIOR
                    // =================================================

                    if (!previousVolume.CurrentVolume.HasValue)
                    {
                        Debug.WriteLine(
                            $"Pit {previousConfiguration.PitNameId}: " +
                            "CurrentVolume anterior es NULL.");

                        continue;
                    }

                    // =================================================
                    // BUSCAR CONFIGURACIÓN NUEVA POR PIT
                    // =================================================

                    var newConfiguration =
                        newConfigurations.FirstOrDefault(
                            x =>
                                x.PitNameId ==
                                previousConfiguration.PitNameId);

                    if (newConfiguration == null)
                    {
                        Debug.WriteLine(
                            $"Pit {previousConfiguration.PitNameId}: " +
                            "no existe configuración nueva.");

                        continue;
                    }

                    // =================================================
                    // EVENT_FLUID_SYSTEM_ID NUEVO
                    // =================================================

                    int newEventFluidSystemId =
                        newConfiguration.EventFluidSystemId;

                    if (newEventFluidSystemId <= 0)
                    {
                        Debug.WriteLine(
                            $"Pit {previousConfiguration.PitNameId}: " +
                            "EventFluidSystemId nuevo inválido.");

                        continue;
                    }

                    // =================================================
                    // CREAR VOL_SYSTEM
                    //
                    // Previous = Current anterior
                    // Current = NULL
                    // Density = NULL
                    // =================================================

                    bool success =
                        _volSystemRepository.CreateEmptyVolumeRecord(
                            newEventFluidSystemId,
                            previousVolume.CurrentVolume);

                    if (!success)
                    {
                        Debug.WriteLine(
                            $"ERROR creando vol_system para Pit " +
                            $"{previousConfiguration.PitNameId}");

                        continue;
                    }

                    created++;

                    Debug.WriteLine(
                        $"VOL_SYSTEM CREADO: " +
                        $"Pit={previousConfiguration.PitNameId}, " +
                        $"EFS={newEventFluidSystemId}, " +
                        $"Previous={previousVolume.CurrentVolume}");
                }

                // ====================================================
                // VERIFICAR
                // ====================================================

                var newVolumes =
                    _volSystemRepository
                        .GetByVolumeBalanceEvent(
                            newEventId);

                Debug.WriteLine(
                    "========================================");

                Debug.WriteLine(
                    "VOL_SYSTEM FINALIZADO");

                Debug.WriteLine(
                    $"Registros creados = {created}");

                Debug.WriteLine(
                    $"Registros encontrados = " +
                    $"{newVolumes.Count}");

                Debug.WriteLine(
                    "========================================");

                return created > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "ERROR InitializeVolumesFromPreviousEvent:");

                Debug.WriteLine(ex);

                return false;
            }
        }

        // ============================================================
        // ACTUALIZAR EVENTO
        // ============================================================

        public void UpdateEvent(
            VolumeBalanceEvent evento)
        {
            if (evento == null)
                return;

            if (evento.VolumeBalanceId !=
                _currentVolumeBalanceId)
                return;

            evento.ModifiedBy =
                Environment.UserName;

            evento.ModifiedDate =
                DateTime.Now;

            _eventRepository.Update(
                evento);

            // ========================================================
            // ACTUALIZAR TABLA DEL EVENTO SELECCIONADO
            // ========================================================

            if (VolumeInfoTableViewModel != null &&
                SelectedEventId > 0)
            {
                VolumeInfoTableViewModel
                    .VolumeBalanceEventId =
                        SelectedEventId;
            }
        }

        // ============================================================
        // VER EVENTO
        // ============================================================

        private void ViewEvent(
            VolumeBalanceEvent? evento)
        {
            if (evento == null)
                return;

            if (evento.VolumeBalanceId !=
                _currentVolumeBalanceId)
                return;

            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "[VolumeBalanceVM] ABRIENDO EVENTO");

            Debug.WriteLine(
                $"EventNo = {evento.EventNo}");

            Debug.WriteLine(
                $"EventId = {evento.VolumeBalanceEventId}");

            Debug.WriteLine(
                "========================================");

            // ========================================================
            // PRIMERO ESTABLECER EVENTO SELECCIONADO
            // ========================================================

            SetCurrentEvent(
                evento);

            // ========================================================
            // LUEGO NAVEGAR
            // ========================================================

            _navigation.NavigateToEvent(
                evento);
        }

        // ============================================================
        // EXPORTAR EVENTO
        // ============================================================

        private void ExportEvent(
            VolumeBalanceEvent? evento)
        {
            if (evento == null)
                return;

            if (evento.VolumeBalanceId !=
                _currentVolumeBalanceId)
                return;

            Debug.WriteLine(
                $"Exporting Event " +
                $"{evento.EventNo} - " +
                $"{evento.EventDateTime:yyyy-MM-dd HH:mm:ss} - " +
                $"{evento.Description}");
        }

        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    name));
        }

        // ============================================================
        // ELIMINAR EVENTO
        // ============================================================

        private void DeleteEvent(
            VolumeBalanceEvent? evento)
        {
            if (evento == null)
                return;

            if (evento.VolumeBalanceId !=
                _currentVolumeBalanceId)
                return;

            var result =
                MessageBox.Show(
                    $"Are you sure you want to delete Event #{evento.EventNo}?",
                    "Delete Event",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result !=
                MessageBoxResult.Yes)
                return;

            // ========================================================
            // SABER SI ESTAMOS ELIMINANDO EL EVENTO ACTUAL
            // ========================================================

            bool deletingSelectedEvent =
                evento.VolumeBalanceEventId ==
                SelectedEventId;

            // ========================================================
            // ELIMINAR
            // ========================================================

            _eventRepository.Delete(
                evento.VolumeBalanceEventId,
                evento.VolumeBalanceId);

            Events.Remove(
                evento);

            // ========================================================
            // SI EL EVENTO ELIMINADO ERA EL SELECCIONADO
            // ========================================================

            if (deletingSelectedEvent)
            {
                if (Events.Count > 0)
                {
                    var fallbackEvent =
                        Events
                            .OrderByDescending(
                                e => e.EventNo)
                            .FirstOrDefault();

                    if (fallbackEvent != null)
                    {
                        SetCurrentEvent(
                            fallbackEvent);
                    }
                }
                else
                {
                    SelectedEventId = 0;

                    if (VolumeInfoTableViewModel != null)
                    {
                        VolumeInfoTableViewModel
                            .VolumeBalanceEventId = 0;
                    }
                }
            }
        }
    }
}