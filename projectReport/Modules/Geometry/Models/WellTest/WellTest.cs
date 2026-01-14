using System;
using System.Collections.ObjectModel;
using ProjectReport.Models;

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
        private WellTestType _type;
        private double _md;
        private double _tvd;
        private double _testPressurePsi;
        private double _emw;

        public WellTestType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    OnPropertyChanged(nameof(TypeString));
                    RecalculateEMW();
                }
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
            set => SetProperty(ref _md, value);
        }

        public double TVD
        {
            get => _tvd;
            set
            {
                if (SetProperty(ref _tvd, value))
                {
                    RecalculateEMW();
                }
            }
        }

        public double TestPressurePsi
        {
            get => _testPressurePsi;
            set
            {
                if (SetProperty(ref _testPressurePsi, value))
                {
                    RecalculateEMW();
                }
            }
        }

        /// <summary>
        /// Equivalent Mud Weight (ppg)
        /// Formula: EMW = (Pressure / (0.052 * TVD))
        /// </summary>
        public double TestValue
        {
            get => _emw;
            private set => SetProperty(ref _emw, value);
        }

        private void RecalculateEMW()
        {
            if (TVD > 0)
            {
                TestValue = TestPressurePsi / (0.052 * TVD);
            }
            else
            {
                TestValue = 0;
            }
        }
    }
}
