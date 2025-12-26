using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;

namespace ProjectReport.ViewModels
{
    public class WellDashboardViewModel : BaseViewModel
    {
        private readonly Project _project;
        private Well? _currentWell;
        private readonly string _projectFilePath;

        private readonly GeometryValidationService _geometryValidationService;

        public WellDashboardViewModel(Project project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _projectFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project_data.json");

            NewReportCommand = new RelayCommand(_ => CreateNewReport(), _ => CanCreateReport());
            ViewReportCommand = new RelayCommand(ViewReport, CanInteractWithReport);
            EditReportCommand = new RelayCommand(EditReport, CanInteractWithReport);
            DuplicateReportCommand = new RelayCommand(DuplicateReport, CanInteractWithReport);

            NavigateHomeCommand = new RelayCommand(_ => NavigateHome());

            // ✅ ESTE ERA EL QUE TE FALTABA
            EditWellDataCommand = new RelayCommand(_ => NavigateToWellData(), _ => CurrentWell != null);

            // Initialize Geometry Services
            var geoService = new GeometryCalculationService();
            var dataService = new DataPersistenceService();
            var thermalService = new ThermalGradientService();
            _geometryValidationService = new GeometryValidationService();

            GeometryViewModel = new GeometryViewModel(geoService, dataService, thermalService);
        }

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

                    // ✅ para que el botón se habilite/deshabilite bien
                    
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

        public ICommand NewReportCommand { get; }
        public ICommand ViewReportCommand { get; }
        public ICommand EditReportCommand { get; }
        public ICommand DuplicateReportCommand { get; }
        public ICommand NavigateHomeCommand { get; }

        // ✅ Ya lo tenías declarado: ahora sí se usa
        public ICommand EditWellDataCommand { get; }

        public GeometryViewModel GeometryViewModel { get; }

        public void LoadWell(Well well)
        {
            CurrentWell = well;
            GeometryViewModel.LoadWell(well);
        }

        private void NavigateHome()
        {
            NavigationService.Instance.NavigateToHome();
        }

        // ✅ navegación al módulo “Well Data”
        private void NavigateToWellData()
        {
            if (CurrentWell == null) return;

            NavigationService.Instance.NavigateToWellData(CurrentWell.Id);

        }

        private void Reports_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateReportsEmpty();
        }

        private void UpdateReportsEmpty()
        {
            ReportsEmpty = Reports == null || Reports.Count == 0;
        }

        private bool CanCreateReport()
        {
            return CurrentWell != null;
        }

        private void CreateNewReport()
        {
            if (CurrentWell == null) return;

            var wnd = new ProjectReport.Views.ReportWizardView(CurrentWell, _project);
            var result = wnd.ShowDialog();

            if (result == true)
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
                var wnd = new ProjectReport.Views.ReportWizardView(CurrentWell, _project, report);
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
                    duplicate.Id = CurrentWell.Reports.Count > 0 ? CurrentWell.Reports.Max(r => r.Id) + 1 : 1;
                    CurrentWell.Reports.Add(duplicate);

                    await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);
                    ToastNotificationService.Instance.ShowSuccess("Report duplicated");
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Error duplicating report: {ex.Message}");
                }
            }
        }

        private bool CanInteractWithReport(object? parameter)
        {
            return parameter is Report;
        }
    }
}
