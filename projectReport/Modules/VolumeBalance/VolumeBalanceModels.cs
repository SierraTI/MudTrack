using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProjectReport.Modules.VolumeBalance
{
    // ─────────────────────────────────────────────────────────────
    // ENUMS
    // ─────────────────────────────────────────────────────────────

    public enum ActivityType
    {
        Drilling,
        Tripping,
        Mixing,
        Cementing,
        Displacement,
        Circulating,
        Other
    }

    public enum LossCategory
    {
        SCE,        // Surface Control Equipment: Shakers, Centrifuges, Mud Cleaners
        Downhole,   // Filtration, Lost in Hole, Left Behind Casing
        Misc        // Evaporation, Trips, Displacement
    }

    public enum SystemType
    {
        Active,
        Reserve,
        Other
    }

    public enum BaseFluidType
    {
        Water,
        DewateringWater,
        OsmosisWater,
        Oil,
        OilBased,
        Influx,
        Other
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT TANK SNAPSHOT  — one row per tank per event
    // ─────────────────────────────────────────────────────────────

    public class EventTankSnapshot : INotifyPropertyChanged
    {
        private string _tankName = string.Empty;
        private string _classification = "Active";
        private string _tankSubtype = string.Empty; // e.g. "Mixing Pit", "Slug Pit"
        private double _previousVol;
        private double _currentVol;
        private double _density;
        private double _maxCapacity;

        public string TankName
        {
            get => _tankName;
            set { _tankName = value; OnPropertyChanged(); }
        }

        public string Classification
        {
            get => _classification;
            set { _classification = value; OnPropertyChanged(); }
        }

        public string TankSubtype
        {
            get => _tankSubtype;
            set { _tankSubtype = value; OnPropertyChanged(); }
        }

        public double PreviousVol
        {
            get => _previousVol;
            set { _previousVol = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumeDelta)); }
        }

        public double CurrentVol
        {
            get => _currentVol;
            set
            {
                _currentVol = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumeDelta));
                OnPropertyChanged(nameof(PercentFull));
                OnPropertyChanged(nameof(IsOverCapacity));
                OnPropertyChanged(nameof(IsNegative));
            }
        }

        public double Density
        {
            get => _density;
            set { _density = value; OnPropertyChanged(); }
        }

        public double MaxCapacity
        {
            get => _maxCapacity;
            set { _maxCapacity = value; OnPropertyChanged(); OnPropertyChanged(nameof(PercentFull)); OnPropertyChanged(nameof(IsOverCapacity)); }
        }

        public double VolumeDelta => CurrentVol - PreviousVol;
        public double PercentFull => MaxCapacity > 0 ? Math.Round((CurrentVol / MaxCapacity) * 100, 1) : 0;
        public bool IsOverCapacity => MaxCapacity > 0 && CurrentVol > MaxCapacity;
        public bool IsNegative => CurrentVol < 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT BASE FLUID  — liquid additions
    // ─────────────────────────────────────────────────────────────

    public class EventBaseFluid : INotifyPropertyChanged
    {
        private BaseFluidType _fluidType = BaseFluidType.Water;
        private SystemType _systemAssignment = SystemType.Active;
        private double _volumeBbl;

        public BaseFluidType FluidType
        {
            get => _fluidType;
            set { _fluidType = value; OnPropertyChanged(); OnPropertyChanged(nameof(FluidTypeLabel)); }
        }

        public SystemType SystemAssignment
        {
            get => _systemAssignment;
            set { _systemAssignment = value; OnPropertyChanged(); }
        }

        public string FluidTypeLabel => FluidType.ToString();

        private double _gallons;
        public double Gallons
        {
            get => _gallons;
            set 
            { 
                _gallons = value; 
                OnPropertyChanged(); 
                VolumeBbl = Math.Round(_gallons / 42.0, 2); 
            }
        }

        public double VolumeBbl
        {
            get => _volumeBbl;
            set { _volumeBbl = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT LOSS  — categorised loss entry
    // ─────────────────────────────────────────────────────────────

    public class EventLoss : INotifyPropertyChanged
    {
        private LossCategory _category = LossCategory.SCE;
        private string _lossType = string.Empty;   // e.g. "Shakers", "Filtration"
        private double _volumeBbl;

        public LossCategory Category
        {
            get => _category;
            set 
            { 
                _category = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(CategoryLabel)); 
                OnPropertyChanged(nameof(AvailableLossTypes));
                // Automatically set a valid default loss type when category changes
                LossType = AvailableLossTypes.FirstOrDefault() ?? string.Empty;
            }
        }

        public string CategoryLabel => Category.ToString();

        public IEnumerable<string> AvailableLossTypes
        {
            get
            {
                switch (Category)
                {
                    case LossCategory.SCE:
                        return new[] { "Shakers", "Centrifuges", "Mud Cleaners", "Desilter", "Other SCE" };
                    case LossCategory.Misc:
                        return new[] { "Evaporation", "Trips", "Displacement", "Surface Leaks", "Other Misc" };
                    case LossCategory.Downhole:
                        return new[] { "Filtration", "Lost in Hole", "Left Behind Casing", "Seepage", "Other Downhole" };
                    default:
                        return Array.Empty<string>();
                }
            }
        }

        public string LossType
        {
            get => _lossType;
            set { _lossType = value; OnPropertyChanged(); }
        }

        public double VolumeBbl
        {
            get => _volumeBbl;
            set { _volumeBbl = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT CHEMICAL  — chemical addition with ppb tracking
    // ─────────────────────────────────────────────────────────────

    public class EventChemical : INotifyPropertyChanged
    {
        private string _productCode = string.Empty;
        private string _productName = string.Empty;
        private double _quantityLbs;
        private double _totalLbs;         // The actual recorded pounds (if different from qty * conc)
        private double _volumeBbl;
        private double _sg;
        private double _systemVolumeBbl;   // set from parent event for ppb calc
        private SystemType _systemAssignment = SystemType.Active;

        public string ProductCode
        {
            get => _productCode;
            set { _productCode = value; OnPropertyChanged(); }
        }

        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        /// <summary>Quantity added in pounds.</summary>
        public double QuantityLbs
        {
            get => _quantityLbs;
            set 
            { 
                _quantityLbs = value; 
                TotalLbs = value; // Keep them in sync by default
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ConcentrationPpb)); 
            }
        }

        /// <summary>Volume contributed in barrels (auto-derived from lbs and SG).</summary>
        public double VolumeBbl
        {
            get => _volumeBbl;
            set { _volumeBbl = value; OnPropertyChanged(); }
        }

        public double SG
        {
            get => _sg;
            set { _sg = value; OnPropertyChanged(); RecalculateVolume(); }
        }

        public double TotalLbs
        {
            get => _totalLbs;
            set { _totalLbs = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsChemicalBalanced)); RecalculateVolume(); }
        }

        public SystemType SystemAssignment
        {
            get => _systemAssignment;
            set { _systemAssignment = value; OnPropertyChanged(); }
        }

        private void RecalculateVolume()
        {
            if (SG <= 0) 
            { 
                VolumeBbl = 0; 
                return; 
            }
            VolumeBbl = Math.Round(TotalLbs / (349.86 * SG), 2);
        }

        /// <summary>Total system volume (bbl) — injected from parent event to calculate ppb.</summary>
        public double SystemVolumeBbl
        {
            get => _systemVolumeBbl;
            set { _systemVolumeBbl = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConcentrationPpb)); }
        }

        /// <summary>Concentration in pounds per barrel across the active system.</summary>
        public double ConcentrationPpb => SystemVolumeBbl > 0 ? Math.Round(QuantityLbs / SystemVolumeBbl, 3) : 0;

        /// <summary>Rule: Ensures pounds added matches volume and recorded ppb.</summary>
        public bool IsChemicalBalanced => Math.Abs(TotalLbs - QuantityLbs) < 0.1; 

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT TRANSFER  — moving volume between pits
    // ─────────────────────────────────────────────────────────────

    public class EventTransfer : INotifyPropertyChanged
    {
        private string _fromTank = string.Empty;
        private string _toTank = string.Empty;
        private double _volumeBbl;

        public string FromTank
        {
            get => _fromTank;
            set { _fromTank = value; OnPropertyChanged(); }
        }

        public string ToTank
        {
            get => _toTank;
            set { _toTank = value; OnPropertyChanged(); }
        }

        public double VolumeBbl
        {
            get => _volumeBbl;
            set { _volumeBbl = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────
    // VOLUME BALANCE EVENT  — one ledger entry
    // ─────────────────────────────────────────────────────────────

    public class VolumeBalanceEvent : INotifyPropertyChanged
    {
        private static int _idSeed = 1;

        public int Id { get; } = _idSeed++;

        private DateTime _timestamp = DateTime.Now;
        private ActivityType _activity = ActivityType.Drilling;
        private double _depthFt;
        private string _notes = string.Empty;

        // Wellbore snapshot (populated from Geometry at moment of event creation)
        private double _stringVolBbl;
        private double _annulusVolBbl;

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimestampLabel)); }
        }

        public string TimestampLabel => Timestamp.ToString("MM/dd HH:mm");

        public ActivityType Activity
        {
            get => _activity;
            set { _activity = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActivityLabel)); }
        }

        public string ActivityLabel => Activity.ToString();

        public double DepthFt
        {
            get => _depthFt;
            set { _depthFt = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public double StringVolBbl
        {
            get => _stringVolBbl;
            set { _stringVolBbl = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalWellboreBbl)); }
        }

        public double AnnulusVolBbl
        {
            get => _annulusVolBbl;
            set { _annulusVolBbl = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalWellboreBbl)); }
        }

        public double TotalWellboreBbl => StringVolBbl + AnnulusVolBbl;

        // Sub-collections
        public ObservableCollection<EventTankSnapshot> TankSnapshots { get; } = new();
        public ObservableCollection<EventBaseFluid> BaseFluidAdditions { get; } = new();
        public ObservableCollection<EventLoss> Losses { get; } = new();
        public ObservableCollection<EventChemical> Chemicals { get; } = new();
        public ObservableCollection<EventTransfer> Transfers { get; } = new();

        // ── Calculated Totals ──────────────────────────────────────

        public double TotalPreviousPitVol => TankSnapshots.Sum(t => t.PreviousVol);
        public double TotalCurrentPitVol  => TankSnapshots.Sum(t => t.CurrentVol);

        public double TotalLiquidAdditions => BaseFluidAdditions.Sum(f => f.VolumeBbl);
        public double TotalLosses          => Losses.Sum(l => l.VolumeBbl);
        public double TotalChemicalVolume  => Chemicals.Sum(c => c.VolumeBbl);

        /// <summary>What current pit vol SHOULD be based on accounting: Prev + Fluid Additions − Losses.</summary>
        public double ProposedCurrentPitVol => TotalPreviousPitVol + TotalLiquidAdditions - TotalLosses;

        /// <summary>Difference between actual and proposed. Zero means perfectly balanced.</summary>
        public double PitVolumeVariance => TotalCurrentPitVol - ProposedCurrentPitVol;

        private const double BalanceTolerance = 1.0; // bbl

        public bool IsBalanced => Math.Abs(PitVolumeVariance) <= BalanceTolerance;
        public string BalanceStatusText => IsBalanced ? "OK" : $"{PitVolumeVariance:+0.0;-0.0} bbl";
        public string BalanceColor       => IsBalanced ? "#388E3C" : "#D32F2F";

        // Logic Gates indicators (Organized Spec)
        public string VolumeBalanceGateText => IsBalanced ? "OK" : $"DISCREPANCY: {Math.Abs(PitVolumeVariance):F1} bbl";
        public bool ChemicalBalanceGate => Chemicals.All(c => c.IsChemicalBalanced);
        public string ChemicalBalanceGateText => ChemicalBalanceGate ? "OK" : "DISCREPANCY: Lbs Mismatch";

        /// <summary>True if any loss entry has no type assigned.</summary>
        public bool HasUnlabeledLoss => Losses.Any(l => string.IsNullOrWhiteSpace(l.LossType));

        /// <summary>True if any tank has a negative or over-capacity volume.</summary>
        public bool HasVolumeViolation => TankSnapshots.Any(t => t.IsNegative || t.IsOverCapacity);

        public bool IsValid => !HasUnlabeledLoss && !HasVolumeViolation && !TankSnapshots.Any(t => t.IsNegative);

        // ── Chemical ppb propagation ───────────────────────────────
 
         /// <summary>Pushes current system volume to all chemical rows so ppb is always current.</summary>
         public void RefreshChemicalConcentrations()
         {
             var sysVol = TotalCurrentPitVol > 0 ? TotalCurrentPitVol : TotalPreviousPitVol;
             foreach (var chem in Chemicals)
                 chem.SystemVolumeBbl = sysVol;
         }
 
        /// <summary>
        /// Resets all CurrentVol in TankSnapshots to their PreviousVol.
        /// Useful before applying automated adjustments.
        /// </summary>
        public void ResetCurrentVolumes()
        {
            foreach (var snap in TankSnapshots)
                snap.CurrentVol = snap.PreviousVol;
        }

        /// <summary>
        /// Automatically adjusts tank CurrentVol based on transfers, starting from PreviousVol.
        /// This ensures the operation is idempotent.
        /// </summary>
        public void ApplyTransfersToSnapshots()
        {
            ResetCurrentVolumes();

            foreach (var transfer in Transfers)
            {
                if (string.IsNullOrEmpty(transfer.FromTank) || string.IsNullOrEmpty(transfer.ToTank) || transfer.VolumeBbl == 0)
                    continue;

                var fromTank = TankSnapshots.FirstOrDefault(t => t.TankName == transfer.FromTank);
                var toTank = TankSnapshots.FirstOrDefault(t => t.TankName == transfer.ToTank);

                if (fromTank != null) fromTank.CurrentVol -= transfer.VolumeBbl;
                if (toTank != null) toTank.CurrentVol += transfer.VolumeBbl;
            }
        }

        // ── Wire sub-collection change notifications ───────────────

        public VolumeBalanceEvent()
        {
            TankSnapshots.CollectionChanged     += OnSubCollectionChanged;
            BaseFluidAdditions.CollectionChanged += OnSubCollectionChanged;
            Losses.CollectionChanged            += OnSubCollectionChanged;
            Chemicals.CollectionChanged         += OnSubCollectionChanged;
            Transfers.CollectionChanged         += OnSubCollectionChanged;
        }

        private void OnSubCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Wire up item-level change notifications
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += OnChildPropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= OnChildPropertyChanged;
            }

            RaiseCalculatedProperties();
        }

        private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaiseCalculatedProperties();
        }

         private void RaiseCalculatedProperties()
         {
             RefreshChemicalConcentrations();
            // We don't call ApplyTransfers here to avoid periodic recalculation loops, 
            // instead we will call it explicitly from the ViewModel when a transfer is finalized/changed.
 
             OnPropertyChanged(nameof(TotalPreviousPitVol));
            OnPropertyChanged(nameof(TotalCurrentPitVol));
            OnPropertyChanged(nameof(TotalLiquidAdditions));
            OnPropertyChanged(nameof(TotalLosses));
            OnPropertyChanged(nameof(TotalChemicalVolume));
            OnPropertyChanged(nameof(ProposedCurrentPitVol));
            OnPropertyChanged(nameof(PitVolumeVariance));
            OnPropertyChanged(nameof(IsBalanced));
            OnPropertyChanged(nameof(BalanceStatusText));
            OnPropertyChanged(nameof(BalanceColor));
            OnPropertyChanged(nameof(VolumeBalanceGateText));
            OnPropertyChanged(nameof(ChemicalBalanceGate));
            OnPropertyChanged(nameof(ChemicalBalanceGateText));
            OnPropertyChanged(nameof(HasUnlabeledLoss));
            OnPropertyChanged(nameof(HasVolumeViolation));
            OnPropertyChanged(nameof(IsValid));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
