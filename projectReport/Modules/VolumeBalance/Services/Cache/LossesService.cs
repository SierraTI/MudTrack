using ProjectReport.Modules.VolumeBalance.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    class LossesService
    {
        // =========================
        // SINGLETON
        // =========================

        private static readonly Lazy<LossesService> _instance =
            new(() => new LossesService());

        public static LossesService Instance => _instance.Value;

        private LossesService()
        {
        }

        // =========================
        // LIVE COLLECTION
        // =========================

        public ObservableCollection<LossesVol> LiveLosses { get; }
            = new ObservableCollection<LossesVol>();

        // =========================
        // CLEAR
        // =========================

        public void Clear()
        {
            LiveLosses.Clear();
        }

        // =========================
        // CACHE
        // =========================

        public bool HasCacheData => LiveLosses.Count > 0;
    }
}

