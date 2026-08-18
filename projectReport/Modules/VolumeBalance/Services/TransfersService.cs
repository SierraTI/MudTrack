using ProjectReport.Modules.VolumeBalance.Models;
using System;
using System.Collections.ObjectModel;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class TransfersService
    {
        // =========================
        // SINGLETON
        // =========================

        private static readonly Lazy<TransfersService> _instance =
            new(() => new TransfersService());

        public static TransfersService Instance => _instance.Value;

        private TransfersService()
        {
        }

        // =========================
        // LIVE COLLECTION
        // =========================

        public ObservableCollection<TransfersVol> LiveTransfers { get; }
            = new ObservableCollection<TransfersVol>();

        // =========================
        // CLEAR
        // =========================

        public void Clear()
        {
            LiveTransfers.Clear();
        }

        // =========================
        // CACHE
        // =========================

        public bool HasCacheData => LiveTransfers.Count > 0;
    }
}