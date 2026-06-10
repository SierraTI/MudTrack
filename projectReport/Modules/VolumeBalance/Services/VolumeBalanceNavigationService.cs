using System;
using ProjectReport.Modules.VolumeBalance.Models;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class VolumeBalanceNavigationService
    {
        public event Action<VolumeBalanceEvent>? NavigateToEventRequested;

        public void NavigateToEvent(VolumeBalanceEvent evento)
        {
            NavigateToEventRequested?.Invoke(evento);
        }
    }
}