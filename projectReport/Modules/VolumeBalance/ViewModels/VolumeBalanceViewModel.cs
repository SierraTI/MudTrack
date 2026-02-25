using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

        /// <summary>Volume added in barrels — auto-calculated from Qty, Unit, and SG.</summary>
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
    /// Data sources:
    ///   - Wellbore (Theoretical): auto-populated via WellContextService.GeometryDataUpdated
    ///   - Surface tanks: auto-populated via WellContextService.RigProfileUpdated
    ///   - Chemicals: manually added or linked from Inventory
    /// </summary>
    public class VolumeBalanceViewModel : BaseViewModel
    {
        #region ── Wellbore Section (auto-populated from Geometry) ───────────────

        private double _holeCapacity;
        /// <summary>Total capacity of the empty wellbore (bbl). From Geometry → Wellbore.</summary>
        public double HoleCapacity
        {
            get => _holeCapacity;
            private set { if (SetField(ref _holeCapacity, value)) RefreshSummary(); }
        }

        private double _stringDisplacement;
        /// <summary>Steel displacement when drill string is run in hole (bbl). From Geometry → Drill String.</summary>
        public double StringDisplacement
        {
            get => _stringDisplacement;
            private set { if (SetField(ref _stringDisplacement, value)) RefreshSummary(); }
        }

        private double _stringTheoretical;
        /// <summary>Internal volume of the drill string at bit depth (bbl).</summary>
        public double StringTheoretical
        {
            get => _stringTheoretical;
            private set { if (SetField(ref _stringTheoretical, value)) RefreshSummary(); }
        }

        private double _stringActual;
        /// <summary>Actual string volume measured at surface (manual input).</summary>
        public double StringActual
        {
            get => _stringActual;
            set { if (SetField(ref _stringActual, value)) RefreshSummary(); }
        }

        private double _annulusTheoretical;
        /// <summary>Active annular volume at bit depth (bbl).</summary>
        public double AnnulusTheoretical
        {
            get => _annulusTheoretical;
            private set { if (SetField(ref _annulusTheoretical, value)) RefreshSummary(); }
        }

        private double _annulusActual;
        /// <summary>Actual annular volume (manual reading).</summary>
        public double AnnulusActual
        {
            get => _annulusActual;
            set { if (SetField(ref _annulusActual, value)) RefreshSummary(); }
        }

        /// <summary>Theoretical total fluid in the wellbore = HoleCapacity − StringDisplacement (bbl).</summary>
        public double TheoreticalWellbore => Math.Max(0, HoleCapacity - StringDisplacement);

        /// <summary>Theoretical total of wellbore sections (String + Annulus).</summary>
        public double TotalWellTheoretical => StringTheoretical + AnnulusTheoretical;

        /// <summary>Actual total of wellbore sections (String + Annulus).</summary>
        public double TotalWellActual => StringActual + AnnulusActual;

        /// <summary>Wellbore-only variance (Actual − Theoretical).</summary>
        public double WellVariance => TotalWellActual - TotalWellTheoretical;

        #endregion

        #region ── Surface Section (auto-populated from Rig Profile) ─────────────

        public ObservableCollection<SurfaceTank> SurfaceTanks { get; } = new();

        /// <summary>Sum of current volume across all surface tanks (bbl).</summary>
        public double TotalSurfaceVolume => SurfaceTanks.Sum(t => t.VolumeBbl);

        /// <summary>Sum of max capacity across all surface tanks (bbl).</summary>
        public double TotalSurfaceMaxCapacity => SurfaceTanks.Sum(t => t.MaxCapacity);

        /// <summary>
        /// Sync surface tanks from a live RigProfile pit list.
        /// Preserves existing user-entered VolumeBbl values where tank name matches.
        /// </summary>
        public void SyncFromRigProfile(IList<RigPit> activePits)
        {
            // Keep existing user entries by name
            var existing = SurfaceTanks.ToDictionary(t => t.Name, t => t.VolumeBbl);
            SurfaceTanks.Clear();

            foreach (var pit in activePits)
            {
                var tank = new SurfaceTank
                {
                    Name = pit.PitName,
                    MaxCapacity = pit.MaxCapacity,
                    VolumeBbl = existing.TryGetValue(pit.PitName, out var vol) ? vol : pit.CurrentVolume
                };
                SurfaceTanks.Add(tank);
            }

            RefreshSummary();
            OnPropertyChanged(nameof(SurfaceTanks));
            OnPropertyChanged(nameof(TotalSurfaceVolume));
            OnPropertyChanged(nameof(TotalSurfaceMaxCapacity));
        }

        #endregion

        #region ── Chemicals Section (from Inventory) ───────────────────────────

        public ObservableCollection<ChemicalUsage> ChemicalUsages { get; } = new();

        /// <summary>Total volume added by all chemical additions (bbl).</summary>
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

        #region ── Golden Equation Summary ──────────────────────────────────────

        // The Golden Equation:
        //   V_variance = (V_initial + V_added) − (V_final + V_lost)
        // Simplified to:
        //   Actual Total  = (StringActual + AnnulusActual) + TotalSurface + ChemicalsAdded
        //   Theoretical Total = TheoreticalWellbore + TotalSurface
        //   Variance = Actual − Theoretical  (negative = loss, positive = gain/kick)

        /// <summary>Theoretical system total: TheoreticalWellbore + Surface max capacity baseline.</summary>
        public double SystemTotalTheoretical => TheoreticalWellbore + TotalSurfaceVolume;

        /// <summary>Actual system total: actual wellbore + actual surface + chemicals added.</summary>
        public double SystemTotalActual => TotalWellActual + TotalSurfaceVolume + TotalChemicalVolumeAdded;

        /// <summary>System variance (bbl). Negative = possible downhole loss. Positive = possible gain/kick.</summary>
        public double SystemVariance => SystemTotalActual - SystemTotalTheoretical;

        /// <summary>Human-readable status for the variance.</summary>
        public string VarianceStatus => SystemVariance switch
        {
            < -0.5 => "⚠ Possible Loss",
            > 0.5  => "⚠ Possible Gain / Kick",
            _      => "✓ Normal"
        };

        /// <summary>Variance color: Red for loss, Orange for gain/kick, DarkGreen for normal.</summary>
        public string VarianceColor => SystemVariance switch
        {
            < -0.5 => "#D32F2F",
            > 0.5  => "#E65100",
            _      => "#388E3C"
        };

        private void RefreshSummary()
        {
            OnPropertyChanged(nameof(TheoreticalWellbore));
            OnPropertyChanged(nameof(TotalWellTheoretical));
            OnPropertyChanged(nameof(TotalWellActual));
            OnPropertyChanged(nameof(WellVariance));
            OnPropertyChanged(nameof(TotalSurfaceVolume));
            OnPropertyChanged(nameof(TotalSurfaceMaxCapacity));
            OnPropertyChanged(nameof(TotalChemicalVolumeAdded));
            OnPropertyChanged(nameof(SystemTotalTheoretical));
            OnPropertyChanged(nameof(SystemTotalActual));
            OnPropertyChanged(nameof(SystemVariance));
            OnPropertyChanged(nameof(VarianceStatus));
            OnPropertyChanged(nameof(VarianceColor));
        }

        #endregion

        #region ── Sync / Last Updated ──────────────────────────────────────────

        private string _lastSyncedAt = "Not yet synced";
        /// <summary>Human-readable timestamp of the last geometry data sync.</summary>
        public string LastSyncedAt
        {
            get => _lastSyncedAt;
            private set { _lastSyncedAt = value; OnPropertyChanged(); }
        }

        private bool _isGeometrySynced;
        /// <summary>True once geometry data has been received at least once.</summary>
        public bool IsGeometrySynced
        {
            get => _isGeometrySynced;
            private set { _isGeometrySynced = value; OnPropertyChanged(); }
        }

        private ICommand? _syncCommand;
        /// <summary>Manually triggers republishing of geometry data from WellContextService last known values.</summary>
        public ICommand SyncCommand => _syncCommand ??= new RelayCommand(_ => RequestGeometryResync());

        /// <summary>
        /// Asks the geometry module to re-broadcast its current calculated data.
        /// This is done by replaying the last event args cached locally.
        /// </summary>
        private void RequestGeometryResync()
        {
            if (_lastGeometryArgs != null)
                ApplyGeometryData(_lastGeometryArgs);
        }

        private GeometryDataUpdatedEventArgs? _lastGeometryArgs;

        #endregion

        #region ── Constructor & Event Subscriptions ─────────────────────────────

        public VolumeBalanceViewModel()
        {
            // Subscribe to cross-module events
            WellContextService.Instance.GeometryDataUpdated += OnGeometryDataUpdated;
            WellContextService.Instance.RigProfileUpdated += OnRigProfileUpdated;
            WellContextService.Instance.ChemicalSelectionUpdated += OnChemicalSelectionUpdated;

            // Wire SurfaceTank collection changes → RefreshSummary
            SurfaceTanks.CollectionChanged += (_, __) => RefreshSummary();

            // Wire ChemicalUsage collection changes → RefreshSummary
            ChemicalUsages.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                    foreach (ChemicalUsage c in e.NewItems)
                        c.PropertyChanged += (__, ___) => RefreshSummary();
                RefreshSummary();
            };
        }

        /// <summary>Unsubscribe to prevent memory leaks (call from View's Unloaded event).</summary>
        public void Detach()
        {
            WellContextService.Instance.GeometryDataUpdated -= OnGeometryDataUpdated;
            WellContextService.Instance.RigProfileUpdated -= OnRigProfileUpdated;
            WellContextService.Instance.ChemicalSelectionUpdated -= OnChemicalSelectionUpdated;
        }

        #endregion

        #region ── Event Handlers ────────────────────────────────────────────────

        private void OnChemicalSelectionUpdated(object? sender, ChemicalSelectionUpdatedEventArgs e)
        {
            // Add freshly selected chemicals to our local additions list
            foreach (var item in e.SelectedItems)
            {
                // Simple check to avoid duplicates if user clicks save multiple times with same items
                if (ChemicalUsages.Any(c => c.ProductCode == item.Code)) continue;

                ChemicalUsages.Add(new ChemicalUsage
                {
                    ProductCode = item.Code,
                    Description = item.Nombre,
                    QtyUsed = 1.0, // Default to 1 unit
                    Unit = item.Unidad,
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
            // These setters fire RefreshSummary each time, but we batch at the end for clarity
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

        #endregion

        #region ── INotifyPropertyChanged helper ─────────────────────────────────

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName!);
            return true;
        }

        #endregion
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Minimal RelayCommand for commands inside this module
    // ─────────────────────────────────────────────────────────────────────────────
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
