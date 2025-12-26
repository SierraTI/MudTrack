using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;

namespace ProjectReport.ViewModels
{
    public class ReportWizardViewModel : BaseViewModel
    {
        private readonly Well _well;
        private readonly DataPersistenceService _dataService;
        private readonly string _projectFilePath;
        private Project _project;
        private readonly Report? _originalReport; // For edit mode tracking
        private bool _isEditMode;

        public ReportWizardViewModel(Well well, Project project, Report? reportToEdit = null)
        {
            _well = well ?? throw new ArgumentNullException(nameof(well));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _projectFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project_data.json");
            _dataService = new DataPersistenceService();

            _originalReport = reportToEdit;
            _isEditMode = reportToEdit != null;

            // Initialize Report
            InitializeReport();

            // Commands
            NextCommand = new RelayCommand(GoNext, CanGoNext);
            BackCommand = new RelayCommand(GoBack, CanGoBack);
            CancelCommand = new RelayCommand(Cancel);
            SaveDraftCommand = new RelayCommand(SaveDraft);
            FinishCommand = new RelayCommand(Finish, CanFinish);

            // Inherited Fields visibility: Only relevant for NEW reports using Jalar
            InheritedFields = !_isEditMode && _well.Reports != null && _well.Reports.Count > 0;
        }

        #region Properties

        private Report _report = null!;
        public Report Report
        {
            get => _report;
            set => SetProperty(ref _report, value);
        }

        private int _currentStep = 1;
        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    OnPropertyChanged(nameof(IsStep1Active));
                    OnPropertyChanged(nameof(IsStep2Active));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsStep1Active => CurrentStep == 1;
        public bool IsStep2Active => CurrentStep == 2;

        public bool InheritedFields { get; private set; }
        public bool IsEditMode => _isEditMode;

        #endregion

        #region Commands

        public ICommand NextCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand FinishCommand { get; }

        public event Action? RequestClose;

        #endregion

        #region Methods

        private void InitializeReport()
        {
            if (_isEditMode && _originalReport != null)
            {
                // EDIT MODE: Create a deep copy to work on
                Report = _originalReport.Duplicate();
                Report.Id = _originalReport.Id; // Preserve ID (Duplicate might treat it as new)
            }
            else
            {
                // NEW MODE
                var newReport = new Report
                {
                    ReportDateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    IsDraft = true,
                    IntervalNumber = (_well.Reports?.Count ?? 0) + 1 + "" 
                };

                // Jalar (Inheritance) Logic
                var lastReport = _well.Reports?.OrderByDescending(r => r.ReportDateTime).FirstOrDefault();
                if (lastReport != null)
                {
                    newReport.PresentActivity = lastReport.PresentActivity;
                    newReport.PrimaryFluidSet = lastReport.PrimaryFluidSet;
                    newReport.OtherActiveFluids = lastReport.OtherActiveFluids;
                    newReport.WellSection = lastReport.WellSection;
                    
                    foreach(var op in lastReport.OperatorReps) newReport.OperatorReps.Add(op);
                    foreach(var c in lastReport.ContractorReps) newReport.ContractorReps.Add(c);
                    foreach(var b in lastReport.BaroidReps) newReport.BaroidReps.Add(b);
                }

                Report = newReport;
            }
        }

        public void StartFresh()
        {
            // Clear inherited fields
            Report.PresentActivity = "";
            Report.PrimaryFluidSet = "";
            Report.OtherActiveFluids = "";
            Report.WellSection = "";
            Report.OperatorReps.Clear();
            Report.ContractorReps.Clear();
            Report.BaroidReps.Clear();
            InheritedFields = false;
            OnPropertyChanged(nameof(InheritedFields));
        }

        private void GoNext(object? obj)
        {
            if (CurrentStep < 2)
            {
                // Use IDataErrorInfo validation check for Step 1
                if (CurrentStep == 1 && !ValidateStep1()) return;
                
                CurrentStep++;
            }
            else
            {
                // Should not happen if UI hides "Next" on Step 2, but fail-safe
                Finish(obj);
            }
        }

        private bool CanGoNext(object? obj)
        {
            if (CurrentStep == 1)
            {
                // Optional: Live validation disable
                // return string.IsNullOrEmpty(Report["IntervalNumber"]) && ...
            }
            return CurrentStep < 2; 
        }

        private void GoBack(object? obj)
        {
            if (CurrentStep > 1)
                CurrentStep--;
        }

        private bool CanGoBack(object? obj)
        {
            return CurrentStep > 1;
        }

        private void Cancel(object? obj)
        {
             var result = MessageBox.Show("Are you sure you want to cancel? Any unsaved changes will be lost.", 
                 "Cancel Report", MessageBoxButton.YesNo, MessageBoxImage.Question);
                 
             if (result == MessageBoxResult.Yes)
             {
                 RequestClose?.Invoke();
             }
        }

        private async void SaveDraft(object? obj)
        {
            Report.IsDraft = true;
            await SaveReportAsync();
            ToastNotificationService.Instance.ShowSuccess("Draft saved");
            RequestClose?.Invoke();
        }

        private async void Finish(object? obj)
        {
            if (!ValidateAll()) return;

            Report.IsDraft = false;
            await SaveReportAsync();
            ToastNotificationService.Instance.ShowSuccess("Report completed");

            NavigationService.Instance.NavigateToGeometry(_well.Id);

            RequestClose?.Invoke();
        }

        private bool CanFinish(object? obj)
        {
            return true;
        }

        private bool ValidateStep1()
        {
            // Check for IDataErrorInfo errors
            if (!string.IsNullOrEmpty(Report["IntervalNumber"]) ||
                !string.IsNullOrEmpty(Report["MD"]) ||
                !string.IsNullOrEmpty(Report["TVD"]) ||
                !string.IsNullOrEmpty(Report["WellSection"]) ||
                !string.IsNullOrEmpty(Report["PresentActivity"]))
            {
                ToastNotificationService.Instance.ShowError("Please fix validation errors before proceeding.");
                return false;
            }
            return true;
        }

        private bool ValidateAll()
        {
            return ValidateStep1();
        }

        private async Task SaveReportAsync()
        {
            if (_isEditMode && _originalReport != null)
            {
                // Update existing report in the collection
                var index = _well.Reports?.IndexOf(_originalReport) ?? -1;
                if (index >= 0 && _well.Reports != null)
                {
                    _well.Reports[index] = Report; // Replace with modified copy
                }
            }
            else
            {
                // New Report
                if (Report.Id == 0)
                {
                    Report.Id = (_well.Reports?.Count > 0 ? _well.Reports.Max(r => r.Id) : 0) + 1;
                    _well.Reports?.Add(Report);
                }
            }

            // Save Project
            await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);
        }

        #endregion
    }
}
