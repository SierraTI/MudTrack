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
        private int _id;
        private string? _section;
        private WellTestType _type;
        private double _testValue;
        
        // Legacy fields for backward compatibility
        private double _md;
        private double _tvd;
        private double _testPressurePsi;

        /// <summary>
        /// Display ID for ordering
        /// </summary>
        public new int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Wellbore section name this test is associated with
        /// </summary>
        public string? Section
        {
            get => _section;
            set => SetProperty(ref _section, value);
        }

        public WellTestType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    OnPropertyChanged(nameof(TypeString));
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

        /// <summary>
        /// Test value in ppg (pounds per gallon)
        /// </summary>
        public double TestValue
        {
            get => _testValue;
            set => SetProperty(ref _testValue, value);
        }

        // Legacy properties for backward compatibility
        public double MD
        {
            get => _md;
            set => SetProperty(ref _md, value);
        }

        public double TVD
        {
            get => _tvd;
            set => SetProperty(ref _tvd, value);
        }

        public double TestPressurePsi
        {
            get => _testPressurePsi;
            set => SetProperty(ref _testPressurePsi, value);
        }
    }
}
