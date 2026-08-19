using ClosedXML.Excel;
using ProjectReport.Models;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Rig;
using ProjectReport.Services;
using ProjectReport.ViewModels; // Added for RelayCommand
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using static ProjectReport.Models.Well;

namespace ProjectReport.Modules.ReportDetail.ViewModels
{
    /// <summary>
    /// ViewModel for the Report Details module, handling report creation and editing.
    /// </summary>
    internal class ReportDViewModel : INotifyPropertyChanged
    {
        private Report _report = new Report();
        private Well _currentWell;
        private bool _isUpdatingSelection = false;

        private readonly HydraulicsCalculationService _hydraulicsService = new HydraulicsCalculationService();

        public Report Report
        {
            get => _report;
            set
            {
                _report = value ?? new Report();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Report> Reports { get; set; }

        public ICommand SaveNewReportCommand { get; }
        public ICommand RemoveFluidCommand { get; }

        public ObservableCollection<string> HoleSizeOptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> WellSectionOptions { get; } = new ObservableCollection<string>
        {
            "Sidetrack",
            "Original"
        };

        public class DisplayFluid : INotifyPropertyChanged
        {
            public WellFluid Fluid { get; set; } = new WellFluid();

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked != value)
                    {
                        _isChecked = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public ObservableCollection<WellFluid> SelectedFluids { get; set; } = new ObservableCollection<WellFluid>();
        public ObservableCollection<string> FluidTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<DisplayFluid> FilteredFluids { get; set; } = new ObservableCollection<DisplayFluid>();


        private string _selectedFluid = string.Empty;
        public string SelectedFluid
        {
            get => _selectedFluid;
            set
            {
                if (_selectedFluid != value)
                {
                    _selectedFluid = value;
                    OnPropertyChanged();
                    FilterFluids();
                }
            }
        }

        public ReportDViewModel(Well well)
        {
            if (well == null)
                throw new ArgumentNullException(nameof(well));

            _currentWell = well;

            Reports = well.Reports != null
                ? new ObservableCollection<Report>(well.Reports)
                : new ObservableCollection<Report>();

            LoadHoleSizeList();
            LoadFluidTypes();
            CreateReportFromPrevious();

            SaveNewReportCommand = new RelayCommand(SaveNewReport);
            RemoveFluidCommand = new RelayCommand<WellFluid>(RemoveFluid);
        }


        // Filtrar fluidos según tipo y marcar seleccionados
        private void FilterFluids()
        {
            _isUpdatingSelection = true;

            try
            {
                FilteredFluids.Clear();

                if (_currentWell.SelectedFluids == null || string.IsNullOrEmpty(SelectedFluid))
                    return;

                var filtered = _currentWell.SelectedFluids
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
                if (!SelectedFluids.Any(sf => sf.Name == fluid.Name && sf.Type == fluid.Type))
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


        private void LoadFluidTypes()
        {
            if (_currentWell.SelectedFluids == null || !_currentWell.SelectedFluids.Any())
                return;

            var types = _currentWell.SelectedFluids
                .Select(f => f.Type)
                .Distinct()
                .OrderBy(t => t);

            FluidTypes.Clear();
            foreach (var t in types)
                FluidTypes.Add(t);

            if (FluidTypes.Any())
                SelectedFluid = FluidTypes.First();
        }


        private void CreateReportFromPrevious()
        {
            if (Reports.Count == 0)
            {
                Report = new Report
                {
                    ReportNumber = 1,
                    ReportDateTime = DateTime.Now,
                    IsDraft = true
                };

                SelectedFluids.Clear();
                FilterFluids();

                HookEvents();
                return;
            }

            var lastReport = Reports
                .OrderByDescending(r => r.ReportNumber)
                .First();

            var newReport = lastReport.Duplicate();
            newReport.Id = 0;
            newReport.ReportNumber = lastReport.ReportNumber + 1;
            newReport.ReportDateTime = DateTime.Now;
            newReport.IsDraft = true;

            if (_currentWell.RigProfile != null)
            {
                newReport.RigName = _currentWell.RigProfile.RigName;
                newReport.Contractor = _currentWell.RigProfile.Contractor;
                newReport.RigType = _currentWell.RigProfile.RigType;
            }

            if (newReport.Pumps.Count == 0 && _currentWell.RigProfile?.Pumps != null)
            {
                foreach (var rp in _currentWell.RigProfile.Pumps)
                {
                    var op = new ReportPumpOperation { No = rp.No };
                    op.UpdateFromRigPump(rp);
                    newReport.Pumps.Add(op);
                }
            }

            if (newReport.Screens.Count == 0 && _currentWell.RigProfile?.SolidsControl != null)
            {
                foreach (var sc in _currentWell.RigProfile.SolidsControl)
                {
                    newReport.Screens.Add(new ReportScreenUsage
                    {
                        ShakerName = $"{sc.Manufacturer} {sc.Model}",
                        ScreenType = sc.ScreenType
                    });
                }
            }

            Report = newReport;
            SelectedFluids.Clear();

            if (lastReport.ActiveFluids != null && lastReport.ActiveFluids.Any())
            {
                foreach (var fluid in lastReport.ActiveFluids)
                {
                    SelectedFluids.Add(new WellFluid
                    {
                        Name = fluid.Name,
                        Type = fluid.Type
                    });
                }
            }

            HookEvents();
            FilterFluids();
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


        private void HookEvents()
        {
            if (Report == null) return;

            Report.PropertyChanged -= OnReportPropertyChanged;
            Report.PropertyChanged += OnReportPropertyChanged;

            foreach (var pump in Report.Pumps)
            {
                pump.PropertyChanged -= OnPumpPropertyChanged;
                pump.PropertyChanged += OnPumpPropertyChanged;
            }
        }

        private void OnReportPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Report.MudDensity))
                UpdateHydraulics();
        }

        private void OnPumpPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportPumpOperation.Gpm) ||
                e.PropertyName == nameof(ReportPumpOperation.Spm))
                UpdateHydraulics();
        }

        private void UpdateHydraulics()
        {
            if (Report == null) return;

            Report.TotalGpm = Math.Round(Report.Pumps.Sum(p => p.Gpm), 2);

            if (_currentWell.RigProfile != null && Report.MudDensity.HasValue)
            {
                Report.SurfacePressureLoss =
                    _hydraulicsService.CalculateTotalSurfacePressureLoss(
                        _currentWell.RigProfile,
                        Report.MudDensity.Value,
                        Report.TotalGpm);
            }
            else
            {
                Report.SurfacePressureLoss = 0;
            }
        }

        private async void SaveNewReport()
        {
            if (!ValidateReport())
                return;

            if (Report.ActiveFluids == null)
                Report.ActiveFluids = new ObservableCollection<WellFluid>();

            Report.ActiveFluids.Clear();

            if (SelectedFluids != null && SelectedFluids.Any())
            {
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

                Report.OtherActiveFluids = string.Empty;
            }
            else
            {
                Report.PrimaryFluidSet = string.Empty;
                Report.OtherActiveFluids = string.Empty;
            }

            if (_currentWell.Reports == null)
                _currentWell.Reports = new ObservableCollection<Report>();

            if (Report.Id == 0)
            {
                Report.Id = _currentWell.Reports.Any()
                    ? _currentWell.Reports.Max(r => r.Id) + 1
                    : 1;

                _currentWell.Reports.Add(Report);
            }

            if (!Reports.Contains(Report))
                Reports.Add(Report);

            // Persist to database
            WellContextService.Instance.CurrentWell = _currentWell;
            WellContextService.Instance.CurrentReport = Report;
            await WellContextService.Instance.SaveCurrentWell();

            ToastNotificationService.Instance.ShowSuccess("Report saved to database");
            OnReportSaved?.Invoke(this, Report);
        }


        private void PrepareNextReport()
        {
            var newReport = Report.Duplicate();
            newReport.Id = 0;
            newReport.ReportNumber++;
            newReport.ReportDateTime = DateTime.Now;
            newReport.IsDraft = true;

            Report = newReport;
            HookEvents();
        }

        public event EventHandler<Report>? OnReportSaved;

        private bool ValidateReport()
        {
            if (Report == null) return false;

            if (string.IsNullOrWhiteSpace(Report.IntervalNumber))
            {
                MessageBox.Show("Interval Number is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Report.IntervalSizeIn))
            {
                MessageBox.Show("Interval Size is required.");
                return false;
            }

            if (Report.MD < 0)
            {
                MessageBox.Show("MD must be greater than 0.");
                return false;
            }

            if (Report.TVD < 0)
            {
                MessageBox.Show("TVD must be greater than 0.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Report.WellSection))
            {
                MessageBox.Show("Well Section is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Report.PresentActivity))
            {
                MessageBox.Show("Present Activity is required.");
                return false;
            }

            return true;
        }

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
                {
                    MessageBox.Show("No se encontró el archivo:\n" + filePath);
                    return;
                }

                HoleSizeOptions.Clear();

                using (var workbook = new XLWorkbook(filePath))
                {
                    var sheet = workbook.Worksheet(1);
                    var rows = sheet.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        var value = row.Cell(1).GetFormattedString().Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                            HoleSizeOptions.Add(value);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error leyendo Excel:\n" + ex.Message);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}