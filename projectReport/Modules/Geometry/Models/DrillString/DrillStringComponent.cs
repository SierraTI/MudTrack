using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.BitAndJets;
using ProjectReport.Models.Geometry.FluidsAndPressure;
using ProjectReport.Models.Geometry.Wellbore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BitJetsConfigModel = ProjectReport.Models.Geometry.BitJetsConfig;
using PressureDropConfigModel = ProjectReport.Models.Geometry.PressureDropConfig;

namespace ProjectReport.Models.Geometry.DrillString
{
    public class DrillStringComponent : BaseModel
    {
        private const double CUBIC_FEET_TO_BBL = 0.178107607; // 1 cubic foot = 0.178107607 barrels

        private double? _topMD;
        private double? _bottomMD;
        private double? _length;
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
        public ProjectReport.Models.Geometry.BitAndJets.MultiBitJetsConfig? MultiBitJetsConfig { get; set; }

        // Configuration objects
        public ToolJointConfig? ToolJoint { get; set; }
        public PressureDropConfigModel? PressureDropConfig { get; set; }
        public BitJetsConfigModel? BitJetsConfig { get; set; }
        public ObservableCollection<WellboreComponent> WellboreComponents { get; set; }



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

        private double _volume;
        private ComponentType _componentType;
        private double? _od;
        private double? _id;

        public double? TopMD
        {
            get => _topMD;
            set
            {
                if (SetProperty(ref _topMD, value))
                {
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(InternalVolume));
                    OnPropertyChanged(nameof(DisplacementVolume));
                }
            }
        }

        public double? BottomMD
        {
            get => _bottomMD;
            set
            {
                if (SetProperty(ref _bottomMD, value))
                {
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(InternalVolume));
                    OnPropertyChanged(nameof(DisplacementVolume));
                }
            }
        }

        public double InternalVolume
        {
            get
            {
                // Internal Volume (bbl) = (ID² / 1029.4) × Length
                // SRS Rule: Use WeightedAverageID if Tool Joint configured
                double effectiveID = WeightedAverageID;
                if (effectiveID <= 0 || Length.GetValueOrDefault() <= 0)
                    return 0;

                return (effectiveID * effectiveID / 1029.4) * Length.GetValueOrDefault();
            }
        }

        // SRS Formula: Displacement Volume (bbl) = (OD² - ID²) / 1029.4 × Length
        public double DisplacementVolume
        {
            get
            {
                // SRS Formula: Displacement Volume (bbl) = (OD² - ID²) / 1029.4 × Length
                // SRS Rule: Use WeightedAverageOD and WeightedAverageID if Tool Joint configured
                double effectiveOD = WeightedAverageOD;
                double effectiveID = WeightedAverageID;

                if (effectiveOD <= 0 || Length.GetValueOrDefault() <= 0)
                    return 0;
                
                return ((effectiveOD * effectiveOD) - (effectiveID * effectiveID)) / 1029.4 * Length.GetValueOrDefault();
            }
        }

        public double? Length
        {
            get => BottomMD.HasValue && TopMD.HasValue ? BottomMD.Value - TopMD.Value : _length;
            set 
            { 
                // CRITICAL: Block negative lengths at source
                if (value.HasValue && value.Value < 0)
                {
                    // Do NOT allow negative lengths to be set
                    // This prevents cascading calculation errors
                    value = 0;
                }
                
                if (SetProperty(ref _length, value))
                {
                    if (value.HasValue && TopMD.HasValue)
                    {
                        BottomMD = TopMD.Value + value.Value;
                    }
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
                 AddError(nameof(Length), "Length is required");
            }
            else if (Length < 0)
            {
                // This should never happen now due to setter blocking, but keep as safety
                AddError(nameof(Length), "Length cannot be negative");
            }
            else if (Length <= 0)
            {
                AddError(nameof(Length), "Length must be > 0");
            }
        }

        public int NumberOfJoints => (JointLength.GetValueOrDefault() > 0 && Length.GetValueOrDefault() > 0) 
            ? (int)Math.Ceiling(Length.GetValueOrDefault() / JointLength.GetValueOrDefault()) 
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
                    ValidateODDrill();  // Se valida inmediatamente al escribir
                    OnPropertyChanged(nameof(DisplacementVolume));
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
                    ValidateODDrill();  // Validamos OD vs ID también
                    OnPropertyChanged(nameof(InternalVolume));
                    OnPropertyChanged(nameof(DisplacementVolume));
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

        /// <summary>
        /// Weighted average OD considering tool joints.
        /// Formula: (PipeLength × PipeOD + JointLength × JointOD) / TotalLength
        /// </summary>
        public double WeightedAverageOD
        {
            get
            {
                if (!IsToolJointConfigured || ToolJoint == null || !ToolJoint.TJ_OD.HasValue)
                    return OD ?? 0;

                double totalLength = Length ?? 0;
                if (totalLength == 0) return OD ?? 0;

                double jointLength = NumberOfJoints * (ToolJoint.TJ_Length ?? 0);
                double pipeLength = totalLength - jointLength;

                return ((pipeLength * (OD ?? 0)) + (jointLength * ToolJoint.TJ_OD.Value)) / totalLength;
            }
        }

        /// <summary>
        /// Weighted average ID considering tool joints.
        /// Formula: (PipeLength × PipeID + JointLength × JointID) / TotalLength
        /// </summary>
        public double WeightedAverageID
        {
            get
            {
                if (!IsToolJointConfigured || ToolJoint == null || !ToolJoint.TJ_ID.HasValue)
                    return ID ?? 0;

                double totalLength = Length ?? 0;
                if (totalLength == 0) return ID ?? 0;

                double jointLength = NumberOfJoints * (ToolJoint.TJ_Length ?? 0);
                double pipeLength = totalLength - jointLength;

                return ((pipeLength * (ID ?? 0)) + (jointLength * ToolJoint.TJ_ID.Value)) / totalLength;
            }
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
        public override bool IsValid => !HasErrors && !HasValidationError;

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

        public bool IsConfigurable => ComponentType switch
        {
            ComponentType.DrillPipe => true,
            ComponentType.HWDP => true,
            ComponentType.MWD => true,
            ComponentType.Motor => true,
            ComponentType.Bit => true,
            _ => false
        };

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

        public void ValidateODDrill()
        {
            ClearErrors(nameof(OD));
            ClearErrors(nameof(ID));

            if (OD == null || OD <= 0)
            {
                AddError(nameof(OD), "OD is required and must be > 0");
            }

            if (ID != null && ID <= 0)
            {
                AddError(nameof(ID), "ID must be > 0");
            }

            if (OD != null && ID != null && ID >= OD)
            {
                AddError(nameof(OD), "OD must be greater than ID");
                AddError(nameof(ID), "ID must be smaller than OD");
            }

            // Validar OD del DrillString contra secciones de Wellbore
            if (WellboreComponents != null && WellboreComponents.Count > 0 && OD.HasValue)
            {
                var activeSection = WellboreComponents
                    .Where(c => c.OD.HasValue && c.OD.Value > 0)
                    .OrderByDescending(c => c.BottomMD ?? 0)
                    .FirstOrDefault();

                if (activeSection != null)
                {
                    if (activeSection.SectionType == ComponentType.OpenHole)
                    {
                        double holeOD = activeSection.OD ?? 0;
                        if (OD.Value >= holeOD && holeOD > 0)
                            AddError(nameof(OD), $"Drill OD ({OD.Value}) must be smaller than Open Hole ({holeOD})");
                    }
                    else if (activeSection.SectionType == ComponentType.Casing || activeSection.SectionType == ComponentType.Liner)
                    {
                        double casingID = activeSection.ID ?? 0;
                        if (OD.Value >= casingID && casingID > 0)
                            AddError(nameof(OD), $"Drill OD ({OD.Value}) must be smaller than ID ({casingID}) of {activeSection.SectionType}");
                    }
                }
            }
        }

    }
}
