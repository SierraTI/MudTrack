using System;
using System.Collections.ObjectModel;
using ProjectReport.Models.Rig;

namespace ProjectReport.Models
{
    public class Report : BaseModel, System.ComponentModel.IDataErrorInfo
    {
        // Sequential report number (displayed as Report #)
        private int _reportNumber;
        public int ReportNumber
        {
            get => _reportNumber;
            set => SetProperty(ref _reportNumber, value);
        }

        private string _intervalNumber = string.Empty;
        public string IntervalNumber
        {
            get => _intervalNumber;
            set => SetProperty(ref _intervalNumber, value);
        }

        private string _rigName = string.Empty;
        public string RigName
        {
            get => _rigName;
            set => SetProperty(ref _rigName, value);
        }

        private string _contractor = string.Empty;
        public string Contractor
        {
            get => _contractor;
            set => SetProperty(ref _contractor, value);
        }

        private string _rigType = string.Empty;
        public string RigType
        {
            get => _rigType;
            set => SetProperty(ref _rigType, value);
        }

        private DateTime _reportDateTime = DateTime.Now;
        public DateTime ReportDateTime
        {
            get => _reportDateTime;
            set => SetProperty(ref _reportDateTime, value);
        }

        private double? _md;
        public double? MD
        {
            get => _md;
            set => SetProperty(ref _md, value);
        }

        private double? _tvd;
        public double? TVD
        {
            get => _tvd;
            set => SetProperty(ref _tvd, value);
        }

        private string _activity = string.Empty;
        public string Activity
        {
            get => _activity;
            set => SetProperty(ref _activity, value);
        }

        private string _wellSection = string.Empty;
        public string WellSection
        {
            get => _wellSection;
            set => SetProperty(ref _wellSection, value);
        }

        private double? _maxBht;
        public double? MaxBHT
        {
            get => _maxBht;
            set => SetProperty(ref _maxBht, value);
        }

        private string _maxBhtSource = "MWD"; // MWD or PWD
        public string MaxBHTSource
        {
            get => _maxBhtSource;
            set => SetProperty(ref _maxBhtSource, value);
        }

        private double? _intervalSizeIn;
        public double? IntervalSizeIn
        {
            get => _intervalSizeIn;
            set => SetProperty(ref _intervalSizeIn, value);
        }

        private string _presentActivity = string.Empty;
        public string PresentActivity
        {
            get => _presentActivity;
            set => SetProperty(ref _presentActivity, value);
        }

        private string _primaryFluidSet = string.Empty;
        public string PrimaryFluidSet
        {
            get => _primaryFluidSet;
            set => SetProperty(ref _primaryFluidSet, value);
        }

        private string _otherActiveFluids = string.Empty;
        public string OtherActiveFluids
        {
            get => _otherActiveFluids;
            set => SetProperty(ref _otherActiveFluids, value);
        }

        private double? _mudDensity;
        public double? MudDensity
        {
            get => _mudDensity;
            set => SetProperty(ref _mudDensity, value);
        }

        private bool _operationalIssues;
        public bool OperationalIssues
        {
            get => _operationalIssues;
            set => SetProperty(ref _operationalIssues, value);
        }

        public ObservableCollection<ReportPumpOperation> Pumps { get; set; } = new ObservableCollection<ReportPumpOperation>();
        public ObservableCollection<ReportScreenUsage> Screens { get; set; } = new ObservableCollection<ReportScreenUsage>();
        public ObservableCollection<string> OperatorReps { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ContractorReps { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> BaroidReps { get; set; } = new ObservableCollection<string>();



        private double _totalGpm;
        public double TotalGpm
        {
            get => _totalGpm;
            set => SetProperty(ref _totalGpm, value);
        }

        private double _surfacePressureLoss;
        public double SurfacePressureLoss
        {
            get => _surfacePressureLoss;
            set => SetProperty(ref _surfacePressureLoss, value);
        }

        private bool _isDraft = false;
        public bool IsDraft
        {
            get => _isDraft;
            set => SetProperty(ref _isDraft, value);
        }

        #region IDataErrorInfo Implementation

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                string? result = null;

                switch (columnName)
                {
                    case nameof(IntervalNumber):
                        if (string.IsNullOrWhiteSpace(IntervalNumber))
                            result = "Interval # is required.";
                        break;
                    case nameof(MD):
                        if (!MD.HasValue)
                            result = "MD is required.";
                        else if (MD < 0)
                            result = "MD must be positive.";
                        else if (TVD.HasValue && MD < TVD)
                            result = "MD cannot be less than TVD.";
                        break;
                    case nameof(TVD):
                        if (!TVD.HasValue)
                            result = "TVD is required.";
                        else if (TVD < 0)
                            result = "TVD must be positive.";
                        else if (MD.HasValue && TVD > MD)
                            result = "TVD cannot be greater than MD.";
                        break;
                    case nameof(WellSection):
                        if (string.IsNullOrWhiteSpace(WellSection))
                            result = "Well Section is required.";
                        break;
                    case nameof(PresentActivity):
                        if (string.IsNullOrWhiteSpace(PresentActivity))
                            result = "Present Activity is required.";
                        break;
                }

                return result ?? string.Empty;
            }
        }

        #endregion

        public Report Duplicate()
        {
            var clone = new Report
            {
                IntervalNumber = this.IntervalNumber,
                ReportNumber = this.ReportNumber,
                ReportDateTime = DateTime.Now,
                MD = this.MD,
                TVD = this.TVD,
                Activity = this.Activity,
                WellSection = this.WellSection,
                MaxBHT = this.MaxBHT,
                MaxBHTSource = this.MaxBHTSource,
                IntervalSizeIn = this.IntervalSizeIn,
                PresentActivity = this.PresentActivity,
                PrimaryFluidSet = this.PrimaryFluidSet,
                OtherActiveFluids = this.OtherActiveFluids,
                MudDensity = this.MudDensity,
                RigName = this.RigName,
                Contractor = this.Contractor,
                RigType = this.RigType,
                OperationalIssues = this.OperationalIssues,
                CreatedDate = DateTime.Now,
                IsDraft = true,
                OperatorReps = new ObservableCollection<string>(this.OperatorReps),
                ContractorReps = new ObservableCollection<string>(this.ContractorReps),
                BaroidReps = new ObservableCollection<string>(this.BaroidReps)
            };

            // Clone Equipment
            foreach (var p in this.Pumps)
            {
                clone.Pumps.Add(new ReportPumpOperation
                {
                    No = p.No,
                    PumpName = p.PumpName,
                    LinerSize = p.LinerSize,
                    StrokeLength = p.StrokeLength,
                    Efficiency = p.Efficiency,
                    Spm = p.Spm,
                    Pressure = p.Pressure
                });
            }

            foreach (var s in this.Screens)
            {
                clone.Screens.Add(new ReportScreenUsage
                {
                    ShakerName = s.ShakerName,
                    ScreenType = s.ScreenType,
                    Quantity = s.Quantity,
                    IsDeducted = false // New report, fresh deduction
                });
            }

            return clone;
        }
    }
}
