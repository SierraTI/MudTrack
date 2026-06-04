using ProjectReport.Models;
using ProjectReport.Modules.ReportDetail.ViewModels;
using ProjectReport.Modules.ReportDetails.Views;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;

namespace ProjectReport.ViewModels
{
    public class WellDashboardViewModel : BaseViewModel
    {
        private readonly Project _project;
        private Well? _currentWell;
        private readonly string _projectFilePath;

        private readonly GeometryValidationService _geometryValidationService;
        private Report? _Report;
      
        public Report? Report
        {
            get => _Report;
            set
            {
                if (SetProperty(ref _Report, value))
                {
                    // Sync with Context Service for SQL persistence
                    WellContextService.Instance.CurrentReport = _Report;

                    // Cada vez que cambia Report, actualizar GeometryViewModel
                    if (_Report != null)
                        GeometryViewModel.LoadReport(_Report);
                }
            }
        }


        public WellDashboardViewModel(Project project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));

            _projectFilePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "project_data.json"
            );

            NewReportCommand = new RelayCommand(async _ => await CreateNewReport(), _ => CanCreateReport());
            ViewReportCommand = new RelayCommand(ViewReport, CanInteractWithReport);
            EditReportCommand = new RelayCommand(EditReport, CanInteractWithReport);
            DuplicateReportCommand = new RelayCommand(async p => await DuplicateReportAsync(p), CanInteractWithReport);

            NavigateHomeCommand = new RelayCommand(_ => NavigateHome());
            EditWellDataCommand = new RelayCommand(_ => NavigateToWellData(), _ => CurrentWell != null);
            NavigateToRigProfileCommand = new RelayCommand(_ => NavigateToRigProfile(), _ => CurrentWell != null);

            var geoService = new GeometryCalculationService();
            var thermalService = new ThermalGradientService();

            _geometryValidationService = new GeometryValidationService();

            GeometryViewModel = new GeometryViewModel(
                geoService,
                thermalService
            );
        }

        #region Properties

        public Well? CurrentWell
        {
            get => _currentWell;
            set
            {
                if (_currentWell != value)
                {
                    if (_currentWell?.Reports != null)
                        _currentWell.Reports.CollectionChanged -= Reports_CollectionChanged;

                    SetProperty(ref _currentWell, value);

                    if (_currentWell?.Reports != null)
                        _currentWell.Reports.CollectionChanged += Reports_CollectionChanged;

                    OnPropertyChanged(nameof(Reports));
                    UpdateReportsEmpty();
                }
            }
        }

        public ObservableCollection<Report>? Reports => CurrentWell?.Reports;

        private bool _reportsEmpty;

        public bool ReportsEmpty
        {
            get => _reportsEmpty;
            set => SetProperty(ref _reportsEmpty, value);
        }

        public GeometryViewModel GeometryViewModel { get; }

        #endregion

        #region Commands

        public ICommand NewReportCommand { get; }
        public ICommand ViewReportCommand { get; }
        public ICommand EditReportCommand { get; }
        public ICommand DuplicateReportCommand { get; }
        public ICommand NavigateHomeCommand { get; }

        public ICommand EditWellDataCommand { get; }
        public ICommand NavigateToRigProfileCommand { get; }

        #endregion

        #region Load Well

        public async Task LoadWell(Well well)
        {
            if (well == null) return;

            CurrentWell = well;

            // Inicializa la colección si es null
            if (CurrentWell.Reports == null)
                CurrentWell.Reports = new ObservableCollection<Report>();

            // Crear primer reporte si no hay ninguno
            if (!CurrentWell.Reports.Any())
            {
                var firstReport = new Report
                {
                    Id = 1,
                    ReportNumber = 1,
                    IntervalNumber = "1",
                    ReportDateTime = DateTime.Now,
                    MD = 1000,
                    TVD = 0,
                    Activity = string.Empty,
                    WellSection = string.Empty,
                    MudDensity = 0,
                    IsDraft = true
                };

                CurrentWell.Reports.Add(firstReport);
                await SaveProject();
            }

            // Seleccionar el último reporte (o el recién creado)
            var lastReport = CurrentWell.Reports
                .OrderByDescending(r => r.ReportNumber)
                .FirstOrDefault();

            if (lastReport != null)
            {
                Report = lastReport; 
            }

            OnPropertyChanged(nameof(Reports));
        }

        #endregion

        #region Navigation

        private void NavigateHome()
        {
            NavigationService.Instance.NavigateToHome();
        }

        private void NavigateToWellData()
        {
            if (CurrentWell == null) return;

            NavigationService.Instance.NavigateToWellData(CurrentWell.Id);
        }

        private void NavigateToRigProfile()
        {
            if (CurrentWell == null) return;

            NavigationService.Instance.NavigateToRigProfile(CurrentWell.Id);
        }

        #endregion

        #region Reports Management

        private bool CanCreateReport()
        {
            return CurrentWell != null;
        }

        public event Action<Well>? OpenReportDetailsRequested;

        private async Task DeleteReport(Report? reportToDelete)
        {
            if (reportToDelete == null || CurrentWell == null) return;

            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete report #{reportToDelete.ReportNumber}?", 
                "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                CurrentWell.Reports.Remove(reportToDelete);
                await SaveProject();
            }
        }

        private async Task CreateNewReport()
        {
            if (CurrentWell == null) return;

            var result = System.Windows.MessageBox.Show(
                "Do you want to modify Report Details?",
                "New Report",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                OpenReportDetailsRequested?.Invoke(CurrentWell);
            }
            else
            {
                await CreateReportFromPrevious();
            }
        }

        private async Task CreateReportFromPrevious()
        {
            try
            {
                if (CurrentWell == null || CurrentWell.Reports == null) return;

                // ✅ Tomar el último por NUMERO
                var lastReport = CurrentWell.Reports
                    .OrderByDescending(r => r.ReportNumber)
                    .FirstOrDefault();

                Report newReport;
                if (lastReport == null)
                {
                    newReport = new Report
                    {
                        Id = 1,
                        ReportNumber = 1,
                        ReportDateTime = DateTime.Now,
                        IsDraft = true
                    };
                }
                else
                {
                    newReport = lastReport.Duplicate();
                    newReport.Id = CurrentWell.Reports.Any() ? CurrentWell.Reports.Max(r => r.Id) + 1 : 1;
                    newReport.ReportNumber = CurrentWell.Reports.Any() ? CurrentWell.Reports.Max(r => r.ReportNumber) + 1 : 1;
                    newReport.ReportDateTime = DateTime.Now;
                    newReport.IsDraft = true;
                }

                CurrentWell.Reports.Add(newReport);
                await SaveProject();
                Report = newReport;
                OnPropertyChanged(nameof(Reports));
                ToastNotificationService.Instance.ShowSuccess("Report created");
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error: {ex.Message}");
            }
        }

        private void ViewReport(object? parameter)
        {
            if (parameter is Report report)
            {
                EditReport(report);
            }
        }

        private void EditReport(object? parameter)
        {
            if (parameter is Report report && CurrentWell != null)
            {
                var wnd = new ProjectReport.Views.ReportWizardView(
                    CurrentWell,
                    _project,
                    report
                );

                var result = wnd.ShowDialog();

                if (result == true)
                    OnPropertyChanged(nameof(Reports));
            }
        }

        private async Task DuplicateReportAsync(object? parameter)
        {
            if (parameter is Report report && CurrentWell != null)
            {
                try
                {
                    var duplicate = report.Duplicate();

                    int newId = CurrentWell.Reports.Any()
                        ? CurrentWell.Reports.Max(r => r.Id) + 1
                        : 1;

                    int newNumber = CurrentWell.Reports.Any()
                        ? CurrentWell.Reports.Max(r => r.ReportNumber) + 1
                        : 1;

                    duplicate.Id = newId;
                    duplicate.ReportNumber = newNumber;
                    duplicate.ReportDateTime = DateTime.Now;
                    duplicate.IsDraft = true;

                    CurrentWell.Reports.Add(duplicate);
                    await SaveProject();

                    ToastNotificationService.Instance.ShowSuccess("Report duplicated");
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Error: {ex.Message}");
                }
            }
        }

        private bool CanInteractWithReport(object? parameter)
        {
            return parameter is Report;
        }

        #endregion

        #region Helpers

        private void Reports_CollectionChanged(
            object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateReportsEmpty();
        }

        private void UpdateReportsEmpty()
        {
            ReportsEmpty = Reports == null || Reports.Count == 0;
        }

        private async Task SaveProject()
        {
            await WellContextService.Instance.SaveCurrentWell();
            OnPropertyChanged(nameof(Reports));
        }

        #endregion
    }
}
