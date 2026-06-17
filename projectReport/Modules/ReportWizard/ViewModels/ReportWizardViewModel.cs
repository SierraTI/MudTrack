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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ProjectReport.Models.Well;

namespace ProjectReport.ViewModels
{
    public class ReportWizardViewModel : BaseViewModel
    {
        private readonly Well _well;
        private readonly Project _project;
        private readonly string _projectFilePath;

        private readonly InventoryService _inventoryService;
        private readonly HydraulicsCalculationService _hydraulicsService;
        private bool _isUpdatingSelection = false;

        public ReportWizardViewModel(Well well, Project project, Report? reportToEdit)
        {
            _well = well ?? throw new ArgumentNullException(nameof(well));
            _project = project ?? throw new ArgumentNullException(nameof(project));

            _projectFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "project_data.json"
            );

            _inventoryService = new InventoryService(new SqliteInventoryRepository());
            _hydraulicsService = new HydraulicsCalculationService();

            Report = reportToEdit ?? new Report();
            
            // Sync with Context Service for SQL persistence
            WellContextService.Instance.CurrentReport = Report;

            HookEvents();
            LoadHoleSizeList();
            LoadFluidTypes();
            LoadExistingFluids();

            NextCommand = new RelayCommand(GoNext);
            BackCommand = new RelayCommand(GoBack);
            CancelCommand = new RelayCommand(Cancel);
            SaveDraftCommand = new RelayCommand(SaveDraft);
            FinishCommand = new RelayCommand(Finish);
            RemoveFluidCommand = new RelayCommand<WellFluid>(RemoveFluid);

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

        #region FLUIDS (NUEVO)

        public class DisplayFluid : BaseViewModel
        {
            public WellFluid Fluid { get; set; } = new WellFluid();

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;
                set => SetProperty(ref _isChecked, value);
            }
        }

        public ObservableCollection<WellFluid> SelectedFluids { get; }
            = new ObservableCollection<WellFluid>();

        public ObservableCollection<string> FluidTypes { get; }
            = new ObservableCollection<string>();

        public ObservableCollection<DisplayFluid> FilteredFluids { get; }
            = new ObservableCollection<DisplayFluid>();

        private string _selectedFluid = string.Empty;
        public string SelectedFluid
        {
            get => _selectedFluid;
            set
            {
                if (SetProperty(ref _selectedFluid, value))
                    FilterFluids();
            }
        }

        public ICommand RemoveFluidCommand { get; }

        private void LoadExistingFluids()
        {
            SelectedFluids.Clear();

            if (Report.ActiveFluids == null)
                return;

            foreach (var fluid in Report.ActiveFluids)
            {
                SelectedFluids.Add(new WellFluid
                {
                    Name = fluid.Name,
                    Type = fluid.Type
                });
            }

            FilterFluids();
        }

        private void LoadFluidTypes()
        {
            // First, load from the Global Fluid Catalog synchronized with DB
            var catalogTypes = WellContextService.Instance.FluidCatalog.ToList();
            
            // Also include types already selected for this well if not in catalog
            if (_well.SelectedFluids != null)
            {
                var wellTypes = _well.SelectedFluids.Select(f => f.Type).Distinct();
                foreach (var t in wellTypes)
                {
                    if (!catalogTypes.Contains(t)) catalogTypes.Add(t);
                }
            }

            FluidTypes.Clear();
            foreach (var t in catalogTypes.OrderBy(x => x))
                FluidTypes.Add(t);

            if (FluidTypes.Any())
                SelectedFluid = FluidTypes.First();
        }

        private void FilterFluids()
        {
            _isUpdatingSelection = true;

            try
            {
                FilteredFluids.Clear();

                if (_well.SelectedFluids == null || string.IsNullOrEmpty(SelectedFluid))
                    return;

                var filtered = _well.SelectedFluids
                    .Where(f => f.Type == SelectedFluid)
                    .OrderBy(f => f.Name);

                foreach (var f in filtered)
                {
                    bool isSelected = SelectedFluids.Any(sf =>
                        sf.Name == f.Name &&
                        sf.Type == f.Type);

                    var display = new DisplayFluid
                    {
                        Fluid = f,
                        IsChecked = isSelected
                    };

                    display.PropertyChanged += DisplayFluid_PropertyChanged;

                    FilteredFluids.Add(display);
                }
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }

        private void DisplayFluid_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingSelection || e.PropertyName != nameof(DisplayFluid.IsChecked))
                return;

            var display = sender as DisplayFluid;
            if (display == null)
                return;

            var fluid = display.Fluid;

            if (display.IsChecked)
            {
                if (!SelectedFluids.Any(sf =>
                    sf.Name == fluid.Name &&
                    sf.Type == fluid.Type))
                {
                    SelectedFluids.Add(new WellFluid
                    {
                        Name = fluid.Name,
                        Type = fluid.Type
                    });
                }
            }
            else
            {
                var existing = SelectedFluids.FirstOrDefault(sf =>
                    sf.Name == fluid.Name &&
                    sf.Type == fluid.Type);

                if (existing != null)
                    SelectedFluids.Remove(existing);
            }
        }

        private void RemoveFluid(WellFluid? fluid)
        {
            if (fluid == null)
                return;

            if (SelectedFluids.Contains(fluid))
                SelectedFluids.Remove(fluid);

            var display = FilteredFluids.FirstOrDefault(f =>
                f.Fluid.Name == fluid.Name &&
                f.Fluid.Type == fluid.Type);

            if (display != null)
            {
                _isUpdatingSelection = true;
                display.IsChecked = false;
                _isUpdatingSelection = false;
            }
        }

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

                // NUEVO: guardar fluidos
                if (Report.ActiveFluids == null)
                    Report.ActiveFluids = new ObservableCollection<WellFluid>();

                Report.ActiveFluids.Clear();

                foreach (var fluid in SelectedFluids)
                {
                    Report.ActiveFluids.Add(new WellFluid
                    {
                        Name = fluid.Name,
                        Type = fluid.Type
                    });
                }

                Report.PrimaryFluidSet = string.Join(", ",
                    Report.ActiveFluids.Select(f => $"{f.Name} ({f.Type})"));

                await SaveReportAsync();

                ToastNotificationService.Instance.ShowSuccess("Report saved successfully.");

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
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
            await WellContextService.Instance.SaveCurrentWell();
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
                    "Assets",
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
