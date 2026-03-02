using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProjectReport.Models;

namespace ProjectReport.Modules.ReportDetail.ViewModels
{
    internal class ReportDViewModel : INotifyPropertyChanged
    {
        private Report _report;
        private Well _currentWell;

        public Report Report
        {
            get => _report;
            set { _report = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Report> Reports { get; set; }

        public ICommand SaveNewReportCommand { get; }

        public ObservableCollection<string> WellSectionOptions { get; }
            = new ObservableCollection<string>
            {
                "Sidetrack",
                "Original"
            };

        public ReportDViewModel(Well well)
        {
            if (well == null)
                throw new ArgumentNullException(nameof(well), "El pozo no puede ser nulo.");

            _currentWell = well;

            // Inicializa ObservableCollection para UI
            Reports = well.Reports != null
                ? new ObservableCollection<Report>(well.Reports)
                : new ObservableCollection<Report>();

            // Mostrar el último reporte si existe, sino uno vacío
            if (Reports.Count > 0)
            {
                var last = Reports[^1];
                // Crear una copia para no modificar el original
                Report = new Report
                {
                    IntervalNumber = last.IntervalNumber,
                    ReportNumber = last.ReportNumber + 1, // Incrementar en 1 para el nuevo
                    ReportDateTime = last.ReportDateTime,
                    MD = last.MD,
                    TVD = last.TVD,
                    WellSection = last.WellSection,
                    MaxBHT = last.MaxBHT,
                    PresentActivity = last.PresentActivity,
                    PrimaryFluidSet = last.PrimaryFluidSet,
                    OperatorReps = new ObservableCollection<string>(last.OperatorReps ?? new ObservableCollection<string>()),
                    ContractorReps = new ObservableCollection<string>(last.ContractorReps ?? new ObservableCollection<string>()),
                    BaroidReps = new ObservableCollection<string>(last.BaroidReps ?? new ObservableCollection<string>())
                };
            }
            else
            {
                Report = new Report
                {
                    ReportNumber = 1 // Si no hay reportes, el primero empieza en 1
                };
            }

            // Comando para guardar un nuevo reporte
            SaveNewReportCommand = new RelayCommand(SaveNewReport);
        }

        private void SaveNewReport()
        {
            if (_report == null) return;

            // Inicializar la lista interna del pozo si es null
            if (_currentWell.Reports == null)
                _currentWell.Reports = new ObservableCollection<Report>();

            // Determinar el siguiente número de reporte
            int nextReportNumber = 1;
            if (_currentWell.Reports.Count > 0)
                nextReportNumber = _currentWell.Reports[^1].ReportNumber + 1;

            // Crear un nuevo reporte basado en lo que escribió el usuario
            var newReport = new Report
            {
                IntervalNumber = _report.IntervalNumber,
                ReportNumber = nextReportNumber, // Asignar número incrementado
                ReportDateTime = _report.ReportDateTime,
                MD = _report.MD,
                TVD = _report.TVD,
                WellSection = _report.WellSection,
                MaxBHT = _report.MaxBHT,
                PresentActivity = _report.PresentActivity,
                PrimaryFluidSet = _report.PrimaryFluidSet,
                OperatorReps = new ObservableCollection<string>(_report.OperatorReps ?? new ObservableCollection<string>()),
                ContractorReps = new ObservableCollection<string>(_report.ContractorReps ?? new ObservableCollection<string>()),
                BaroidReps = new ObservableCollection<string>(_report.BaroidReps ?? new ObservableCollection<string>())
            };

            // Agregar el nuevo reporte a la colección interna y a la UI
            _currentWell.Reports.Add(newReport);
            Reports.Add(newReport);

            // Disparar evento opcional para navegación
            OnReportSaved?.Invoke(this, newReport);

            // Preparar un nuevo reporte vacío listo para llenar
            Report = new Report
            {
                ReportNumber = newReport.ReportNumber + 1 // Siguiente número listo para el próximo
            };
        }

        // Evento para que la vista principal navegue después de guardar
        public event EventHandler<Report> OnReportSaved;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        // RelayCommand simple incluido aquí
        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
            public void Execute(object parameter) => _execute();

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}
