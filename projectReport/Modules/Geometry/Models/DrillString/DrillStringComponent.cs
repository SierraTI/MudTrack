using System;
using System.Collections.Generic;
using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.BitAndJets;
using ProjectReport.Models.Geometry.FluidsAndPressure;
using BitJetsConfigModel = ProjectReport.Models.Geometry.BitJetsConfig;
using PressureDropConfigModel = ProjectReport.Models.Geometry.PressureDropConfig;

namespace ProjectReport.Models.Geometry.DrillString
{
    public class DrillStringComponent : BaseModel
    {
        private const double CUBIC_FEET_TO_BBL = 0.178107607; // 1 cubic foot = 0.178107607 barrels

        private double? _length;
        private double? _od;
        private double? _id;
        private double _volume;
        private ComponentType _componentType;
        private string _name = string.Empty;

        // Tubular properties
        private double? _toolJointOD;
        private double? _toolJointId;
        private double? _jointLength;
        private double? _toolJointLength;
        private double? _weightPerFoot;
        private double _buoyancyFactor = 0.85;

        // Fluid/hydraulic properties
        private double? _fluidDensity;
        private List<PressureDropPoint> _pressureDropPoints;

        // ✅ Jets only relevant if this component is BIT
        public BitJetSet Jets { get; set; } = new BitJetSet();

        // Configuration objects
        public ToolJointConfig? ToolJoint { get; set; }
        public PressureDropConfigModel? PressureDropConfig { get; set; }
        public BitJetsConfigModel? BitJetsConfig { get; set; }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public double Volume
        {
            get => _volume;
            set { _volume = value; OnPropertyChanged(); }
        }

        public double InternalVolume
        {
            get
            {
                // Internal Volume (bbl) = (ID² / 1029.4) × Length
                if (ID.GetValueOrDefault() <= 0 || Length.GetValueOrDefault() <= 0)
                    return 0;

                return (ID.Value * ID.Value / 1029.4) * Length.Value;
            }
        }

        // SRS Formula: Displacement Volume (bbl) = (OD² - ID²) / 1029.4 × Length
        public double DisplacementVolume
        {
            get
            {
                // SRS Formula: Displacement Volume (bbl) = (OD² - ID²) / 1029.4 × Length
                // Where OD = Outer Diameter (inches), ID = Inner Diameter (inches), Length (feet)
                if (OD.GetValueOrDefault() <= 0 || Length.GetValueOrDefault() <= 0)
                    return 0;
                
                // If ID is null/0, it acts as a solid bar (Displacement = Volume of cylinder)
                // If ID exists, subtract it
                double validId = ID.GetValueOrDefault();
                return ((OD.Value * OD.Value) - (validId * validId)) / 1029.4 * Length.Value;
            }
        }

        public double? Length
        {
            get => _length;
            set 
            { 
                if (SetProperty(ref _length, value))
                {
                    OnPropertyChanged(nameof(NumberOfJoints)); 
                    OnPropertyChanged(nameof(InternalVolume)); 
                    OnPropertyChanged(nameof(DisplacementVolume));
                    ValidateLength();
                }
            }
        }

        private void ValidateLength()
        {
            ClearErrors(nameof(Length));
            if (Length == null)
            {
                 // Suppress error for clean UI on start, typically ViewModel handles "Required" or we leave blank
                 // But validation often wants to flag emptiness.
                 // Strategy: Don't error on null if we want "clean start", ONLY error if 0 or negative clearly entered?
                 // No, standard practice for "Required" fields:
                 // We'll let null pass but UI might mark it if submitted.
                 // Actually, user wants "blank", but eventually it must be valid.
                 // Let's rely on standard IDataErrorInfo: if it's null, is it Valid? No.
                 AddError(nameof(Length), "Length is required");
            }
            else if (Length <= 0)
            {
                AddError(nameof(Length), "Length must be > 0");
            }
        }

        public int NumberOfJoints => (JointLength.GetValueOrDefault() > 0 && Length.GetValueOrDefault() > 0) 
            ? (int)Math.Ceiling(Length.Value / JointLength.Value) 
            : 0;

        public ComponentType ComponentType
        {
            get => _componentType;
            set
            {
                _componentType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ComponentTypeString));

                // ✅ Only Bit keeps jets
                if (value != ComponentType.Bit)
                {
                    Jets?.Clear();
                    IsTfaConfigured = false;
                }
            }
        }

        // ... ComponentTypeString kept as is (no change needed usually) ...

        public string ComponentTypeString
        {
            get => ComponentType switch
            {
                ComponentType.DrillPipe => "Drill Pipe",
                ComponentType.HWDP => "HWDP",
                ComponentType.Casing => "Casing",
                ComponentType.Liner => "Liner",
                ComponentType.SettingTool => "Setting Tool",
                ComponentType.DC => "DC",
                ComponentType.LWD => "LWD",
                ComponentType.MWD => "MWD",
                ComponentType.PWO => "PWO",
                ComponentType.PWD => "PWD", // Kept for backward compatibility
                ComponentType.Motor => "Motor",
                ComponentType.XO => "XO",
                ComponentType.Jar => "JAR",
                ComponentType.Accelerator => "Accelerator",
                ComponentType.NearBit => "Near Bit",
                ComponentType.BitSub => "Bit Sub",
                ComponentType.Bit => "Bit", // Kept for backward compatibility
                _ => ComponentType.ToString()
            };
            set
            {
                ComponentType = value switch
                {
                    "Drill Pipe" => ComponentType.DrillPipe,
                    "HWDP" => ComponentType.HWDP,
                    "Casing" => ComponentType.Casing,
                    "Liner" => ComponentType.Liner,
                    "Setting Tool" => ComponentType.SettingTool,
                    "DC" => ComponentType.DC,
                    "LWD" => ComponentType.LWD,
                    "MWD" => ComponentType.MWD,
                    "PWO" => ComponentType.PWO,
                    "PWD" => ComponentType.PWD, // Kept for backward compatibility
                    "Motor" => ComponentType.Motor,
                    "XO" => ComponentType.XO,
                    "JAR" => ComponentType.Jar,
                    "Accelerator" => ComponentType.Accelerator,
                    "Near Bit" => ComponentType.NearBit,
                    "Bit Sub" => ComponentType.BitSub,
                    "Bit" => ComponentType.Bit, // Map to DrillBit for backward compatibility
                    _ => ComponentType.DrillPipe
                };
            }
        }

        public double? OD
        {
            get => _od;
            set 
            { 
                if (SetProperty(ref _od, value))
                {
                    OnPropertyChanged(nameof(DisplacementVolume));
                    ValidateOD();
                    ValidateID(); // Re-validate ID as it depends on OD
                }
            }
        }

        private void ValidateOD()
        {
            ClearErrors(nameof(OD));
            if (OD == null)
            {
                AddError(nameof(OD), "OD is required");
            }
            else if (OD <= 0)
            {
                AddError(nameof(OD), "OD must be > 0");
            }
            else if (ID != null && OD <= ID && ID > 0)
            {
                AddError(nameof(OD), "OD must be greater than ID");
            }
        }

        public double? ID
        {
            get => _id;
            set 
            { 
                if (SetProperty(ref _id, value))
                {
                    OnPropertyChanged(nameof(InternalVolume)); 
                    OnPropertyChanged(nameof(DisplacementVolume));
                    ValidateID();
                }
            }
        }

        private void ValidateID()
        {
            ClearErrors(nameof(ID));
            if (ID == null)
            {
                 // ID might not be strictly "Required" if it's solid?
                 // Usually standard validation requires it.
                 // For now, let's say "Required" to match user expectation of filling formatting.
                 // Or we suppress if we want 'clean' look.
                 // Actually the user wants "Blank" at start, but if they try to calculate, it should show error.
                 // So adding "Required" error is correct, BUT we might want to suppress it *initially* if we want purely white rows?
                 // No, standard is: validation shows immediately if invalid logic exists. A blank row is invalid state.
                 // However, Wellbore component suppresses validation on null to keep UI "clean" initially.
                 // Let's copy Wellbore pattern: if ID is null, suppress error?
                 // "If null, we suppress error to keep UI clean (ValidationService catches it on Save)."
                 // I will adopt that pattern.
            }
            else if (ID <= 0)
            {
                AddError(nameof(ID), "ID must be > 0");
            }
            else if (OD != null && ID >= OD && OD > 0)
            {
                AddError(nameof(ID), "ID must be < OD");
            }
        }

        public double? WeightPerFoot
        {
            get => _weightPerFoot;
            set
            {
                if (value < 0) throw new ArgumentException("Weight per foot cannot be negative");
                _weightPerFoot = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalWeight));
                OnPropertyChanged(nameof(BuoyantWeight));
            }
        }

        public double TotalWeight => (WeightPerFoot ?? 0) * (Length ?? 0);
        public double BuoyantWeight => TotalWeight * BuoyancyFactor;

        public double BuoyancyFactor
        {
            get => _buoyancyFactor;
            set { _buoyancyFactor = value; OnPropertyChanged(); OnPropertyChanged(nameof(BuoyantWeight)); }
        }

        public double? JointLength
        {
            get => _jointLength;
            set
            {
                _jointLength = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NumberOfJoints));
            }
        }

        public double? ToolJointLength
        {
            get => _toolJointLength;
            set { _toolJointLength = value; OnPropertyChanged(); }
        }

        public double? ToolJointOD
        {
            get => _toolJointOD;
            set { _toolJointOD = value; OnPropertyChanged(); }
        }

        public double? ToolJointId
        {
            get => _toolJointId;
            set { _toolJointId = value; OnPropertyChanged(); }
        }

        public double? FluidDensity
        {
            get => _fluidDensity;
            set { _fluidDensity = value; OnPropertyChanged(); }
        }

        public List<PressureDropPoint> PressureDropPoints
        {
            get => _pressureDropPoints ??= new List<PressureDropPoint>();
            set { _pressureDropPoints = value; OnPropertyChanged(); }
        }

        // Configuration flags used later in UI
        public bool IsTfaConfigured { get; set; }
        public bool IsPressureDropConfigured { get; set; }
        public bool IsToolJointConfigured { get; set; }

        public bool IsConfigured => ComponentType switch
        {
            ComponentType.DrillPipe => IsToolJointConfigured,
            ComponentType.HWDP => IsToolJointConfigured,
            ComponentType.MWD => IsPressureDropConfigured,
            ComponentType.Motor => IsPressureDropConfigured,
            ComponentType.Bit => IsTfaConfigured,
            _ => false
        };

        /// <summary>
        /// Gets whether this component is valid (has no validation errors)
        /// </summary>
        public bool IsValid => !HasErrors && !HasValidationError;

        // Sync with standard validation
        protected override void OnErrorsChanged(string propertyName)
        {
            base.OnErrorsChanged(propertyName);
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(HasValidationError));
            OnPropertyChanged(nameof(ValidationMessage));
        }

        public bool HasValidationError => HasErrors;

        public string ValidationMessage
        {
            get
            {
                var errors = GetErrors(null).Cast<string>();
                return errors.Any() ? string.Join(Environment.NewLine, errors) : string.Empty;
            }
        }

        // UI Helper property for highlighting
        private bool _isHighlighted;
        [Newtonsoft.Json.JsonIgnore]
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        public DrillStringComponent()
        {
            _pressureDropPoints = new List<PressureDropPoint>();
            _componentType = ComponentType.DrillPipe;
        }
    }
}
