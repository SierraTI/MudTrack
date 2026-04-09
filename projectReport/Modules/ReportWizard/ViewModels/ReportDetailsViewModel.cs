using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Services;

namespace ProjectReport.ViewModels
{
    public class ReportDetailsViewModel : BaseViewModel
    {
        private readonly Well _well;

        public ReportDetailsViewModel(Well well)
        {
            _well = well ?? throw new ArgumentNullException(nameof(well));

            // Draft report used by the XAML: Report.IntervalNumber, Report.MD, etc.
            Report = new ReportDraft
            {
                ReportDateTime = DateTime.Now,
                ReportNumber = (_well.Reports?.Count ?? 0) + 1
            };

            // Initialize Load Fluid from Well Data (The Bridge - Task 3)
            if (!string.IsNullOrWhiteSpace(_well.LoadFluid))
            {
                Report.PrimaryFluidSet = _well.LoadFluid;
            }

            /// <summary>
            /// Standardized Well Section options
            /// </summary>
            var validWellSections = new[] { "Conductor", "Surface", "Intermediate 1", "Intermediate 2", "Production", "Liner", "Open Hole", "Sidetrack", "Original" };

            // Inherit from last report if exists
            if (_well.LastReport != null)
            {
                var last = _well.LastReport;

                // Inherit Interval Number - continue from last report
                Report.IntervalNumber = last.IntervalNumber ?? string.Empty;
                Report.PresentActivity = last.PresentActivity ?? string.Empty;
                Report.PrimaryFluidSet = last.PrimaryFluidSet ?? string.Empty;
                Report.OtherActiveFluids = last.OtherActiveFluids ?? string.Empty;
                // Only inherit WellSection if it's in our standardized list
                Report.WellSection = (!string.IsNullOrWhiteSpace(last.WellSection) && Array.Exists(validWellSections, s => s == last.WellSection)) ? last.WellSection : string.Empty;
                Report.MaxBHT = last.MaxBHT;
                Report.OperationalIssues = last.OperationalIssues;
                
                Report.RigName = last.RigName ?? _well.RigName;
                Report.Contractor = last.Contractor ?? _well.Contractor;
                Report.RigType = last.RigType ?? _well.RigType;

                InheritedFields = true;
            }
            else
            {
                Report.RigName = _well.RigName;
                Report.Contractor = _well.Contractor;
                Report.RigType = _well.RigType;
                InheritedFields = false;
            }

            // Always initialize Pumps from Rig Profile if empty
            if (Report.Pumps.Count == 0 && _well.RigProfile?.Pumps != null)
            {
                foreach (var rigPump in _well.RigProfile.Pumps)
                {
                    var op = new ReportPumpOperation { No = rigPump.No };
                    op.UpdateFromRigPump(rigPump);
                    Report.Pumps.Add(op);
                }
            }

            ClearInheritedFieldCommand = new RelayCommand(_ => ClearInheritedFields());
            SetCurrentTimeCommand = new RelayCommand(_ => SetCurrentTime());
        }

        public Well ParentWell => _well;

        // This is what your XAML binds to: Report.IntervalNumber, Report.MD, etc.
        public ReportDraft Report { get; }

        public bool InheritedFields { get; private set; }

        /// <summary>
        /// Indicates if Load Fluid is not defined in the parent well
        /// </summary>
        public bool IsLoadFluidUndefined => string.IsNullOrWhiteSpace(_well.LoadFluid);

        // Matches your XAML: Command bindings
        public ICommand ClearInheritedFieldCommand { get; }
        public ICommand SetCurrentTimeCommand { get; }

        /// <summary>
        /// Standardized Present Activity options - grouped by category
        /// </summary>
        public ObservableCollection<string> PresentActivityOptions { get; } = new ObservableCollection<string>
        {
            // Drilling
            "Drilling",
            "Reaming",
            "Underreaming",
            "Directional Drilling",
            // Tripping
            "Tripping In",
            "Tripping Out",
            "Laying Down Pipe",
            "Picking up BHA",
            // Casing & Cement
            "Running Casing",
            "Cementing",
            "WOC (Waiting on Cement)",
            "Nipple Up BOP",
            // Evaluation
            "Wireline Logging",
            "MWD/LWD Survey",
            "Circulating for Samples",
            // Maintenance/Other
            "Rig Repairs",
            "Function Test BOP",
            "Safety Meeting",
            "WOW (Waiting on Weather)"
        };

        /// <summary>
        /// Standardized Well Section options
        /// </summary>
        public ObservableCollection<string> WellSectionOptions { get; } = new ObservableCollection<string>
        {
            "Conductor",
            "Surface",
            "Intermediate 1",
            "Intermediate 2",
            "Production",
            "Liner",
            "Open Hole",
            "Sidetrack",
            "Original"
        };

        private void ClearInheritedFields()
        {
            Report.PresentActivity = string.Empty;
            Report.PrimaryFluidSet = string.Empty;
            Report.OtherActiveFluids = string.Empty;
            Report.WellSection = string.Empty;

            InheritedFields = false;
            OnPropertyChanged(nameof(InheritedFields));
        }

        private void SetCurrentTime()
        {
            Report.ReportDateTime = DateTime.Now;
        }

        /// <summary>
        /// Converts the draft into your domain Report model (ProjectReport.Models.Report).
        /// Call this when you "Save/Next".
        /// </summary>
        public Report BuildReport()
        {
            return new Report
            {
                ReportNumber = Report.ReportNumber,
                IntervalNumber = Report.IntervalNumber,
                ReportDateTime = Report.ReportDateTime,
                MD = Report.MD,
                TVD = Report.TVD,
                WellSection = Report.WellSection,
                MaxBHT = Report.MaxBHT,
                MaxBHTSource = Report.MaxBHTSource,
                IntervalSizeIn = Report.IntervalSizeIn,
                PresentActivity = Report.PresentActivity,
                PrimaryFluidSet = Report.PrimaryFluidSet,
                OtherActiveFluids = Report.OtherActiveFluids,
                OperationalIssues = Report.OperationalIssues,
                CreatedDate = DateTime.Now,
                Pumps = new ObservableCollection<ReportPumpOperation>(Report.Pumps),
                RigName = Report.RigName,
                Contractor = Report.Contractor,
                RigType = Report.RigType
            };
        }
    }

    /// <summary>
    /// View-facing draft with validation (ValidatesOnDataErrors=True works with IDataErrorInfo).
    /// </summary>
    public class ReportDraft : BaseViewModel, IDataErrorInfo
    {
        private int _reportNumber;
        private string _intervalNumber = string.Empty;
        private DateTime _reportDateTime = DateTime.Now;
        private double? _md;
        private double? _tvd;
        private string _wellSection = string.Empty;
        private double? _maxBht;
        private string _presentActivity = string.Empty;
        private string _primaryFluidSet = string.Empty;
        private string _otherActiveFluids = string.Empty;
        private bool _operationalIssues;
        private string _rigName = string.Empty;
        private string _contractor = string.Empty;
        private string _rigType = string.Empty;
        private string _intervalSizeIn = string.Empty;

        private string _maxBhtSource = "MWD"; // MWD or PWD
        public string MaxBHTSource
        {
            get => _maxBhtSource;
            set { _maxBhtSource = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ReportPumpOperation> Pumps { get; } = new ObservableCollection<ReportPumpOperation>();

        public int ReportNumber
        {
            get => _reportNumber;
            set { _reportNumber = value; OnPropertyChanged(); }
        }

        public string IntervalNumber
        {
            get => _intervalNumber;
            set { _intervalNumber = value; OnPropertyChanged(); }
        }

        public DateTime ReportDateTime
        {
            get => _reportDateTime;
            set { _reportDateTime = value; OnPropertyChanged(); }
        }

        public double? MD
        {
            get => _md;
            set 
            { 
                if (SetProperty(ref _md, value))
                {
                    // Sincronizar Report MD con TotalWellboreMD en Geometry
                    // Al guardar o actualizar el "Report MD" en la cabecera del reporte,
                    // este valor debe pasar al totalWellboreMD del servicio de validación
                    if (value.HasValue && value.Value > 0)
                    {
                        WellContextService.Instance.UpdateSystemDepth(value.Value);
                    }
                }
            }
        }

        public double? TVD
        {
            get => _tvd;
            set 
            { 
                if (SetProperty(ref _tvd, value))
                {
                    // Notify Thermal Gradient module of TVD change
                    if (value.HasValue && value.Value > 0)
                    {
                        WellContextService.Instance.NotifyReportThermalDataUpdated(value, MaxBHT);
                    }
                }
            }
        }

        public string WellSection
        {
            get => _wellSection;
            set { _wellSection = value; OnPropertyChanged(); }
        }

        public double? MaxBHT
        {
            get => _maxBht;
            set 
            { 
                if (SetProperty(ref _maxBht, value))
                {
                    // Notify Thermal Gradient module of MaxBHT change
                    WellContextService.Instance.NotifyReportThermalDataUpdated(TVD, value);
                }
            }
        }

        public string PresentActivity
        {
            get => _presentActivity;
            set { _presentActivity = value; OnPropertyChanged(); }
        }

        public string PrimaryFluidSet
        {
            get => _primaryFluidSet;
            set { _primaryFluidSet = value; OnPropertyChanged(); }
        }

        public string OtherActiveFluids
        {
            get => _otherActiveFluids;
            set { _otherActiveFluids = value; OnPropertyChanged(); }
        }

        public bool OperationalIssues
        {
            get => _operationalIssues;
            set { _operationalIssues = value; OnPropertyChanged(); }
        }

        public string RigName
        {
            get => _rigName;
            set { _rigName = value; OnPropertyChanged(); }
        }

        public string Contractor
        {
            get => _contractor;
            set { _contractor = value; OnPropertyChanged(); }
        }

        public string RigType
        {
            get => _rigType;
            set { _rigType = value; OnPropertyChanged(); }
        }

        public string IntervalSizeIn
        {
            get => _intervalSizeIn;
            set { _intervalSizeIn = value; OnPropertyChanged(); }
        }

        // IDataErrorInfo
        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(IntervalNumber):
                        return string.IsNullOrWhiteSpace(IntervalNumber) ? "Interval # is required." : string.Empty;

                    case nameof(MD):
                        return (MD == null || MD <= 0) ? "Report MD must be a positive number." : string.Empty;

                    case nameof(TVD):
                        if (TVD == null || TVD <= 0) return "Report TVD must be a positive number.";
                        if (MD.HasValue && TVD.HasValue && TVD > MD) return "TVD cannot exceed MD.";
                        return string.Empty;

                    case nameof(IntervalSizeIn):

                        if (string.IsNullOrWhiteSpace(IntervalSizeIn))
                            return "You must select an Interval Size.";

                        return string.Empty;


                    case nameof(WellSection):
                        return string.IsNullOrWhiteSpace(WellSection) ? "Well Section is required." : string.Empty;

                    case nameof(ReportDateTime):
                        return (ReportDateTime == default) ? "Report Date/Time is required." : string.Empty;

                    default:
                        return string.Empty;
                }
            }
        }
    }
}
