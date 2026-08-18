using ProjectReport.Modules.VolumeBalance.Models;
using System;
using System.Collections.ObjectModel;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class AdditionsFluidService
    {
        // =========================
        // SINGLETON
        // =========================
        private static readonly Lazy<AdditionsFluidService> _instance =
            new(() => new AdditionsFluidService());

        public static AdditionsFluidService Instance => _instance.Value;

        private AdditionsFluidService()
        {
        }

        // =========================
        // LIVE COLLECTION
        // =========================
        public ObservableCollection<AdditionsFluidVol> LiveAdditionsFluidVolumes { get; }
            = new ObservableCollection<AdditionsFluidVol>();

        // =========================
        // EVENTS
        // =========================
        public event EventHandler? AdditionsFluidUpdated;

        // =========================
        // SAVE
        // =========================
        public void SaveAdditions(ObservableCollection<AdditionsFluidVol> additions)
        {
            LiveAdditionsFluidVolumes.Clear();

            foreach (var item in additions)
                LiveAdditionsFluidVolumes.Add(item);

            AdditionsFluidUpdated?.Invoke(this, EventArgs.Empty);
        }

        // =========================
        // CLEAR
        // =========================
        public void Clear()
        {
            LiveAdditionsFluidVolumes.Clear();
            AdditionsFluidUpdated?.Invoke(this, EventArgs.Empty);
        }

        // =========================
        // CACHE
        // =========================
        public bool HasCacheData => LiveAdditionsFluidVolumes.Count > 0;
    }
}