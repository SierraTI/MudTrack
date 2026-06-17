using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProjectReport.Modules.VolumeBalance.Data;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.ViewModels;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolumeBalanceViewModel : INotifyPropertyChanged
    {
        private readonly VolumeBalanceNavigationService _navigation;
        private readonly VolumeBalanceEventRepository _repository;

        private readonly int _currentWellId;

        private ObservableCollection<VolumeBalanceEvent> _events;

        public ObservableCollection<VolumeBalanceEvent> Events
        {
            get => _events;
            set
            {
                _events = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddEventCommand { get; }
        public ICommand ViewEventCommand { get; }
        public ICommand ExportEventCommand { get; }

        public VolumeBalanceViewModel(
            VolumeBalanceNavigationService navigation,
            int wellId)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _repository = new VolumeBalanceEventRepository();

            _currentWellId = wellId;

            LoadFromDatabase(); // 🔥 SIEMPRE CARGA SOLO ESTE WELL

            AddEventCommand = new RelayCommand(_ => AddEvent());
            ViewEventCommand = new RelayCommand<VolumeBalanceEvent>(ViewEvent);
            ExportEventCommand = new RelayCommand<VolumeBalanceEvent>(ExportEvent);
        }

        // RELOAD DESDE BD
        public void Refresh()
        {
            LoadFromDatabase();
        }

        private void LoadFromDatabase()
        {
            var data = _repository.GetAllByWell(_currentWellId);

            Events = new ObservableCollection<VolumeBalanceEvent>(data);
        }

        private void AddEvent()
        {
            var evento = new VolumeBalanceEvent
            {
                EventTime = DateTime.Now.ToString("HH:mm:ss"),
                Description = string.Empty,
                CurrentDepth = 0,
                Activity = string.Empty,
                IdW = _currentWellId
            };

            evento.Id = _repository.Insert(evento);

            Events.Insert(0, evento);
        }

        // ESTE ES EL QUE GUARDA CAMBIOS DEL DATAGRID
        public void UpdateEvent(VolumeBalanceEvent evento)
        {
            if (evento == null) return;

            if (evento.IdW != _currentWellId)
                return;

            _repository.Update(evento);
        }

        private void ViewEvent(VolumeBalanceEvent? evento)
        {
            if (evento == null) return;

            if (evento.IdW != _currentWellId) return;

            _navigation.NavigateToEvent(evento);
        }

        private void ExportEvent(VolumeBalanceEvent? evento)
        {
            if (evento == null) return;

            if (evento.IdW != _currentWellId) return;

            System.Diagnostics.Debug.WriteLine(
                $"Exporting {evento.EventTime} - {evento.Description}"
            );
        }

        // NOTIFY SYSTEM
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}