using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
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

        public double TheoreticalWellbore => Math.Max(0, HoleCapacity - StringDisplacement);
        public double TotalWellTheoretical => StringTheoretical + AnnulusTheoretical;
        public double TotalWellActual => StringActual + AnnulusActual;
        public double WellVariance => TotalWellActual - TotalWellTheoretical;

        #endregion

        #region Surface Section (auto-populated from Rig Profile)

        public ObservableCollection<SurfaceTank> SurfaceTanks { get; } = new();

        public double TotalSurfaceVolume => SurfaceTanks.Sum(t => t.VolumeBbl);
        public double TotalActiveSurfaceVolume => SurfaceTanks.Where(IsActiveTank).Sum(t => t.VolumeBbl);
        public double TotalSurfaceMaxCapacity => SurfaceTanks.Sum(t => t.MaxCapacity);

        public void SyncFromRigProfile(IList<RigPit> activePits)
        {
            var existing = new Dictionary<string, SurfaceTank>(StringComparer.OrdinalIgnoreCase);
            var existingNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var tank in SurfaceTanks)
            {
                tank.PropertyChanged -= OnSurfaceTankPropertyChanged;

                var displayName = string.IsNullOrWhiteSpace(tank.Name) ? "Unnamed Pit" : tank.Name.Trim();
                var ordinal = existingNameCounts.TryGetValue(displayName, out var current) ? current + 1 : 1;
                existingNameCounts[displayName] = ordinal;
                existing[$"{displayName}#{ordinal}"] = tank;
            }

            SurfaceTanks.Clear();

            var incomingNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var position = 0;
            foreach (var pit in activePits)
            {
                position++;
                var displayName = string.IsNullOrWhiteSpace(pit.PitName)
                    ? $"Pit {((pit.No > 0) ? pit.No : position)}"
                    : pit.PitName.Trim();

                var ordinal = incomingNameCounts.TryGetValue(displayName, out var current) ? current + 1 : 1;
                incomingNameCounts[displayName] = ordinal;
                var lookupKey = $"{displayName}#{ordinal}";

                var tank = new SurfaceTank
                {
                    Name = displayName,
                    MaxCapacity = pit.MaxCapacity
                };

                if (existing.TryGetValue(lookupKey, out var oldTank))
                {
                    tank.VolumeBbl = oldTank.VolumeBbl;
                    tank.Classification = oldTank.Classification;
                    tank.FluidType = oldTank.FluidType;
                    tank.Density = oldTank.Density;
                    tank.YesterdayVol = oldTank.YesterdayVol;
                }
                else
                {
                    tank.VolumeBbl = pit.CurrentVolume;
                }

                tank.PropertyChanged += OnSurfaceTankPropertyChanged;
                SurfaceTanks.Add(tank);
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
        }

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
        }

        public void Detach()
        {
            WellContextService.Instance.GeometryDataUpdated -= OnGeometryDataUpdated;
            WellContextService.Instance.RigProfileUpdated -= OnRigProfileUpdated;
            WellContextService.Instance.ChemicalSelectionUpdated -= OnChemicalSelectionUpdated;

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

        private void OnGeometryDataUpdated(object? sender, GeometryDataUpdatedEventArgs e)
        {
            _lastGeometryArgs = e;
            ApplyGeometryData(e);
        }

        private void ApplyGeometryData(GeometryDataUpdatedEventArgs e)
        {
            HoleCapacity = Math.Round(e.HoleCapacity, 2);
            StringDisplacement = Math.Round(e.StringDisplacement, 2);
            StringTheoretical = Math.Round(e.StringInternalVolume, 2);
            AnnulusTheoretical = Math.Round(e.AnnularVolume, 2);

            LastSyncedAt = DateTime.Now.ToString("HH:mm:ss");
            IsGeometrySynced = true;
            RefreshSummary();
        }

        private void OnRigProfileUpdated(object? sender, RigProfileUpdatedEventArgs e)
        {
            SyncFromRigProfile(e.ActivePits);
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
