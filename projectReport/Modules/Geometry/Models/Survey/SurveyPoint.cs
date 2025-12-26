using ProjectReport.Models;
using System.Linq;

namespace ProjectReport.Models.Geometry.Survey
{
    public class SurveyPoint : BaseModel
    {
        private string _section = string.Empty;
        private double _md;
        private double _tvd;
        private double _holeAngle;
        private double _azimuth;
        private double _doglegSeverity;
        private double _buildRate;
        private double _turnRate;
        private double _northing;
        private double _easting;
        private double _verticalSection;

        public string Section
        {
            get => _section;
            set => SetProperty(ref _section, value);
        }

        public double MD
        {
            get => _md;
            set
            {
                if (SetProperty(ref _md, value))
                {
                    ValidateMD();
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
                    ValidateTVD();
                }
            }
        }

        public double HoleAngle
        {
            get => _holeAngle;
            set
            {
                if (SetProperty(ref _holeAngle, value))
                {
                    ValidateHoleAngle();
                }
            }
        }

        public double Azimuth
        {
            get => _azimuth;
            set
            {
                if (SetProperty(ref _azimuth, value))
                {
                    ValidateAzimuth();
                }
            }
        }

        public double DoglegSeverity
        {
            get => _doglegSeverity;
            set
            {
                SetProperty(ref _doglegSeverity, value);
                OnPropertyChanged(nameof(IsHighDogleg));
            }
        }

        public double BuildRate
        {
            get => _buildRate;
            set => SetProperty(ref _buildRate, value);
        }

        public double TurnRate
        {
            get => _turnRate;
            set => SetProperty(ref _turnRate, value);
        }

        public bool IsHighDogleg => DoglegSeverity > 3.0;

        public double Northing
        {
            get => _northing;
            set
            {
                SetProperty(ref _northing, value);
                OnPropertyChanged(nameof(VerticalSection));
            }
        }

        public double Easting
        {
            get => _easting;
            set
            {
                SetProperty(ref _easting, value);
                OnPropertyChanged(nameof(VerticalSection));
            }
        }

        public double VerticalSection
        {
            get => _verticalSection;
            set => SetProperty(ref _verticalSection, value);
        }

        #region Validation

        private void ValidateMD()
        {
            ClearErrors(nameof(MD));
            if (MD < TVD)
            {
                AddError(nameof(MD), $"Error S2: TVD ({TVD:F2} ft) exceeds MD ({MD:F2} ft). This is physically impossible.");
            }
        }

        private void ValidateTVD()
        {
            ClearErrors(nameof(TVD));
            if (TVD > MD)
            {
                AddError(nameof(TVD), $"Error S2: TVD ({TVD:F2} ft) exceeds MD ({MD:F2} ft). This is physically impossible.");
            }
            // Trigger MD validation as well since they are related
            ValidateMD();
        }

        private void ValidateHoleAngle()
        {
            ClearErrors(nameof(HoleAngle));
            if (HoleAngle > 93 || HoleAngle < 0)
            {
                AddError(nameof(HoleAngle), $"Error S3: Hole Angle ({HoleAngle:F2}°) is outside valid measurement range (0° - 93°).");
            }
        }

        private void ValidateAzimuth()
        {
            ClearErrors(nameof(Azimuth));
            if (Azimuth > 360 || Azimuth < 0)
            {
                AddError(nameof(Azimuth), $"Error S3: Azimuth ({Azimuth:F2}°) is outside valid measurement range (0° - 360°).");
            }
        }

        /// <summary>
        /// Validates depth progression against previous survey point
        /// Rule S1: MD[i] >= MD[i-1] AND TVD[i] >= TVD[i-1]
        /// The well cannot go backwards in depth
        /// </summary>
        public void ValidateDepthProgression(SurveyPoint? previousPoint)
        {
            if (previousPoint == null) return; // First point, no validation needed
            
            // S1: MD progression
            if (MD < previousPoint.MD)
            {
                AddError(nameof(MD), $"Error S1: MD ({MD:F2} ft) cannot be less than previous survey point ({previousPoint.MD:F2} ft). The well cannot go backwards in depth.");
            }
            
            // S1: TVD progression
            if (TVD < previousPoint.TVD)
            {
                AddError(nameof(TVD), $"Error S1: TVD ({TVD:F2} ft) cannot be less than previous survey point ({previousPoint.TVD:F2} ft). The well cannot go backwards in depth.");
            }
        }

        /// <summary>
        /// Gets the first validation error message for display in UI
        /// </summary>
        public string ValidationMessage
        {
            get
            {
                var errors = GetErrors(null);
                if (errors != null)
                {
                    var errorList = errors.Cast<string>().ToList();
                    return errorList.Count > 0 ? errorList[0] : string.Empty;
                }
                return string.Empty;
            }
        }

        #endregion

        public bool IsValid => !HasErrors;
    }
}
