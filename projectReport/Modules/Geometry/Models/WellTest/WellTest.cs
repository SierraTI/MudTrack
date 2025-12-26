using ProjectReport.Models;
using System;

namespace ProjectReport.Models.Geometry.WellTest
{
    public enum WellTestType
    {
        LeakOff,
        FractureGradient,
        PorePressure,
        FormationIntegrity
    }

    public enum PressureUnit
    {
        PPG,
        PSI_FT,
        KPA_M,
        MPA_M
    }

    public class WellTest : BaseModel
    {
        // Conversion constants:
        public const double PPG_TO_PSI_FT = 0.052;
        public const double PSI_FT_TO_PPG = 1 / 0.052;
        public const double PPG_TO_KPA_M = 9.81;
        public const double KPA_M_TO_PSI_FT = 0.00694;

        private string _section = string.Empty;
        private WellTestType _type;
        private double _md;
        private double _tvd;
        private double _testValue;
        private string _description = string.Empty;
        private double _formationPressureGradient;
        private PressureUnit _pressureUnit = PressureUnit.PPG;

        public string Section
        {
            get => _section;
            set => SetProperty(ref _section, value);
        }

        private string _componentType = "Liner";
        public string ComponentType
        {
            get => _componentType;
            set => SetProperty(ref _componentType, value);
        }

        public WellTestType Type
        {
            get => _type;
            set
            {
                SetProperty(ref _type, value);
                OnPropertyChanged(nameof(TypeString));
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string TypeString
        {
            get => Type switch
            {
                WellTestType.LeakOff => "Leak Off",
                WellTestType.FractureGradient => "Fracture gradient",
                WellTestType.PorePressure => "Pore pressure",
                WellTestType.FormationIntegrity => "Integrity",
                _ => Type.ToString()
            };
            set
            {
                Type = value switch
                {
                    "Leak Off" => WellTestType.LeakOff,
                    "Fracture gradient" => WellTestType.FractureGradient,
                    "Pore pressure" => WellTestType.PorePressure,
                    "Integrity" => WellTestType.FormationIntegrity,
                    _ => WellTestType.LeakOff
                };
            }
        }

        public double MD
        {
            get => _md;
            set
            {
                if (SetProperty(ref _md, value))
                {
                    ValidateDepthRelation();
                }
            }
        }

        public double TVD
        {
            get => _tvd;
            set
            {
                if (SetProperty(ref _tvd, value))
                {
                    ValidateDepthRelation();
                }
            }
        }

        private void ValidateDepthRelation()
        {
            if (_md > 0 && _tvd > 0 && _tvd > _md)
                throw new InvalidOperationException("TVD cannot be greater than MD.");
        }

        public double TestValue
        {
            get => _testValue;
            set
            {
                if (SetProperty(ref _testValue, value))
                {
                    ValidateTestValue();
                }
            }
        }

        private void ValidateTestValue()
        {
            ClearErrors(nameof(TestValue));
            
            // BR-WT-003: Test Value must be positive and within range 0-25,000
            if (TestValue < 0)
            {
                AddError(nameof(TestValue), "Test Value cannot be negative");
            }
            else if (TestValue > 25000)
            {
                AddError(nameof(TestValue), "Test Value cannot exceed 25,000 ppb");
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public double FormationPressureGradient
        {
            get => _formationPressureGradient;
            set
            {
                if (SetProperty(ref _formationPressureGradient, value))
                {
                    OnPropertyChanged(nameof(FormationPressureInPPG));
                    OnPropertyChanged(nameof(FormationPressureInPSI_FT));
                }
            }
        }

        public PressureUnit PressureUnit
        {
            get => _pressureUnit;
            set
            {
                if (SetProperty(ref _pressureUnit, value))
                {
                    OnPropertyChanged(nameof(FormationPressureInPPG));
                    OnPropertyChanged(nameof(FormationPressureInPSI_FT));
                }
            }
        }

        public double FormationPressureInPPG =>
            PressureUnit switch
            {
                PressureUnit.PPG => FormationPressureGradient,
                PressureUnit.PSI_FT => FormationPressureGradient / PPG_TO_PSI_FT,
                PressureUnit.KPA_M => FormationPressureGradient / PPG_TO_KPA_M,
                PressureUnit.MPA_M => (FormationPressureGradient * 1000) / PPG_TO_KPA_M,
                _ => FormationPressureGradient
            };

        public double FormationPressureInPSI_FT =>
            PressureUnit switch
            {
                PressureUnit.PPG => FormationPressureGradient * PPG_TO_PSI_FT,
                PressureUnit.PSI_FT => FormationPressureGradient,
                PressureUnit.KPA_M => FormationPressureGradient * KPA_M_TO_PSI_FT,
                PressureUnit.MPA_M => (FormationPressureGradient * 1000) * KPA_M_TO_PSI_FT,
                _ => FormationPressureGradient
            };

        public static double ConvertPressure(double value, PressureUnit from, PressureUnit to)
        {
            if (from == to) return value;

            // Convert to PPG base
            double ppg = from switch
            {
                PressureUnit.PPG => value,
                PressureUnit.PSI_FT => value / PPG_TO_PSI_FT,
                PressureUnit.KPA_M => value / PPG_TO_KPA_M,
                PressureUnit.MPA_M => (value * 1000) / PPG_TO_KPA_M,
                _ => value
            };

            // Convert from PPG to target
            return to switch
            {
                PressureUnit.PPG => ppg,
                PressureUnit.PSI_FT => ppg * PPG_TO_PSI_FT,
                PressureUnit.KPA_M => ppg * PPG_TO_KPA_M,
                PressureUnit.MPA_M => (ppg * PPG_TO_KPA_M) / 1000,
                _ => ppg
            };
        }
    }
}
