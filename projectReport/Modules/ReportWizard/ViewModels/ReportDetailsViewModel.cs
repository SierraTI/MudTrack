using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using ProjectReport.Models;

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
                ReportDateTime = DateTime.Now
            };

            // Inherit from last report if exists
            if (_well.LastReport != null)
            {
                var last = _well.LastReport;

                Report.PresentActivity = last.PresentActivity ?? string.Empty;
                Report.PrimaryFluidSet = last.PrimaryFluidSet ?? string.Empty;
                Report.OtherActiveFluids = last.OtherActiveFluids ?? string.Empty;
                Report.WellSection = last.WellSection ?? string.Empty;
                Report.MaxBHT = last.MaxBHT;
                Report.OperationalIssues = last.OperationalIssues;

                InheritedFields = true;
            }
            else
            {
                InheritedFields = false;
            }

            ClearInheritedFieldCommand = new RelayCommand(_ => ClearInheritedFields());
        }

        public Well ParentWell => _well;

        // This is what your XAML binds to: Report.IntervalNumber, Report.MD, etc.
        public ReportDraft Report { get; }

        public bool InheritedFields { get; private set; }

        // Matches your XAML: Command="{Binding ClearInheritedFieldCommand}"
        public ICommand ClearInheritedFieldCommand { get; }

        private void ClearInheritedFields()
        {
            Report.PresentActivity = string.Empty;
            Report.PrimaryFluidSet = string.Empty;
            Report.OtherActiveFluids = string.Empty;
            Report.WellSection = string.Empty;

            InheritedFields = false;
            OnPropertyChanged(nameof(InheritedFields));
        }

        /// <summary>
        /// Converts the draft into your domain Report model (ProjectReport.Models.Report).
        /// Call this when you "Save/Next".
        /// </summary>
        public Report BuildReport()
        {
            return new Report
            {
                IntervalNumber = Report.IntervalNumber,
                ReportDateTime = Report.ReportDateTime,
                MD = Report.MD,
                TVD = Report.TVD,
                WellSection = Report.WellSection,
                MaxBHT = Report.MaxBHT,
                PresentActivity = Report.PresentActivity,
                PrimaryFluidSet = Report.PrimaryFluidSet,
                OtherActiveFluids = Report.OtherActiveFluids,
                OperationalIssues = Report.OperationalIssues,
                CreatedDate = DateTime.Now
            };
        }
    }

    /// <summary>
    /// View-facing draft with validation (ValidatesOnDataErrors=True works with IDataErrorInfo).
    /// </summary>
    public class ReportDraft : BaseViewModel, IDataErrorInfo
    {
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
            set { _md = value; OnPropertyChanged(); }
        }

        public double? TVD
        {
            get => _tvd;
            set { _tvd = value; OnPropertyChanged(); }
        }

        public string WellSection
        {
            get => _wellSection;
            set { _wellSection = value; OnPropertyChanged(); }
        }

        public double? MaxBHT
        {
            get => _maxBht;
            set { _maxBht = value; OnPropertyChanged(); }
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
