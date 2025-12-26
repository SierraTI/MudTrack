using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Models.Geometry.ThermalGradient;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ProjectReport.Models
{
    /// <summary>
    /// Represents a complete well with all metadata and geometry data
    /// </summary>
    public class Well : BaseModel
    {
        #region Private Fields

        // Unique identifier
        private int _id;

        // General Information
        private string _wellName = string.Empty;
        private string _operator = string.Empty;
        private string _operatorLogoPath = string.Empty;
        private string _loadFluid = string.Empty;
        private DateTime? _spudDate;
    
        // Design & Classification
        private string _trajectory = string.Empty;
        private string _wellType = string.Empty;
        private string _rigName = string.Empty;
        private string _rigType = string.Empty;

        // Location Details
        private string _location = string.Empty; // Onshore/Offshore
        private string _country = string.Empty;
        private string _basin= string.Empty;
        private string _state = string.Empty;
        private string _field = string.Empty;
        private string _municipality = string.Empty;
        

        // Operational Information
        private double? _totalMD;
        private double? _totalTVD;
        private string _currentOperation = string.Empty;
        private int? _drillingDays;
        private string _targetFormation = string.Empty;
        private DateTime? _expectedCompletionDate;

        // Additional Details
        private string _projectCode = string.Empty;
        private string _costCenter = string.Empty;
        private string _afeNumber = string.Empty;
        private string _wellObjectives = string.Empty;
        private string _specialRequirements = string.Empty;
        private string _notes = string.Empty;

        // Status & Tracking
        private WellStatus _status = WellStatus.Draft;
        private DateTime _createdDate = DateTime.Now;
        private DateTime _lastModified = DateTime.Now;
        private string _lastModifiedBy = string.Empty;

        #endregion

        #region General Information Properties

        public new int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string WellName
        {
            get => _wellName;
            set
            {
                if (SetProperty(ref _wellName, value))
                    UpdateLastModified();
            }
        }

        public string Operator
        {
            get => _operator;
            set
            {
                if (SetProperty(ref _operator, value))
                    UpdateLastModified();
            }
        }

        public string OperatorLogoPath
        {
            get => _operatorLogoPath;
            set
            {
                if (SetProperty(ref _operatorLogoPath, value))
                    UpdateLastModified();
            }
        }

        public string LoadFluid
        {
            get => _loadFluid;
            set
            {
                if (SetProperty(ref _loadFluid, value))
                    UpdateLastModified();
            }
        }

        public DateTime? SpudDate
        {
            get => _spudDate;
            set
            {
                if (SetProperty(ref _spudDate, value))
                {
                    UpdateLastModified();
                    OnPropertyChanged(nameof(DrillingDays));
                }
            }
        }

       
        #endregion

        #region Design & Classification Properties

        public string Trajectory
        {
            get => _trajectory;
            set
            {
                if (SetProperty(ref _trajectory, value))
                    UpdateLastModified();
            }
        }

        public string WellType
        {
            get => _wellType;
            set
            {
                if (SetProperty(ref _wellType, value))
                    UpdateLastModified();
            }
        }

        public string RigName
        {
            get => _rigName;
            set
            {
                if (SetProperty(ref _rigName, value))
                    UpdateLastModified();
            }
        }

        public string RigType
        {
            get => _rigType;
            set
            {
                if (SetProperty(ref _rigType, value))
                    UpdateLastModified();
            }
        }

        #endregion

        #region Location Details Properties

        public string Location
        {
            get => _location;
            set
            {
                if (SetProperty(ref _location, value))
                    UpdateLastModified();
            }
        }

        public string Country
        {
            get => _country;
            set
            {
                if (SetProperty(ref _country, value))
                    UpdateLastModified();
            }
        }

        public string Basin
        {
            get => _basin;
            set
            {
                if (SetProperty(ref _basin, value))
                    UpdateLastModified();
            }
        }
        public string State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                    UpdateLastModified();
            }
        }

        public string Municipality
        {
            get => _municipality;
            set
            {
                if (SetProperty(ref _municipality, value))
                    UpdateLastModified();
            }
        }
        public string Field
        {
            get => _field;
            set
            {
                if (SetProperty(ref _field, value))
                    UpdateLastModified();
            }
        }

        #endregion

        #region Operational Information Properties

        public double? TotalMD
        {
            get => _totalMD;
            set
            {
                if (SetProperty(ref _totalMD, value))
                    UpdateLastModified();
            }
        }

        public double? TotalTVD
        {
            get => _totalTVD;
            set
            {
                if (SetProperty(ref _totalTVD, value))
                    UpdateLastModified();
            }
        }

        public string CurrentOperation
        {
            get => _currentOperation;
            set
            {
                if (SetProperty(ref _currentOperation, value))
                    UpdateLastModified();
            }
        }

        public int? DrillingDays
        {
            get
            {
                if (_drillingDays.HasValue)
                    return _drillingDays;

                // Auto-calculate from spud date if available
                if (SpudDate.HasValue)
                {
                    return (int)(DateTime.Now - SpudDate.Value).TotalDays;
                }

                return null;
            }
            set
            {
                if (SetProperty(ref _drillingDays, value))
                    UpdateLastModified();
            }
        }

        public string TargetFormation
        {
            get => _targetFormation;
            set
            {
                if (SetProperty(ref _targetFormation, value))
                    UpdateLastModified();
            }
        }

        public DateTime? ExpectedCompletionDate
        {
            get => _expectedCompletionDate;
            set
            {
                if (SetProperty(ref _expectedCompletionDate, value))
                    UpdateLastModified();
            }
        }

        #endregion

        #region Additional Details Properties

        public string ProjectCode
        {
            get => _projectCode;
            set
            {
                if (SetProperty(ref _projectCode, value))
                    UpdateLastModified();
            }
        }

        public string CostCenter
        {
            get => _costCenter;
            set
            {
                if (SetProperty(ref _costCenter, value))
                    UpdateLastModified();
            }
        }

        public string AFENumber
        {
            get => _afeNumber;
            set
            {
                if (SetProperty(ref _afeNumber, value))
                    UpdateLastModified();
            }
        }

        public string WellObjectives
        {
            get => _wellObjectives;
            set
            {
                if (SetProperty(ref _wellObjectives, value))
                    UpdateLastModified();
            }
        }

        public string SpecialRequirements
        {
            get => _specialRequirements;
            set
            {
                if (SetProperty(ref _specialRequirements, value))
                    UpdateLastModified();
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (SetProperty(ref _notes, value))
                    UpdateLastModified();
            }
        }

        public ObservableCollection<string> CustomTags { get; set; } = new ObservableCollection<string>();

        #endregion

        #region Status & Tracking Properties

        public WellStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                    UpdateLastModified();
            }
        }

        public new DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set => SetProperty(ref _lastModified, value);
        }

        public string LastModifiedBy
        {
            get => _lastModifiedBy;
            set => SetProperty(ref _lastModifiedBy, value);
        }

        #endregion

        #region Geometry Data Collections

        public ObservableCollection<WellboreComponent> WellboreComponents { get; set; } = new ObservableCollection<WellboreComponent>();
        public ObservableCollection<DrillStringComponent> DrillStringComponents { get; set; } = new ObservableCollection<DrillStringComponent>();
        public ObservableCollection<SurveyPoint> SurveyPoints { get; set; } = new ObservableCollection<SurveyPoint>();
        public ObservableCollection<WellTest> WellTests { get; set; } = new ObservableCollection<WellTest>();
        public ObservableCollection<ThermalGradientPoint> ThermalGradientPoints { get; set; } = new ObservableCollection<ThermalGradientPoint>();
        public ObservableCollection<Report> Reports { get; set; } = new ObservableCollection<Report>();

        #endregion

        /// <summary>
        /// Convenience property to get the most recent report or null
        /// </summary>
        public Report? LastReport => Reports.Count > 0 ? Reports[^1] : null;

        #region Validation Properties

        /// <summary>
        /// Checks if all required fields are filled
        /// </summary>
        public bool IsRequiredFieldsComplete =>
            !string.IsNullOrWhiteSpace(WellName) &&
            !string.IsNullOrWhiteSpace(Operator) &&
            !string.IsNullOrWhiteSpace(LoadFluid) &&
            SpudDate.HasValue &&
            !string.IsNullOrWhiteSpace(Trajectory) &&
            !string.IsNullOrWhiteSpace(WellType) &&
            !string.IsNullOrWhiteSpace(RigType) &&
            !string.IsNullOrWhiteSpace(RigName) &&
            !string.IsNullOrWhiteSpace(Location) &&
            !string.IsNullOrWhiteSpace(Country) &&
            !string.IsNullOrWhiteSpace(State) &&
            !string.IsNullOrWhiteSpace(Field);

        /// <summary>
        /// Gets a list of missing required fields
        /// </summary>
        public List<string> GetMissingRequiredFields()
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(WellName)) missing.Add("Well Name");
            if (string.IsNullOrWhiteSpace(Operator)) missing.Add("Operator");
            if (string.IsNullOrWhiteSpace(LoadFluid)) missing.Add("Load Fluid");
            if (!SpudDate.HasValue) missing.Add("Spud Date");
            if (string.IsNullOrWhiteSpace(Trajectory)) missing.Add("Trajectory");
            if (string.IsNullOrWhiteSpace(WellType)) missing.Add("Well Type");
            if (string.IsNullOrWhiteSpace(RigType)) missing.Add("Rig Type");
            if (string.IsNullOrWhiteSpace(RigName)) missing.Add("Rig Name");
            if (string.IsNullOrWhiteSpace(Location)) missing.Add("Location");
            if (string.IsNullOrWhiteSpace(Country)) missing.Add("Country");
            if (string.IsNullOrWhiteSpace(Basin)) missing.Add("Basin");
            if (string.IsNullOrWhiteSpace(State)) missing.Add("State");
            if (string.IsNullOrWhiteSpace(Municipality)) missing.Add("Municipality");
            if (string.IsNullOrWhiteSpace(Field)) missing.Add("Field");

            return missing;
        }

        #endregion

        #region Helper Methods

        private void UpdateLastModified()
        {
            LastModified = DateTime.Now;
        }

        /// <summary>
        /// Creates a duplicate of this well with "-Copy" suffix
        /// </summary>
        public Well Duplicate()
        {
            var copy = new Well
            {
                // Copy general information
                WellName = $"{WellName}-Copy",
                Operator = Operator,
                OperatorLogoPath = OperatorLogoPath,
                LoadFluid = LoadFluid,
                SpudDate = DateTime.Now, // Reset to today
              

                // Copy design & classification
                Trajectory = Trajectory,
                WellType = WellType,
                RigName = RigName,
                RigType = RigType,

                // Copy location details
                Location = Location,
                Country = Country,
                State = State,
                Field = Field,
                Basin = Basin,
                Municipality = Municipality,

                // Reset operational information
                TotalMD = null,
                TotalTVD = null,
                CurrentOperation = string.Empty,
                DrillingDays = 0,
                TargetFormation = TargetFormation,
                ExpectedCompletionDate = null,

                // Copy additional details
                ProjectCode = ProjectCode,
                CostCenter = CostCenter,
                AFENumber = string.Empty, // Don't copy AFE number
                WellObjectives = WellObjectives,
                SpecialRequirements = SpecialRequirements,
                Notes = Notes,

                // Reset status
                Status = WellStatus.Draft,
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now
            };

            // Copy custom tags
            foreach (var tag in CustomTags)
            {
                copy.CustomTags.Add(tag);
            }

            return copy;
        }

        #endregion
    }
}
