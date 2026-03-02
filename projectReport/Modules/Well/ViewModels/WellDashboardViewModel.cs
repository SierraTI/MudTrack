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
            set => SetProperty(ref _Report, value);
        }


        public WellDashboardViewModel(Project project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));

            _projectFilePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "project_data.json"
            );

            NewReportCommand = new RelayCommand(_ => CreateNewReport(), _ => CanCreateReport());
            ViewReportCommand = new RelayCommand(ViewReport, CanInteractWithReport);
            EditReportCommand = new RelayCommand(EditReport, CanInteractWithReport);
            DuplicateReportCommand = new RelayCommand(DuplicateReport, CanInteractWithReport);

            NavigateHomeCommand = new RelayCommand(_ => NavigateHome());
            EditWellDataCommand = new RelayCommand(_ => NavigateToWellData(), _ => CurrentWell != null);
            NavigateToRigProfileCommand = new RelayCommand(_ => NavigateToRigProfile(), _ => CurrentWell != null);

            var geoService = new GeometryCalculationService();
            var dataService = new DataPersistenceService();
            var thermalService = new ThermalGradientService();

            _geometryValidationService = new GeometryValidationService();

            GeometryViewModel = new GeometryViewModel(
                geoService,
                dataService,
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
            GeometryViewModel.LoadWell(well);

            // Asegurarse de que la colección de reportes exista
            if (CurrentWell.Reports == null)
                CurrentWell.Reports = new ObservableCollection<Report>();

            // Crear primer reporte si no hay ninguno
            if (!CurrentWell.Reports.Any())
            {
                CreateFirstEmptyReport();
                await SaveProject(); // Guardar inmediatamente
            }

            // Seleccionar el último reporte existente (o el que acabamos de crear)
            if (CurrentWell.Reports.Any())
            {
                Report = CurrentWell.Reports
                    .OrderByDescending(r => r.ReportNumber)
                    .First();
            }

            OnPropertyChanged(nameof(Reports));
        }


        private void CreateFirstEmptyReport()
        {
            if (CurrentWell == null) return;

            var firstReport = new Report
            {
                Id = 1,
                ReportNumber = 1,
                IntervalNumber = "1",

                // Fecha fija inicial
                ReportDateTime = DateTime.Now,

                MD = 0,
                TVD = 0,
                Activity = string.Empty,
                WellSection = string.Empty,
                MudDensity = 0,

                IsDraft = true
            };

            CurrentWell.Reports.Add(firstReport);

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

        private void CreateNewReport()
        {
            if (_currentWell == null) return;

            var result = MessageBox.Show(
                "Do you want to modify Report Details?",
                "New Report",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                OpenReportDetailsRequested?.Invoke(_currentWell);
            }
            else
            {
                CreateReportFromPrevious();
            }
        }




        private async void CreateReportFromPrevious()
        {
            if (CurrentWell == null || CurrentWell.Reports == null) return;

            // ✅ Tomar el último por NUMERO
            var lastReport = CurrentWell.Reports
                .OrderByDescending(r => r.ReportNumber)
                .FirstOrDefault();

            if (lastReport == null) return;

            var newReport = lastReport.Duplicate();

            int newId = CurrentWell.Reports.Any()
                ? CurrentWell.Reports.Max(r => r.Id) + 1
                : 1;

            int newNumber = CurrentWell.Reports.Any()
                ? CurrentWell.Reports.Max(r => r.ReportNumber) + 1
                : 1;

            newReport.Id = newId;
            newReport.ReportNumber = newNumber;

            // Mantener intervalo anterior
            newReport.IntervalNumber = lastReport.IntervalNumber;

            // Fecha solo informativa
            newReport.ReportDateTime = DateTime.Now;

            newReport.IsDraft = true;

            CurrentWell.Reports.Add(newReport);

            await SaveProject();

            // ✅ Forzar selección del nuevo
            Report = newReport;

            OnPropertyChanged(nameof(Reports));

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

        private async void DuplicateReport(object? parameter)
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
            await DataPersistenceService.SaveProjectAsync(
                _projectFilePath,
                _project
            );

            OnPropertyChanged(nameof(Reports));
        }



        #endregion
    }
}
