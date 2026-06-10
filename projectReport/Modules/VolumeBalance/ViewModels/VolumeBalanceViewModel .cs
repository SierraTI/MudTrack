using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.ViewModels;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolumeBalanceViewModel
    {
        private int _nextId = 1;

        private readonly VolumeBalanceNavigationService _navigation;

        public ObservableCollection<VolumeBalanceEvent> Events { get; set; }

        public ICommand AddEventCommand { get; }
        public ICommand ViewEventCommand { get; }

        public VolumeBalanceViewModel(
            VolumeBalanceNavigationService navigation)
        {
            _navigation = navigation;

            Events = new ObservableCollection<VolumeBalanceEvent>();

            AddEventCommand =
                new RelayCommand(_ => AddEvent());

            ViewEventCommand =
                new RelayCommand<VolumeBalanceEvent>(ViewEvent);
        }

        private void AddEvent()
        {
            Events.Add(new VolumeBalanceEvent
            {
                Id = _nextId++,

                Hora = DateTime.Now.ToString("HH:mm:ss"),
                Description = "New event",
                CurrentDepth = "0",
                Activity = "Added"
            });
        }

        private void ViewEvent(VolumeBalanceEvent? evento)
        {
            if (evento == null)
                return;

            _navigation.NavigateToEvent(evento);
        }
    }
}