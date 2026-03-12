using ClosedXML.Excel;
using ProjectReport.Models;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Rig;
using ProjectReport.Services;
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
    internal class ReportDViewModel : INotifyPropertyChanged
    {
        private Report? _report;
        private Well _currentWell;
        private WellboreComponent? _wellboreComponent;

        private readonly HydraulicsCalculationService _hydraulicsService = new HydraulicsCalculationService();

        public Report Report
        {
            get => _report;
            set
            {
                _report = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Report> Reports { get; set; }

        public ICommand SaveNewReportCommand { get; }

        // Comandos para seleccionar y remover fluidos
        public ICommand RemoveFluidCommand { get; }

        public ObservableCollection<string> HoleSizeOptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> WellSectionOptions { get; } = new ObservableCollection<string>
        {
            "Sidetrack",
            "Original"
        };

        public class DisplayFluid : INotifyPropertyChanged
        {
            public WellFluid Fluid { get; set; }

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



        // Para binding de fluidos
        public ObservableCollection<WellFluid> SelectedFluids { get; set; } = new ObservableCollection<WellFluid>();
        public ObservableCollection<string> FluidTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<DisplayFluid> FilteredFluids { get; set; } = new ObservableCollection<DisplayFluid>();


        private string _selectedFluid;
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

            
        }

        // Filtrar fluidos según tipo y marcar seleccionados
        private void FilterFluids()
        {
            FilteredFluids.Clear();

            if (_currentWell.SelectedFluids == null || string.IsNullOrEmpty(SelectedFluid))
                return;

            var filtered = _currentWell.SelectedFluids
                .Where(f => f.Type == SelectedFluid)
                .OrderBy(f => f.Name);

            foreach (var f in filtered)
            {
                var display = new DisplayFluid
                {
                    Fluid = f,

                    // sincroniza SIEMPRE con SelectedFluids
                    IsChecked = SelectedFluids.Any(sf =>
                        sf.Name == f.Name &&
                        sf.Type == f.Type)
                };

                display.PropertyChanged += DisplayFluid_PropertyChanged;

                FilteredFluids.Add(display);
            }
        }




        private void DisplayFluid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DisplayFluid.IsChecked))
                return;

            var df = sender as DisplayFluid;
            if (df == null)
                return;

            if (df.IsChecked)
            {
                if (!SelectedFluids.Any(sf =>
                    sf.Name == df.Fluid.Name &&
                    sf.Type == df.Fluid.Type))
                {
                    SelectedFluids.Add(new WellFluid
                    {
                        Name = df.Fluid.Name,
                        Type = df.Fluid.Type
                    });
                }
            }
            else
            {
                var existing = SelectedFluids.FirstOrDefault(sf =>
                    sf.Name == df.Fluid.Name &&
                    sf.Type == df.Fluid.Type);

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

            // Cargar fluidos del último reporte
            SelectedFluids.Clear();

            if (!string.IsNullOrEmpty(lastReport.PrimaryFluidSet))
            {
                var fluids = lastReport.PrimaryFluidSet
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var f in fluids)
                {
                    var parts = f.Trim()
                        .Split('(', ')', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        SelectedFluids.Add(new WellFluid
                        {
                            Name = parts[0].Trim(),
                            Type = parts[1].Trim()
                        });
                    }
                }
            }

            // Refrescar lista izquierda
            FilterFluids();



            HookEvents();
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

        private void OnReportPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Report.MudDensity))
                UpdateHydraulics();
        }

        private void OnPumpPropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private void SaveNewReport()
        {
            if (!ValidateReport())
                return;

            if (SelectedFluids != null && SelectedFluids.Any())
            {
                Report.PrimaryFluidSet = string.Join(", ", SelectedFluids.Select(f => $"{f.Name} ({f.Type})"));
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

            ToastNotificationService.Instance.ShowSuccess("Report saved");
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

        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
            public void Execute(object? parameter) => _execute();

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }

        private class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Func<T, bool> _canExecute;

            public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);
            public void Execute(object parameter) => _execute((T)parameter);

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}
