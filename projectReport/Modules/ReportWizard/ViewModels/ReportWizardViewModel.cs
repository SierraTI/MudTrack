using ClosedXML.Excel;
using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Inventory;
using ProjectReport.Models.Rig;
using ProjectReport.Services;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels.Geometry;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace ProjectReport.ViewModels
{
    public class ReportWizardViewModel : BaseViewModel
    {
        private readonly Well _well;
        private readonly Project _project;
        private readonly string _projectFilePath;

        private readonly InventoryService _inventoryService;
        private readonly HydraulicsCalculationService _hydraulicsService;

        public ReportWizardViewModel(Well well, Project project, Report reportToEdit)
        {
            _well = well ?? throw new ArgumentNullException(nameof(well));
            _project = project ?? throw new ArgumentNullException(nameof(project));

            if (reportToEdit == null)
                throw new ArgumentNullException(nameof(reportToEdit));

            _projectFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "project_data.json"
            );

            _inventoryService = new InventoryService(new JsonInventoryRepository());
            _hydraulicsService = new HydraulicsCalculationService();

            // ⚡ IMPORTANTE: editar el mismo objeto
            Report = reportToEdit;

            HookEvents();

            LoadHoleSizeList();

            NextCommand = new RelayCommand(GoNext);
            BackCommand = new RelayCommand(GoBack);
            CancelCommand = new RelayCommand(Cancel);
            SaveDraftCommand = new RelayCommand(SaveDraft);
            FinishCommand = new RelayCommand(Finish);

            UpdateHydraulics();
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
                }
            }
        }

        public bool IsStep1Active => CurrentStep == 1;
        public bool IsStep2Active => CurrentStep == 2;

        #endregion

        #region Commands

        public ICommand NextCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand FinishCommand { get; }

        public event Action? RequestClose;

        #endregion

        #region Hydraulics

        private void HookEvents()
        {
            if (Report == null)
                return;

            Report.PropertyChanged += OnReportPropertyChanged;

            Report.Pumps.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (ReportPumpOperation p in e.NewItems)
                        p.PropertyChanged += OnPumpPropertyChanged;

                if (e.OldItems != null)
                    foreach (ReportPumpOperation p in e.OldItems)
                        p.PropertyChanged -= OnPumpPropertyChanged;

                UpdateHydraulics();
            };

            foreach (var p in Report.Pumps)
                p.PropertyChanged += OnPumpPropertyChanged;
        }

        private void OnReportPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Report.MudDensity))
                UpdateHydraulics();
        }

        private void OnPumpPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportPumpOperation.Gpm) ||
                e.PropertyName == nameof(ReportPumpOperation.Spm))
            {
                UpdateHydraulics();
            }
        }

        private void UpdateHydraulics()
        {
            if (Report == null) return;

            Report.TotalGpm = Math.Round(
                Report.Pumps.Sum(p => p.Gpm),
                2
            );

            if (_well.RigProfile != null && Report.MudDensity.HasValue)
            {
                Report.SurfacePressureLoss =
                    _hydraulicsService.CalculateTotalSurfacePressureLoss(
                        _well.RigProfile,
                        Report.MudDensity.Value,
                        Report.TotalGpm
                    );
            }
            else
            {
                Report.SurfacePressureLoss = 0;
            }
        }

        #endregion

        #region Navigation

        private void GoNext(object? obj)
        {
            if (CurrentStep == 1)
            {
                if (!ValidateStep1()) return;

                CurrentStep = 2;
            }
            else
            {
                Finish(obj);
            }
        }

        private void GoBack(object? obj)
        {
            if (CurrentStep > 1)
                CurrentStep--;
        }

        #endregion

        #region Actions

        private void Cancel(object? obj)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel?",
                "Cancel Report",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                RequestClose?.Invoke();
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
            try
            {
                if (!ValidateAll()) return;

                Report.IsDraft = false;

                DeductScreensFromInventory();

                await SaveReportAsync();

                // Mostrar mensaje de éxito
                ToastNotificationService.Instance.ShowSuccess("Report saved successfully.");

                // Por ahora no navegamos a Geometry
                // NavigationService.Instance.NavigateToGeometry(_well);

                // Cerrar modal/ventana
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                // Si hay un error al guardar, mostramos mensaje de error
                ToastNotificationService.Instance.ShowError(
                    $"Error saving report: {ex.Message}"
                );
            }
        }



        #endregion

        #region Inventory

        private void DeductScreensFromInventory()
        {
            if (Report.Screens == null || Report.Screens.Count == 0)
                return;

            var ticket = new Ticket
            {
                Date = Report.ReportDateTime,
                Type = TicketType.Consumed,
                User = "System",
                Observations = $"Daily Report {Report.IntervalNumber}",
                Lines = new List<TicketLine>()
            };

            foreach (var screen in Report.Screens)
            {
                if (screen.Quantity > 0 && !screen.IsDeducted)
                {
                    ticket.Lines.Add(new TicketLine
                    {
                        ProductCode = screen.ScreenType,
                        ProductName = $"{screen.ShakerName} Screen",
                        Quantity = screen.Quantity
                    });

                    screen.IsDeducted = true;
                }
            }

            if (ticket.Lines.Count > 0)
            {
                try
                {
                    _inventoryService.CreateTicketConsumed(ticket);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        #endregion

        #region Validation

        private bool ValidateStep1()
        {
            if (!string.IsNullOrEmpty(Report["IntervalNumber"]))
            {
                ToastNotificationService.Instance.ShowError(
                    "Please fix validation errors"
                );
                return false;
            }

            return true;
        }

        private bool ValidateAll()
        {
            return ValidateStep1();
        }

        #endregion

        #region Persistence

        private async Task SaveReportAsync()
        {
            // ⚡ SOLO guarda el proyecto
            await DataPersistenceService.SaveProjectAsync(
                _projectFilePath,
                _project
            );
        }

        #endregion

        #region Lists

        public ObservableCollection<string> WellSectionOptions { get; }
            = new ObservableCollection<string>
            {
                "Sidetrack",
                "Original"
            };

        public ObservableCollection<string> HoleSizeOptions { get; }
            = new ObservableCollection<string>();

        private void LoadHoleSizeList()
        {
            try
            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Data",
                    "HoleSizeList.xlsx"
                );

                if (!File.Exists(filePath))
                    return;

                HoleSizeOptions.Clear();

                using var workbook = new XLWorkbook(filePath);

                var sheet = workbook.Worksheet(1);

                foreach (var row in sheet.RowsUsed().Skip(1))
                {
                    var value = row.Cell(1)
                        .GetFormattedString()
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(value))
                        HoleSizeOptions.Add(value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion
    }
}
