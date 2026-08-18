using ProjectReport.Modules.VolumeBalance.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class AdditionsLiquidService
    {
        public static AdditionsLiquidService Instance { get; } = new AdditionsLiquidService();

        private AdditionsLiquidService() { }

        // =========================
        // EVENT
        // =========================
        public event EventHandler? AdditionsUpdated;

        // =========================
        // LIVE CACHE
        // =========================
        private ObservableCollection<AdditionsLiquidVol> _cache = new();

        public ObservableCollection<AdditionsLiquidVol> LiveAdditions => _cache;

        public bool HasCacheData => _cache.Count > 0;

        // =========================
        // GET
        // =========================
        public ObservableCollection<AdditionsLiquidVol> GetAdditions()
        {
            return _cache;
        }

        // =========================
        // SAVE
        // =========================
        public void SaveAdditions(ObservableCollection<AdditionsLiquidVol> additions)
        {
            if (additions == null)
            {
                Clear();
                return;
            }

            foreach (var item in _cache)
            {
                item.PropertyChanged -= Addition_PropertyChanged;
            }

            _cache = additions;

            foreach (var item in _cache)
            {
                item.PropertyChanged -= Addition_PropertyChanged;
                item.PropertyChanged += Addition_PropertyChanged;
            }

            NotifyUpdated();
        }

        // =========================
        // ADD
        // =========================
        public void Add(AdditionsLiquidVol addition)
        {
            if (addition == null)
                return;

            addition.PropertyChanged -= Addition_PropertyChanged;
            addition.PropertyChanged += Addition_PropertyChanged;

            _cache.Add(addition);

            NotifyUpdated();
        }

        // =========================
        // REMOVE
        // =========================
        public void Remove(AdditionsLiquidVol addition)
        {
            if (addition == null)
                return;

            addition.PropertyChanged -= Addition_PropertyChanged;

            _cache.Remove(addition);

            NotifyUpdated();
        }

        // =========================
        // CLEAR
        // =========================
        public void Clear()
        {
            foreach (var item in _cache)
            {
                item.PropertyChanged -= Addition_PropertyChanged;
            }

            _cache.Clear();

            NotifyUpdated();
        }

        // =========================
        // PROPERTY CHANGED (Debounce)
        // =========================
        private System.Timers.Timer? _debounceTimer;

        private void Addition_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_debounceTimer == null)
            {
                _debounceTimer = new System.Timers.Timer(120);
                _debounceTimer.AutoReset = false;
                _debounceTimer.Elapsed += (_, __) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NotifyUpdated();
                    });
                };
            }

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        // =========================
        // NOTIFY
        // =========================
        private void NotifyUpdated()
        {
            AdditionsUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}