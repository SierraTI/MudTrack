using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Services;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    /// <summary>
    /// A chemical usage entry pulled from Inventory.
    /// </summary>
    public class ChemicalUsage : INotifyPropertyChanged
    {
        private string _productCode = string.Empty;
        private string _description = string.Empty;
        private double _qtyUsed;
        private string _unit = string.Empty;
        private double _sg;
        private double _volumeAdded;

        public string ProductCode
        {
            get => _productCode;
            set { _productCode = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public double QtyUsed
        {
            get => _qtyUsed;
            set { _qtyUsed = value; OnPropertyChanged(); RecalculateVolume(); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); RecalculateVolume(); }
        }

        /// <summary>Specific Gravity of the chemical product.</summary>
        public double SG
        {
            get => _sg;
            set { _sg = value; OnPropertyChanged(); RecalculateVolume(); }
        }

        /// <summary>Volume added in barrels. Auto-calculated from Qty, Unit and SG.</summary>
        public double VolumeAdded
        {
            get => _volumeAdded;
            private set { _volumeAdded = value; OnPropertyChanged(); }
        }

        private void RecalculateVolume()
        {
            VolumeAdded = ChemicalVolumeConverter.ToBarrels(QtyUsed, Unit, SG > 0 ? SG : 1.0);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Main ViewModel for the Volume Balance screen.
    /// </summary>
    public class VolumeBalanceViewModel : BaseViewModel
    {
        private const string ClassificationActive = "Active";

        // Latest geometry values cached for stamping into new events
        private double _latestStringVol;
        private double _latestAnnulusVol;
        private double _latestDepthFt;

        #region Wellbore Section (auto-populated from Geometry)

        private double _holeCapacity;
        public double HoleCapacity
        {
            get => _holeCapacity;
            private set { if (SetField(ref _holeCapacity, value)) RefreshSummary(); }
        }

        private double _stringDisplacement;
        public double StringDisplacement
        {
            get => _stringDisplacement;
            private set { if (SetField(ref _stringDisplacement, value)) RefreshSummary(); }
        }

        private double _stringTheoretical;
        public double StringTheoretical
        {
            get => _stringTheoretical;
            private set { if (SetField(ref _stringTheoretical, value)) RefreshSummary(); }
        }

        private double _stringActual;
        public double StringActual
        {
            get => _stringActual;
            set { if (SetField(ref _stringActual, value)) RefreshSummary(); }
        }

        private double _annulusTheoretical;
        public double AnnulusTheoretical
        {
            get => _annulusTheoretical;
            private set { if (SetField(ref _annulusTheoretical, value)) RefreshSummary(); }
        }

        private double _annulusActual;
        public double AnnulusActual
        {
            get => _annulusActual;
            set { if (SetField(ref _annulusActual, value)) RefreshSummary(); }
        }

        // Granular Hole Volumes (from organized spec)
        public double HoleRiserTheoretical => WellContextService.Instance.CurrentWell?.WellboreComponents
            .Where(c => c.Component == ComponentType.Riser).Sum(c => c.Volume) ?? 0;

        public double HoleCasingTheoretical => WellContextService.Instance.CurrentWell?.WellboreComponents
            .Where(c => c.Component == ComponentType.Casing || c.Component == ComponentType.Liner).Sum(c => c.Volume) ?? 0;

        public double HoleOpenHoleTheoretical => WellContextService.Instance.CurrentWell?.WellboreComponents
            .Where(c => c.Component == ComponentType.OpenHole).Sum(c => c.Volume) ?? 0;

        public double HoleAnnularTheoretical => AnnulusTheoretical;

        public double TotalHoleTheoretical => HoleRiserTheoretical + HoleCasingTheoretical + HoleOpenHoleTheoretical + HoleAnnularTheoretical;

        public double TheoreticalWellbore => Math.Max(0, HoleCapacity - StringDisplacement);
        public double TotalWellTheoretical => StringTheoretical + AnnulusTheoretical;
        public double TotalWellActual => StringActual + AnnulusActual;
        public double WellVariance => TotalWellActual - TotalWellTheoretical;

        #endregion

        #region Surface Section (auto-populated from Rig Profile)

        public ObservableCollection<SurfaceTank> SurfaceTanks { get; } = new();

        public double TotalSurfaceMaxCapacity => SurfaceTanks.Sum(t => t.MaxCapacity);

        public double TotalActiveSurfaceVolume => SurfaceTanks.Where(t => string.Equals(t.Classification, "Active", StringComparison.OrdinalIgnoreCase)).Sum(t => t.VolumeBbl);
        public double TotalReserveSurfaceVolume => SurfaceTanks.Where(t => string.Equals(t.Classification, "Reserve", StringComparison.OrdinalIgnoreCase)).Sum(t => t.VolumeBbl);
        public double TotalOtherSurfaceVolume => SurfaceTanks.Where(t => string.Equals(t.Classification, "Other", StringComparison.OrdinalIgnoreCase)).Sum(t => t.VolumeBbl);

        public double TotalSurfaceVolume => SurfaceTanks.Sum(t => t.VolumeBbl);

        public double TotalFluidOnLocation => TotalHoleTheoretical + TotalSurfaceVolume;

        public void SyncFromRigProfile(IList<RigPit> activePits)
        {
            var existing = new Dictionary<string, SurfaceTank>(StringComparer.OrdinalIgnoreCase);

            foreach (var tank in SurfaceTanks)
            {
                tank.PropertyChanged -= OnSurfaceTankPropertyChanged;
                existing[tank.Name.Trim()] = tank;
            }

            SurfaceTanks.Clear();

            // Always enforce the 10 standard Listas tanks
            var standardTanks = new List<SurfaceTank>();
            for (int i = 1; i <= 5; i++) standardTanks.Add(new SurfaceTank { Name = $"Tank {i}", Classification = "Active", MaxCapacity = 500 });
            for (int i = 6; i <= 8; i++) standardTanks.Add(new SurfaceTank { Name = $"Tank {i}", Classification = "Reserve", MaxCapacity = 500 });
            for (int i = 9; i <= 10; i++) standardTanks.Add(new SurfaceTank { Name = $"Tank {i}", Classification = "Other", MaxCapacity = 500 });

            // If activePits has data, we can optionally map it to standardTanks by index.
            // But main requirement is the 10 Listas tanks are always present.
            for (int i = 0; i < standardTanks.Count; i++)
            {
                var tank = standardTanks[i];
                if (existing.TryGetValue(tank.Name, out var oldTank))
                {
                    tank.VolumeBbl = oldTank.VolumeBbl;
                    tank.Classification = oldTank.Classification; // allow user to override classification
                    tank.FluidType = oldTank.FluidType;
                    tank.Density = oldTank.Density;
                    tank.YesterdayVol = oldTank.YesterdayVol;
                    tank.MaxCapacity = oldTank.MaxCapacity;
                }
                else if (activePits != null && i < activePits.Count)
                {
                    // Map rig profile pit limits if it's new
                    tank.MaxCapacity = activePits[i].MaxCapacity > 0 ? activePits[i].MaxCapacity : 500;
                    tank.VolumeBbl = activePits[i].CurrentVolume;
                }

                tank.PropertyChanged += OnSurfaceTankPropertyChanged;
                SurfaceTanks.Add(tank);
            }

            // Also add any extra pits from Rig Profile beyond the 10 if necessary
            if (activePits != null && activePits.Count > 10)
            {
                for (int i = 10; i < activePits.Count; i++)
                {
                    var pit = activePits[i];
                    var displayName = string.IsNullOrWhiteSpace(pit.PitName) ? $"Pit {i+1}" : pit.PitName.Trim();
                    
                    var tank = new SurfaceTank { Name = displayName, Classification = "Other", MaxCapacity = pit.MaxCapacity };
                    if (existing.TryGetValue(displayName, out var oldExtra))
                    {
                        tank.VolumeBbl = oldExtra.VolumeBbl;
                        tank.Classification = oldExtra.Classification;
                        tank.FluidType = oldExtra.FluidType;
                        tank.Density = oldExtra.Density;
                        tank.YesterdayVol = oldExtra.YesterdayVol;
                    }
                    else
                    {
                        tank.VolumeBbl = pit.CurrentVolume;
                    }
                    tank.PropertyChanged += OnSurfaceTankPropertyChanged;
                    SurfaceTanks.Add(tank);
                }
            }

            RefreshSummary();
            OnPropertyChanged(nameof(SurfaceTanks));
            OnPropertyChanged(nameof(TotalSurfaceVolume));
            OnPropertyChanged(nameof(TotalActiveSurfaceVolume));
            OnPropertyChanged(nameof(TotalSurfaceMaxCapacity));
        }

        #endregion

        #region Losses and Gains (Additions and Reductions)

        private double _waterAdded;
        public double WaterAdded
        {
            get => _waterAdded;
            set { if (SetField(ref _waterAdded, value)) RefreshSummary(); }
        }

        private double _transfersIn;
        public double TransfersIn
        {
            get => _transfersIn;
            set { if (SetField(ref _transfersIn, value)) RefreshSummary(); }
        }

        private double _surfaceLosses;
        public double SurfaceLosses
        {
            get => _surfaceLosses;
            set { if (SetField(ref _surfaceLosses, value)) RefreshSummary(); }
        }

        private double _subSurfaceLosses;
        public double SubSurfaceLosses
        {
            get => _subSurfaceLosses;
            set { if (SetField(ref _subSurfaceLosses, value)) RefreshSummary(); }
        }

        private double _transfersOut;
        public double TransfersOut
        {
            get => _transfersOut;
            set { if (SetField(ref _transfersOut, value)) RefreshSummary(); }
        }

        public double TotalGains => WaterAdded + TransfersIn + TotalChemicalVolumeAdded;
        public double TotalLosses => SurfaceLosses + SubSurfaceLosses + TransfersOut;

        private double _hoursElapsed;
        public double HoursElapsed
        {
            get => _hoursElapsed;
            set { if (SetField(ref _hoursElapsed, value)) RefreshSummary(); }
        }

        public double SeepageRate => VolumeBalanceEngine.CalculateSeepageRate(SubSurfaceLosses, HoursElapsed);

        #endregion

        #region Chemicals Section (from Inventory)

        public ObservableCollection<ChemicalUsage> ChemicalUsages { get; } = new();

        public double TotalChemicalVolumeAdded => ChemicalUsages.Sum(c => c.VolumeAdded);

        private ICommand? _addChemicalCommand;
        public ICommand AddChemicalCommand => _addChemicalCommand ??= new RelayCommand(_ =>
        {
            ChemicalUsages.Add(new ChemicalUsage { Unit = "sack", SG = 1.0 });
            RefreshSummary();
        });

        private ICommand? _removeChemicalCommand;
        public ICommand RemoveChemicalCommand => _removeChemicalCommand ??= new RelayCommand(param =>
        {
            if (param is ChemicalUsage item && ChemicalUsages.Contains(item))
            {
                ChemicalUsages.Remove(item);
                RefreshSummary();
            }
        });

        #endregion

        #region Theoretical Density and Rollover (Yesterday)

        private double _yesterdayWellboreVol;
        public double YesterdayWellboreVol
        {
            get => _yesterdayWellboreVol;
            set { if (SetField(ref _yesterdayWellboreVol, value)) RefreshSummary(); }
        }

        public double TotalYesterdaySurfaceVolume => SurfaceTanks.Sum(t => t.YesterdayVol);
        public double TotalYesterdayActiveSurfaceVolume => SurfaceTanks.Where(IsActiveTank).Sum(t => t.YesterdayVol);
        public double SystemTotalYesterday => YesterdayWellboreVol + TotalYesterdaySurfaceVolume;

        public double AverageActualSurfaceDensity
        {
            get
            {
                var active = SurfaceTanks.Where(IsActiveTank).ToList();
                if (!active.Any()) return 0;

                var totalActiveVol = active.Sum(t => t.VolumeBbl);
                if (totalActiveVol <= 0) return active.Average(t => t.Density);

                var weightedSg = active.Sum(t => t.Density * t.VolumeBbl) / totalActiveVol;
                return Math.Round(weightedSg, 2);
            }
        }

        public double TheoreticalSystemDensity
        {
            get
            {
                double startVolume = TotalYesterdayActiveSurfaceVolume;
                if (startVolume <= 0) return AverageActualSurfaceDensity;

                double startSg = AverageActualSurfaceDensity;
                double chemVol = TotalChemicalVolumeAdded;
                double chemSg = chemVol > 0
                    ? ChemicalUsages.Sum(c => c.VolumeAdded * (c.SG > 0 ? c.SG : 1.0)) / chemVol
                    : 1.0;

                double totalVolume = startVolume + chemVol + WaterAdded;
                if (totalVolume <= 0) return AverageActualSurfaceDensity;

                return Math.Round(VolumeBalanceEngine.CalculateTheoreticalDensity(
                    startVolume: startVolume,
                    startSg: startSg,
                    addedChemicalVolume: chemVol,
                    addedChemicalSg: chemSg,
                    addedWaterVolume: WaterAdded,
                    addedWaterSg: 1.0), 2);
            }
        }

        private ICommand? _rolloverCommand;
        public ICommand RolloverCommand => _rolloverCommand ??= new RelayCommand(_ =>
        {
            foreach (var tank in SurfaceTanks)
            {
                tank.YesterdayVol = tank.VolumeBbl;
            }

            YesterdayWellboreVol = TotalWellActual;
            RefreshSummary();
        });

        #endregion

        #region Golden Equation Summary (Accounting vs Physical)

        public double AccountingTotal => SystemTotalYesterday + TotalGains - TotalLosses;

        public double TheoreticalSurfaceEquipmentVolume =>
            CalculateRigSurfaceEquipmentVolume(WellContextService.Instance.CurrentWell);

        private double _actualSurfaceEquipmentVolume;
        public double ActualSurfaceEquipmentVolume
        {
            get => _actualSurfaceEquipmentVolume;
            set { if (SetField(ref _actualSurfaceEquipmentVolume, value)) RefreshSummary(); }
        }

        public double SurfaceEquipmentVolumeUsed =>
            ActualSurfaceEquipmentVolume > 0 ? ActualSurfaceEquipmentVolume : TheoreticalSurfaceEquipmentVolume;

        public double PhysicalTotal => TotalWellActual + TotalActiveSurfaceVolume + SurfaceEquipmentVolumeUsed;

        public double SystemVariance => PhysicalTotal - AccountingTotal;

        public double BalanceToleranceBbl => Math.Abs(AccountingTotal) * 0.02;

        public string VarianceStatus =>
            Math.Abs(SystemVariance) <= BalanceToleranceBbl
                ? "Balanced"
                : SystemVariance < 0 ? "Possible Loss" : "Possible Gain / Kick";

        public string VarianceColor => Math.Abs(SystemVariance) <= BalanceToleranceBbl ? "#388E3C" : "#D32F2F";

        private double _previousVariance;
        private double _varianceDelta;
        public string VarianceTrend
        {
            get
            {
                if (Math.Abs(_varianceDelta) < 0.5) return "-";
                return _varianceDelta > 0 ? "Up" : "Down";
            }
        }

        private void RefreshSummary()
        {
            var newVariance = SystemVariance;
            _varianceDelta = newVariance - _previousVariance;
            _previousVariance = newVariance;

            OnPropertyChanged(nameof(TheoreticalWellbore));
            OnPropertyChanged(nameof(TotalWellTheoretical));
            OnPropertyChanged(nameof(TotalWellActual));
            OnPropertyChanged(nameof(WellVariance));

            OnPropertyChanged(nameof(TotalSurfaceVolume));
            OnPropertyChanged(nameof(TotalActiveSurfaceVolume));
            OnPropertyChanged(nameof(TotalSurfaceMaxCapacity));

            OnPropertyChanged(nameof(TotalGains));
            OnPropertyChanged(nameof(TotalLosses));
            OnPropertyChanged(nameof(SeepageRate));
            OnPropertyChanged(nameof(TotalChemicalVolumeAdded));

            OnPropertyChanged(nameof(TotalYesterdaySurfaceVolume));
            OnPropertyChanged(nameof(TotalYesterdayActiveSurfaceVolume));
            OnPropertyChanged(nameof(SystemTotalYesterday));

            OnPropertyChanged(nameof(AverageActualSurfaceDensity));
            OnPropertyChanged(nameof(TheoreticalSystemDensity));

            OnPropertyChanged(nameof(AccountingTotal));
            OnPropertyChanged(nameof(TheoreticalSurfaceEquipmentVolume));
            OnPropertyChanged(nameof(SurfaceEquipmentVolumeUsed));
            OnPropertyChanged(nameof(PhysicalTotal));
            OnPropertyChanged(nameof(SystemVariance));
            OnPropertyChanged(nameof(BalanceToleranceBbl));
            OnPropertyChanged(nameof(VarianceStatus));
            OnPropertyChanged(nameof(VarianceColor));
            OnPropertyChanged(nameof(VarianceTrend));

            // Refinement Summary
            OnPropertyChanged(nameof(HoleRiserTheoretical));
            OnPropertyChanged(nameof(HoleCasingTheoretical));
            OnPropertyChanged(nameof(HoleOpenHoleTheoretical));
            OnPropertyChanged(nameof(HoleAnnularTheoretical));
            OnPropertyChanged(nameof(TotalHoleTheoretical));
            OnPropertyChanged(nameof(TotalActiveSurfaceVolume));
            OnPropertyChanged(nameof(TotalReserveSurfaceVolume));
            OnPropertyChanged(nameof(TotalOtherSurfaceVolume));
            OnPropertyChanged(nameof(TotalFluidOnLocation));

            // Logic Gates for UI
            OnPropertyChanged(nameof(VolumeBalanceGateStatus));
            OnPropertyChanged(nameof(ChemicalBalanceGateStatus));
        }

        public string VolumeBalanceGateStatus => SelectedEvent?.VolumeBalanceGateText ?? "NO DATA";
        public string ChemicalBalanceGateStatus => SelectedEvent?.ChemicalBalanceGateText ?? "NO DATA";

        #endregion

        #region Sync / Last Updated

        private string _lastSyncedAt = "Not yet synced";
        public string LastSyncedAt
        {
            get => _lastSyncedAt;
            private set { _lastSyncedAt = value; OnPropertyChanged(); }
        }

        private bool _isGeometrySynced;
        public bool IsGeometrySynced
        {
            get => _isGeometrySynced;
            private set { _isGeometrySynced = value; OnPropertyChanged(); }
        }

        private ICommand? _syncCommand;
        public ICommand SyncCommand => _syncCommand ??= new RelayCommand(_ => RequestGeometryResync());

        private void RequestGeometryResync()
        {
            if (_lastGeometryArgs != null)
                ApplyGeometryData(_lastGeometryArgs);
        }

        private GeometryDataUpdatedEventArgs? _lastGeometryArgs;

        #endregion

        #region Constructor and Event Subscriptions

        public VolumeBalanceViewModel()
        {
            WellContextService.Instance.GeometryDataUpdated += OnGeometryDataUpdated;
            WellContextService.Instance.RigProfileUpdated += OnRigProfileUpdated;
            WellContextService.Instance.ChemicalSelectionUpdated += OnChemicalSelectionUpdated;
            WellContextService.Instance.VolumeEventsUpdated += OnVolumeEventsUpdated;
            WellContextService.Instance.DepthUpdated += OnGlobalDepthUpdated;

            SurfaceTanks.CollectionChanged += OnSurfaceTanksCollectionChanged;

            ChemicalUsages.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (ChemicalUsage c in e.NewItems)
                        c.PropertyChanged += (_, _) => RefreshSummary();
                }

                RefreshSummary();
            };

            // Pre-load the 10 tanks on initialization
            SyncFromRigProfile(new List<RigPit>());

            // Subscribe to event ledger changes for persistence
            Events.CollectionChanged += (s, e) => WellContextService.Instance.PublishVolumeEvents(Events);

            // Load existing data if available
            LoadExistingEvents();
        }

        private void LoadExistingEvents()
        {
            var loaded = WellContextService.Instance.GetLoadedEvents();
            if (loaded != null && loaded.Any())
            {
                Events.Clear();
                // newest first
                foreach (var ev in loaded.OrderByDescending(x => x.Timestamp))
                {
                    Events.Add(ev);
                }
                
                // Also update current SurfaceTanks to the most recent event's snapshot
                var latest = loaded.OrderByDescending(x => x.Timestamp).FirstOrDefault();
                if (latest != null)
                {
                    foreach (var snap in latest.TankSnapshots)
                    {
                        var tank = SurfaceTanks.FirstOrDefault(t => t.Name == snap.TankName);
                        if (tank != null)
                        {
                            tank.VolumeBbl = snap.CurrentVol;
                            tank.Density = snap.Density;
                        }
                    }
                }
                RefreshSummary();
            }
        }
        private void OnVolumeEventsUpdated(IEnumerable<VolumeBalanceEvent> events)
        {
            LoadExistingEvents();
        }

        public void Detach()
        {
            WellContextService.Instance.GeometryDataUpdated -= OnGeometryDataUpdated;
            WellContextService.Instance.RigProfileUpdated -= OnRigProfileUpdated;
            WellContextService.Instance.ChemicalSelectionUpdated -= OnChemicalSelectionUpdated;
            WellContextService.Instance.VolumeEventsUpdated -= OnVolumeEventsUpdated;
            WellContextService.Instance.DepthUpdated -= OnGlobalDepthUpdated;

            SurfaceTanks.CollectionChanged -= OnSurfaceTanksCollectionChanged;
            foreach (var tank in SurfaceTanks)
                tank.PropertyChanged -= OnSurfaceTankPropertyChanged;
        }

        #endregion

        #region Event Handlers

        private void OnChemicalSelectionUpdated(object? sender, ChemicalSelectionUpdatedEventArgs e)
        {
            foreach (var item in e.SelectedItems)
            {
                if (ChemicalUsages.Any(c => c.ProductCode == item.Code))
                    continue;

                ChemicalUsages.Add(new ChemicalUsage
                {
                    ProductCode = item.Code,
                    Description = item.Name,
                    QtyUsed = 1.0,
                    Unit = item.Unit,
                    SG = item.SG
                });
            }

            RefreshSummary();
        }

        private void OnGlobalDepthUpdated(object? sender, double depth)
        {
            _latestDepthFt = depth;
        }

        private void OnGeometryDataUpdated(object? sender, GeometryDataUpdatedEventArgs e)
        {
            _lastGeometryArgs = e;
            ApplyGeometryData(e);
        }

        private void OnRigProfileUpdated(object? sender, RigProfileUpdatedEventArgs e)
        {
            SyncFromRigProfile(e.ActivePits);
        }

        private void ApplyGeometryData(GeometryDataUpdatedEventArgs e)
        {
            _latestStringVol = e.StringInternalVolume;
            _latestAnnulusVol = e.AnnularVolume;
            LastSyncedAt = DateTime.Now.ToString("HH:mm:ss");
            IsGeometrySynced = true;
            RefreshSummary();
        }

        private void OnSurfaceTanksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SurfaceTank tank in e.NewItems)
                    tank.PropertyChanged += OnSurfaceTankPropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (SurfaceTank tank in e.OldItems)
                    tank.PropertyChanged -= OnSurfaceTankPropertyChanged;
            }

            RefreshSummary();
        }

        private void OnSurfaceTankPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RefreshSummary();
        }

        private static bool IsActiveTank(SurfaceTank tank)
        {
            return string.Equals(tank.Classification?.Trim(), ClassificationActive, StringComparison.OrdinalIgnoreCase);
        }

        private static double CalculateRigSurfaceEquipmentVolume(Well? well)
        {
            var rigProfile = well?.RigProfile;
            if (rigProfile == null) return 0;

            return CalculateCollectionVolume(rigProfile.SurfaceEquipment) +
                   CalculateCollectionVolume(rigProfile.ServiceLine);
        }

        private static double CalculateCollectionVolume(IEnumerable<RigSurfaceEquipment>? equipment)
        {
            if (equipment == null) return 0;

            return equipment
                .Where(e => e.InternalDiameter > 0 && e.Length > 0)
                .Sum(e => VolumeBalanceEngine.CalculateSurfaceEquipmentVolume(e.InternalDiameter, e.Length));
        }

        #endregion

        #region INotifyPropertyChanged helper

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName!);
            return true;
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region Event Ledger
        // ════════════════════════════════════════════════════════

        public ObservableCollection<VolumeBalanceEvent> Events { get; } = new();

        private VolumeBalanceEvent? _selectedEvent;
        public VolumeBalanceEvent? SelectedEvent
        {
            get => _selectedEvent;
            set { if (SetField(ref _selectedEvent, value)) OnPropertyChanged(nameof(HasSelectedEvent)); }
        }

        public bool HasSelectedEvent => SelectedEvent != null;

        private bool _isEventPanelOpen;
        public bool IsEventPanelOpen
        {
            get => _isEventPanelOpen;
            set { SetField(ref _isEventPanelOpen, value); }
        }

        private VolumeBalanceEvent? _draftEvent;
        public VolumeBalanceEvent? DraftEvent
        {
            get => _draftEvent;
            private set 
            { 
                if (_draftEvent != null)
                {
                    _draftEvent.Transfers.CollectionChanged -= OnDraftTransfersChanged;
                }

                if (SetField(ref _draftEvent, value))
                {
                    if (_draftEvent != null)
                    {
                        _draftEvent.Transfers.CollectionChanged += OnDraftTransfersChanged;
                    }
                    OnPropertyChanged(nameof(HasSelectedEvent)); 
                }
            }
        }

        private void OnDraftTransfersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += OnTransferItemChanged;
            }
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= OnTransferItemChanged;
            }

            DraftEvent?.ApplyTransfersToSnapshots();
        }

        private void OnTransferItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            DraftEvent?.ApplyTransfersToSnapshots();
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set { SetField(ref _validationMessage, value); }
        }

        // ── ADD EVENT command ─────────────────────────────────────
        private ICommand? _addEventCommand;
        public ICommand AddEventCommand => _addEventCommand ??= new RelayCommand(_ =>
        {
            var draft = new VolumeBalanceEvent
            {
                Timestamp     = DateTime.Now,
                DepthFt       = _latestDepthFt,
                StringVolBbl  = _latestStringVol,
                AnnulusVolBbl = _latestAnnulusVol
            };

            // Snapshot current pits → populate PreviousVol from current VolumeBbl
            foreach (var tank in SurfaceTanks)
            {
                draft.TankSnapshots.Add(new EventTankSnapshot
                {
                    TankName       = tank.Name,
                    Classification = tank.Classification ?? "Active",
                    PreviousVol    = tank.VolumeBbl,
                    CurrentVol     = tank.VolumeBbl,   // starts same; user edits current
                    Density        = tank.Density,
                    MaxCapacity    = tank.MaxCapacity
                });
            }

            // Add default rows for common loss categories
            draft.Losses.Add(new EventLoss { Category = LossCategory.SCE,      LossType = "Shakers" });
            draft.Losses.Add(new EventLoss { Category = LossCategory.Downhole,  LossType = "Filtration" });
            draft.Losses.Add(new EventLoss { Category = LossCategory.Misc,      LossType = "Evaporation" });

            // Copy active chemicals from the global list
            foreach (var chem in ChemicalUsages)
            {
                draft.Chemicals.Add(new EventChemical
                {
                    ProductCode  = chem.ProductCode,
                    ProductName  = chem.Description,
                    SG           = chem.SG
                });
            }

            DraftEvent = draft;
            ValidationMessage = string.Empty;
            IsEventPanelOpen = true;
        });

        // ── SAVE EVENT command ────────────────────────────────────
        private ICommand? _saveEventCommand;
        public ICommand SaveEventCommand => _saveEventCommand ??= new RelayCommand(_ =>
        {
            if (DraftEvent == null) return;

            var errors = ValidateDraft(DraftEvent);
            if (errors.Count > 0)
            {
                ValidationMessage = string.Join("  |  ", errors);
                return;
            }

            // Push current-vol back to the live SurfaceTanks so the main dashboard stays in sync
            foreach (var snap in DraftEvent.TankSnapshots)
            {
                var tank = SurfaceTanks.FirstOrDefault(t =>
                    string.Equals(t.Name, snap.TankName, StringComparison.OrdinalIgnoreCase));
                if (tank != null)
                {
                    tank.VolumeBbl = snap.CurrentVol;
                    tank.Density   = snap.Density;
                }
            }

            Events.Insert(0, DraftEvent);     // newest first
            SelectedEvent = DraftEvent;

            DraftEvent = null;
            IsEventPanelOpen = false;
            ValidationMessage = string.Empty;

            OnPropertyChanged(nameof(TrendPoints));
            RefreshSummary();
        });

        // ── CANCEL EVENT command ──────────────────────────────────
        private ICommand? _cancelEventCommand;
        public ICommand CancelEventCommand => _cancelEventCommand ??= new RelayCommand(_ =>
        {
            DraftEvent = null;
            IsEventPanelOpen = false;
            ValidationMessage = string.Empty;
        });

        // ── DELETE EVENT command ──────────────────────────────────
        private ICommand? _deleteEventCommand;
        public ICommand DeleteEventCommand => _deleteEventCommand ??= new RelayCommand(_ =>
        {
            if (SelectedEvent == null) return;

            var result = MessageBox.Show(
                $"Delete event from {SelectedEvent.TimestampLabel}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            Events.Remove(SelectedEvent);
            SelectedEvent = Events.FirstOrDefault();
            OnPropertyChanged(nameof(TrendPoints));
        });

        // ── DRAFT — add/remove loss ───────────────────────────────
        private ICommand? _addDraftLossCommand;
        public ICommand AddDraftLossCommand => _addDraftLossCommand ??= new RelayCommand(_ =>
        {
            DraftEvent?.Losses.Add(new EventLoss { Category = LossCategory.SCE, LossType = "Shakers" });
        });

        private ICommand? _removeDraftLossCommand;
        public ICommand RemoveDraftLossCommand => _removeDraftLossCommand ??= new RelayCommand(p =>
        {
            if (p is EventLoss loss) DraftEvent?.Losses.Remove(loss);
        });

        // ── DRAFT — add/remove base fluid ────────────────────────
        private ICommand? _addDraftFluidCommand;
        public ICommand AddDraftFluidCommand => _addDraftFluidCommand ??= new RelayCommand(_ =>
        {
            DraftEvent?.BaseFluidAdditions.Add(new EventBaseFluid { FluidType = BaseFluidType.Water });
        });

        private ICommand? _removeDraftFluidCommand;
        public ICommand RemoveDraftFluidCommand => _removeDraftFluidCommand ??= new RelayCommand(p =>
        {
            if (p is EventBaseFluid item) DraftEvent?.BaseFluidAdditions.Remove(item);
        });

        // ── DRAFT — add/remove chemical ──────────────────────────
        private ICommand? _addDraftChemicalCommand;
        public ICommand AddDraftChemicalCommand => _addDraftChemicalCommand ??= new RelayCommand(_ =>
        {
            DraftEvent?.Chemicals.Add(new EventChemical { ProductName = "New Chemical", SG = 1.0 });
        });

        private ICommand? _removeDraftChemicalCommand;
        public ICommand RemoveDraftChemicalCommand => _removeDraftChemicalCommand ??= new RelayCommand(p =>
        {
            if (p is EventChemical item) DraftEvent?.Chemicals.Remove(item);
        });

        // ── DRAFT — add/remove transfer ──────────────────────────
        private ICommand? _addDraftTransferCommand;
        public ICommand AddDraftTransferCommand => _addDraftTransferCommand ??= new RelayCommand(_ =>
        {
            DraftEvent?.Transfers.Add(new EventTransfer());
        });

        private ICommand? _removeDraftTransferCommand;
        public ICommand RemoveDraftTransferCommand => _removeDraftTransferCommand ??= new RelayCommand(p =>
        {
            if (p is EventTransfer item) DraftEvent?.Transfers.Remove(item);
        });

        // ── Validation ────────────────────────────────────────────
        private static List<string> ValidateDraft(VolumeBalanceEvent draft)
        {
            var errors = new List<string>();

            if (draft.HasUnlabeledLoss)
                errors.Add("All loss entries must have a Type selected.");

            if (draft.TankSnapshots.Any(t => t.IsNegative))
                errors.Add("Pit volume cannot be negative.");

            if (draft.TankSnapshots.Any(t => t.IsOverCapacity))
                errors.Add("One or more pits exceed their max capacity.");

            return errors;
        }

        // ── Trend data for chart ──────────────────────────────────
        /// <summary>Ordered list of (Timestamp, TotalPitVol) for the trend chart.</summary>
        public IList<(DateTime Time, double Volume)> TrendPoints =>
            Events
                .OrderBy(e => e.Timestamp)
                .Select(e => (e.Timestamp, e.TotalCurrentPitVol))
                .ToList();

        #endregion
    }

    internal class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => System.Windows.Input.CommandManager.RequerySuggested += value;
            remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
        }
    }
}
