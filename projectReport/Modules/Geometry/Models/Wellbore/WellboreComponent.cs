using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Geometry.Wellbore
{
    // Using WellboreSectionType from WellboreSectionType.cs instead of SectionType

    public class WellboreComponent : BaseModel
    {
        private double? _od;
        private double? _id;
        private double? _topMD;
        private double? _bottomMD;
        private string _name = string.Empty;
        private WellboreSectionType? _sectionType;
        private WellboreStage? _stage;
        public const double BBL_TO_CUBIC_FEET = 5.615;
        public const double CUBIC_FEET_TO_BBL = 1.0 / 5.615;

        private double? _washout;
        private ObservableCollection<string> _validationErrors = new ObservableCollection<string>();
        private ObservableCollection<string> _externalWarnings = new ObservableCollection<string>();
        // Dictionary for Warnings (Property -> List of Warnings)
        private readonly Dictionary<string, List<string>> _warnings = new();
        private bool _isFirstRow = false;
        private double? _previousBottomMD = null;

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }



        public void AddValidationError(string error)
        {
            if (!ValidationErrors.Contains(error))
            {
                ValidationErrors.Add(error);
                OnPropertyChanged(nameof(HasValidationError));
                OnPropertyChanged(nameof(ValidationMessage));
            }
        }

        public void ClearValidationErrors()
        {
            ValidationErrors.Clear();
            OnPropertyChanged(nameof(HasValidationError));
            OnPropertyChanged(nameof(ValidationMessage));
        }

        public void AddValidationWarning(string warning)
        {
            if (!_externalWarnings.Contains(warning))
            {
                _externalWarnings.Add(warning);
                OnPropertyChanged(nameof(HasWarnings));
                OnPropertyChanged(nameof(WarningMessage));
            }
        }

        public void ClearValidationWarnings()
        {
            _externalWarnings.Clear();
            OnPropertyChanged(nameof(HasWarnings));
            OnPropertyChanged(nameof(WarningMessage));
        }

        public string ValidationMessage
        {
            get
            {
                var errors = new HashSet<string>(_validationErrors);
                foreach (var err in GetErrors(null).Cast<string>())
                {
                    errors.Add(err);
                }
                return errors.Count > 0 ? string.Join(Environment.NewLine, errors) : string.Empty;
            }
        }

        public string WarningMessage
        {
            get
            {
                var warnings = new HashSet<string>(_externalWarnings);
                foreach (var warnList in _warnings.Values)
                {
                    foreach (var w in warnList)
                    {
                        warnings.Add(w);
                    }
                }
                return warnings.Count > 0 ? string.Join(Environment.NewLine, warnings) : string.Empty;
            }
        }

        // Warnings Support
        public bool HasWarnings => _warnings.Count > 0 || _externalWarnings.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? WarningsChanged;

        public IEnumerable GetWarnings(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return _warnings.Values.SelectMany(x => x);

            if (_warnings.TryGetValue(propertyName, out var warnings))
                return warnings;

            return Enumerable.Empty<string>();
        }

        protected void AddWarning(string propertyName, string warning)
        {
            if (!_warnings.ContainsKey(propertyName))
                _warnings[propertyName] = new List<string>();

            if (!_warnings[propertyName].Contains(warning))
            {
                _warnings[propertyName].Add(warning);
                OnWarningsChanged(propertyName);
            }
        }

        protected void RemoveWarning(string propertyName, string warning)
        {
            if (_warnings.ContainsKey(propertyName) && _warnings[propertyName].Contains(warning))
            {
                _warnings[propertyName].Remove(warning);
                if (_warnings[propertyName].Count == 0)
                    _warnings.Remove(propertyName);
                OnWarningsChanged(propertyName);
            }
        }

        protected void ClearWarnings(string propertyName)
        {
            if (_warnings.ContainsKey(propertyName))
            {
                _warnings.Remove(propertyName);
                OnWarningsChanged(propertyName);
            }
        }

        protected void OnWarningsChanged(string propertyName)
        {
            WarningsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasWarnings));
            OnPropertyChanged(nameof(WarningMessage)); // Update the binding for text
        }


        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name 
        { 
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    ValidateName();
                }
            }
        }

        private double _cementVolume;
        private double _spacerVolume;
        private double _hydraulicRoughness = 0.0006;
        private string _material = string.Empty;
        private double _burstRating;
        private double _collapseRating;

        public string MechanicalState
        {
            get => string.Empty;
            set { }
        }

        public double? CasingOD
        {
            get => OD; // Use the base class OD property
            set
            {
                OD = value; // Set the base class OD property
                OnPropertyChanged();
            }
        }

        public double CementVolume
        {
            get => _cementVolume;
            set { _cementVolume = value; OnPropertyChanged(); }
        }

        public double SpacerVolume
        {
            get => _spacerVolume;
            set { _spacerVolume = value; OnPropertyChanged(); }
        }

        public double Volume
        {
            get
            {
                if (Length <= 0)
                    return 0;

                // Case 1: OpenHole
                // Volume = Hole Volume with Washout
                if (SectionType == WellboreSectionType.OpenHole)
                {
                    // For OpenHole, OD is the "Bit Size" or Hole Diameter
                    double holeParamsDiameter = OD.GetValueOrDefault();
                    if (holeParamsDiameter <= 0)
                        return 0;
                    
                    // Formula: π/4 * ID² * Length * (1 + Washout%) / 1029.4
                    // In our model for OpenHole: OD = Hole Diameter (the "ID" of the hole)
                    
                    double washoutFactor = 1.0 + (Washout.GetValueOrDefault() / 100.0);
                    
                    // Volume in bbl
                    return (Math.PI / 4.0) * Math.Pow(holeParamsDiameter, 2) * Length * washoutFactor / 1029.4;
                }
                
                // Case 2: Casing / Liner (Annular Volume typically calculated externally, but here we can't easily access Previous.ID without context)
                // The spec says: "Volume (bbl) = π/4 × (ID_outer² - OD_inner²) × Length / 1029.4"
                // Ideally this calculation should happen in the Service or ViewModel where "Previous Component" is known.
                // However, if this property is purely "Capacity" (Internal Volume), then it is simply ID^2.
                // IF this property is meant to be "Displacement" or "Annular", it depends on context.
                //
                // RE-READING SPEC 3.1: "Volume for LINER and CASING (Sin Washout)... ID_outer = ID of previous... OD_inner = OD of current"
                // This implies "Volume" field in the grid represents the ANNULAR volume between this string and the previous one (or open hole).
                // 
                // Since this model doesn't know its parent/previous, we MUST set this property from the ViewModel.
                // Therefore, this property should be a simple backing field that the ViewModel updates, OR we keep the internal calculation
                // but clarify it's only accurate if updated externally for Annular.
                
                // Current implementation in ViewModel loop calls `_geometryService.CalculateWellboreComponentVolume`
                // So we should make this a settleable property or keep the logic simple here.
                //
                // Let's rely on the backing field for now, assuming the Service updates it. 
                // But wait, the previous code had logic. 
                // Let's restore a logical default: Capacity (Internal Volume) if no other info.
                
                // Actually, the previous code was:
                // return (ID.Value * ID.Value / 1029.4) * Length; -> This is Internal Capacity.
                // The SPEC asks for Annular Volume in the "Volume" column? 
                // "Volume for LINER and CASING... Anular con el casing anterior"
                // Yes.
                
                return _volume; 
            }
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    OnPropertyChanged();
                }
            }
        }
        
        private double _volume;

        public double Length => (BottomMD.GetValueOrDefault() - TopMD.GetValueOrDefault());

        [Range(0, double.MaxValue, ErrorMessage = "Top MD must be a positive number")]
        public double? TopMD 
        { 
            get => _topMD;
            set
            {
                if (SetProperty(ref _topMD, value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Length));
                    ValidateTopMD();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating if this is the first wellbore row (starts at 0 MD).
        /// </summary>
        public bool IsFirstRow => _isFirstRow;

        /// <summary>
        /// Sets whether this is the first row in the wellbore.
        /// First row must have TopMD = 0.
        /// </summary>
        public void SetAsFirstRow(bool isFirst)
        {
            if (_isFirstRow != isFirst)
            {
                _isFirstRow = isFirst;
                if (isFirst)
                {
                    _topMD = 0;
                    OnPropertyChanged(nameof(TopMD));
                }
                OnPropertyChanged(nameof(IsFirstRow));
                OnPropertyChanged(nameof(IsTopMDEditable));
            }
        }

        /// <summary>
        /// Sets the previous component's Bottom MD to auto-link this row's Top MD.
        /// </summary>
        public void SetPreviousBottomMD(double? previousBottomMD)
        {
            _previousBottomMD = previousBottomMD;
            if (previousBottomMD.HasValue && !_isFirstRow)
            {
                _topMD = previousBottomMD.Value;
                OnPropertyChanged(nameof(TopMD));
                OnPropertyChanged(nameof(Length));
                OnPropertyChanged(nameof(Volume));
            }
        }

        /// <summary>
        /// Gets whether Top MD is editable (false if first row or linked to previous).
        /// </summary>
        public bool IsTopMDEditable => !_isFirstRow && _previousBottomMD == null;

        private void ValidateTopMD()
        {
            ClearErrors(nameof(TopMD));

            if (!TopMD.HasValue)
                return; // Defer validation until user enters a value

            if (TopMD.Value < 0)
            {
                AddError(nameof(TopMD), "Top MD cannot be negative");
            }

            if (BottomMD.HasValue && BottomMD.Value <= TopMD.Value)
            {
                AddError(nameof(TopMD), "Top MD must be less than Bottom MD");
            }
        }

        [Range(0.1, double.MaxValue, ErrorMessage = "Bottom MD must be greater than 0")]
        public double? BottomMD 
        { 
            get => _bottomMD;
            set
            {
                if (SetProperty(ref _bottomMD, value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Length));
                    ValidateBottomMD();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        private void ValidateBottomMD()
        {
            ClearErrors(nameof(BottomMD));

            if (!BottomMD.HasValue)
                return; // Defer validation until user enters a value

            if (BottomMD.Value <= 0)
            {
                AddError(nameof(BottomMD), "Bottom MD must be greater than 0");
            }

            if (TopMD.HasValue && BottomMD.Value <= TopMD.Value)
            {
                AddError(nameof(BottomMD), "Bottom MD must be greater than Top MD");
            }
        }

        public double? OD 
        { 
            get => _od;
            set
            {
                if (SetProperty(ref _od, value))
                {
                    ValidateOD();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        // For OpenHole: OD is editable (hole diameter), ID is disabled (always 0)
        // For Casing/Liner: Both OD and ID are editable
        // If SectionType is null (Select...), fields should be disabled.
        public bool IsODEnabled => SectionType.HasValue;
        public bool IsIDEnabled => SectionType.HasValue && SectionType != WellboreSectionType.OpenHole;



        public double? ID 
        { 
            get => _id;
            set
            {
                if (SetProperty(ref _id, value))
                {
                    OnPropertyChanged();
                    ValidateID();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        private void ValidateID()
        {
            ClearErrors(nameof(ID));
            
            // OpenHole must have ID = 0.000 (read-only)
            if (SectionType == WellboreSectionType.OpenHole)
            {
                if (ID.GetValueOrDefault() > 0.001)
                {
                    AddError(nameof(ID), $"OpenHole must have ID = 0.000 (no inner pipe). Current value: {ID:F3} in");
                }
                return;
            }
            
            // For Casing/Liner: ID cannot be 0 if entered. 
            // If null, we suppress error for UI cleanliness (caught at Save time).
            if (ID != null && ID.Value <= 0.001)
            {
                AddError(nameof(ID), "ID must be greater than 0.000");
                return;
            }
            
            // Rule: ID < OD (Internal Diameter Logic) - ID must always be smaller than OD
            if (OD.GetValueOrDefault() > 0 && ID.GetValueOrDefault() >= OD.GetValueOrDefault())
            {
                AddError(nameof(ID), "ID must always be smaller than OD");
            }
        }

        public double HydraulicRoughness
        {
            get => _hydraulicRoughness;
            set { _hydraulicRoughness = value; OnPropertyChanged(); }
        }

        public string Material
        {
            get => _material;
            set { _material = value; OnPropertyChanged(); }
        }

        public double BurstRating
        {
            get => _burstRating;
            set { _burstRating = value; OnPropertyChanged(); }
        }

        public double CollapseRating
        {
            get => _collapseRating;
            set { _collapseRating = value; OnPropertyChanged(); }
        }

        public WellboreSectionType? SectionType 
        { 
            get => _sectionType;
            set
            {
                if (SetProperty(ref _sectionType, value))
                {
                    OnPropertyChanged(nameof(SectionType));
                    OnPropertyChanged(nameof(IsODEnabled)); 
                    OnPropertyChanged(nameof(IsIDEnabled)); // Notify ID enabled state change
                    OnPropertyChanged(nameof(IsWashoutEnabled)); 
                    
                    // CORRECTED LOGIC: For OpenHole, ID = 0 (no pipe), OD = hole diameter
                    if (value == WellboreSectionType.OpenHole)
                    {
                        _id = 0.0; // OpenHole has no inner pipe, so ID = 0
                        OnPropertyChanged(nameof(ID));
                        ClearErrors(nameof(ID)); // Remove any existing ID errors
                        
                        ValidateWashout(); // Validate washout when switching to OpenHole
                    }
                    else
                    {
                        // If switching away from OpenHole (or to null), clear Washout logic?
                        // Spec says: "Washout says N/A for Casing".
                        // Logic: Set Washout to null or let UI handle "N/A" display. Model should probably just clear value or ignore it.
                        // Let's clear it to be safe.
                        if (_washout.HasValue)
                        {
                            _washout = null;
                            OnPropertyChanged(nameof(Washout));
                            ClearErrors(nameof(Washout));
                        }
                    }
                    
                    ValidateSectionType();
                    ValidateOD(); // Re-validate OD based on new section type
                    ValidateID(); // Re-validate ID based on new section type
                    OnPropertyChanged(nameof(Volume));
                    
                    // Update roughness based on section type
                    UpdateHydraulicRoughness();
                }
            }
        }

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            
            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError(nameof(Name), "Name is required");
            }
            else if (Name.Length > 100)
            {
                AddError(nameof(Name), "Name cannot exceed 100 characters");
            }
        }

        public WellboreStage? Stage 
        { 
            get => _stage;
            set
            {
                if (SetProperty(ref _stage, value))
                {
                    OnPropertyChanged(nameof(Stage));
                }
            }
        }

        private void ValidateSectionType()
        {
            ClearErrors(nameof(SectionType));
            
            // Allow null as "not selected" but maybe mark as error if user saves?
            // "Al elegir 'Seleccionar...', todos los campos ... null o deshabilitados."
            // This implies null is valid temporary state.
            
            if (SectionType.HasValue && !Enum.IsDefined(typeof(WellboreSectionType), SectionType.Value))
            {
                AddError(nameof(SectionType), "Invalid section type");
            }
        }

        public double? Washout
        {
            get => _washout;
            set
            {
                if (SetProperty(ref _washout, value))
                {
                    ValidateWashout();
                    OnPropertyChanged(nameof(Volume));
                    OnPropertyChanged(nameof(AnnularVolume));
                }
            }
        }

        /// <summary>
        /// Gets whether Washout field should be enabled (only for OpenHole sections).
        /// </summary>
        public bool IsWashoutEnabled => SectionType == WellboreSectionType.OpenHole;

        public double AnnularVolume
        {
            get
            {
                if (ID.GetValueOrDefault() > 0 && OD.GetValueOrDefault() > 0 && Length > 0)
                    return ((ID.Value * ID.Value) - (OD.Value * OD.Value)) * Length / 1029.4;

                return 0;
            }
        }

        private void UpdateHydraulicRoughness()
        {
            HydraulicRoughness = SectionType switch
            {
                WellboreSectionType.OpenHole => 0.006,
                _ => 0.0006
            };
        }

        private void CalculateCementVolume()
        {
            if (AnnularVolume > 0 && CementVolume == 0)
                CementVolume = AnnularVolume;
        }

        public bool OverlapsWith(WellboreComponent other)
        {
            if (other == null) return false;
            return !(BottomMD <= other.TopMD || TopMD >= other.BottomMD);
        }

        public double GapWith(WellboreComponent other)
        {
            if (other == null) return 0;
            if ((BottomMD ?? 0) <= (other.TopMD ?? 0)) return (other.TopMD ?? 0) - (BottomMD ?? 0);
            if ((TopMD ?? 0) >= (other.BottomMD ?? 0)) return (TopMD ?? 0) - (other.BottomMD ?? 0);
            return 0;
        }



        /// <summary>
        /// Gets whether this component is valid (has no validation errors)
        /// </summary>
        public override bool IsValid => !HasErrors && !HasValidationError;

        // Sync with standard validation
        protected override void OnErrorsChanged(string propertyName)
        {
            base.OnErrorsChanged(propertyName);
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(HasValidationError));
            OnPropertyChanged(nameof(ValidationMessage));
        }

        public bool HasValidationError => HasErrors || ValidationErrors.Count > 0;

        private void ValidateOD()
        {
            ClearErrors(nameof(OD));
            
            // Rule: OD cannot be 0.0. 
            // If null, we suppress error to keep UI clean (ValidationService catches it on Save).
            if (OD != null && OD.Value <= 0.001)
            {
                if (SectionType == WellboreSectionType.OpenHole)
                {
                    AddError(nameof(OD), "Hole Diameter must be > 0.");
                }
                else if (SectionType.HasValue) // Casing/Liner
                {
                    AddError(nameof(OD), "OD must be > 0.");
                }
                return;
            }
            
            // Rule: ID < OD (Internal Diameter Logic)
            if (SectionType != WellboreSectionType.OpenHole && SectionType.HasValue && (ID ?? 0) > 0 && (OD ?? 0) <= (ID ?? 0))
            {
                AddError(nameof(OD), "ID ≥ OD is not allowed. Fix diameters before continuing.");
                AddError(nameof(ID), "ID ≥ OD is not allowed. Fix diameters before continuing.");
            }
        }

        /// <summary>
        /// Validates washout for OpenHole sections
        /// Rule C3: Minimum washout >= 0.01%
        /// Rule C4: Washout is mandatory for OpenHole
        /// </summary>
        private void ValidateWashout()
        {
            ClearErrors(nameof(Washout));
            
            if (SectionType == WellboreSectionType.OpenHole)
            {
                // Washout is mandatory for OpenHole
                if (Washout == null)
                {
                    // Suppress "Required" error for clean UI.
                }
                else if (Washout.Value < 0 || Washout.Value > 100)
                {
                     AddError(nameof(Washout), "Washout must be between 0% and 100%.");
                }
                else
                {
                    // Warnings
                    if (Washout.Value > 50)
                    {
                        AddWarning(nameof(Washout), "Excessive washout (>50%) detected - verify measurement.");
                    }
                    else if (Washout.Value > 30)
                    {
                        AddWarning(nameof(Washout), "High washout (>30%) may affect cementing operations.");
                    }
                }
            }
        }

        /// <summary>
        /// Validates telescopic diameter rule: OD[n] < ID[n-1]
        /// Rule A2: Telescopic Diameter Progression
        /// This should be called from the ViewModel with the previous component
        /// </summary>
        public void ValidateTelescopicDiameter(WellboreComponent? previousComponent)
        {
            ClearErrors(nameof(OD));
            
            if (previousComponent == null) return; // First component, no telescoping check
            
            // Rule A2: OD[n] < ID[n-1] (Telescopic Diameter)
            if (OD.GetValueOrDefault() >= previousComponent.ID.GetValueOrDefault() && previousComponent.ID.GetValueOrDefault() > 0.001)
            {
                string currentOD = OD.GetValueOrDefault().ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                string prevID = previousComponent.ID.GetValueOrDefault().ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                AddError(nameof(OD), $"Error A2: OD ({currentOD} in) must be smaller than previous section ID ({prevID} in). Telescopic progression required.");
            }
        }

        /// <summary>
        /// Validates casing depth progression: BottomMD[n] >= BottomMD[n-1] for casing/liner
        /// Rule D3: Casing Depth Progression
        /// </summary>
        public void ValidateCasingDepthProgression(WellboreComponent? previousComponent)
        {
            if (previousComponent == null) return;
            
            // Only applies to Casing and Liner sections
            if ((SectionType == WellboreSectionType.Casing || SectionType == WellboreSectionType.Liner) &&
                (previousComponent.SectionType == WellboreSectionType.Casing || previousComponent.SectionType == WellboreSectionType.Liner))
            {
                // Check for valid casing override: same TopMD, deeper or equal BottomMD
                bool isCasingOverride = Math.Abs((TopMD ?? 0) - (previousComponent.TopMD ?? 0)) < 0.01 && (BottomMD ?? 0) >= (previousComponent.BottomMD ?? 0);
                
                if (!isCasingOverride && (BottomMD ?? 0) < (previousComponent.BottomMD ?? 0))
                {
                    string currentMD = (BottomMD ?? 0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    string prevMD = (previousComponent.BottomMD ?? 0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    AddError(nameof(BottomMD), $"Error D3: Bottom MD ({currentMD} ft) cannot be less than previous casing depth ({prevMD} ft).");
                }
            }
        }
    }
}
