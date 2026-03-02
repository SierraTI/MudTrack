using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Inventory
{
    public class ReportOtherViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<string> Processes { get; } = new ObservableCollection<string>
        {
            "Prod / fluids : Drilling",
            "Prod / fluids : Completion / Filtration",
            "Solids Control / Waste Management",
            "Prod / fluids : Cementing",
            "Lost / Damage",
            "Other"
        };

        // Lines editable by user
        public ObservableCollection<OtherActivityLine> Lines { get; } = new();

        private OtherActivityLine? _selectedLine;
        public OtherActivityLine? SelectedLine
        {
            get => _selectedLine;
            set => SetProperty(ref _selectedLine, value);
        }

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveCommand { get; }

        public event Action? RequestClose;

        public ReportOtherViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            AddRowCommand = new RelayCommand(_ => AddRow());
            RemoveRowCommand = new RelayCommand(_ => RemoveRow(), _ => SelectedLine != null);
            CancelCommand = new RelayCommand(_ => OnCancel());
            SaveCommand = new RelayCommand(_ => OnSave());

            // Start with one editable row
            AddRow();
        }

        private void AddRow()
        {
            var line = new OtherActivityLine
            {
                Process = Processes.FirstOrDefault() ?? string.Empty,
                Quantity = 0,
                Notes = string.Empty
            };
            Lines.Add(line);
            SelectedLine = line;
        }

        private void RemoveRow()
        {
            if (SelectedLine != null)
            {
                Lines.Remove(SelectedLine);
                SelectedLine = Lines.FirstOrDefault();
            }
        }

        private void OnCancel()
        {
            Debug.WriteLine("ReportOtherViewModel: cancel");
            RequestClose?.Invoke();
        }

        private void OnSave()
        {
            Debug.WriteLine("ReportOtherViewModel: save");

            var validLines = Lines.Where(l => !string.IsNullOrWhiteSpace(l.Process) && l.Quantity > 0).ToList();

            if (validLines.Count == 0)
            {
#if DEBUG
                MessageBox.Show("Add at least one line with quantity > 0.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
#endif
                return;
            }

            try
            {
                var ticket = new Ticket
                {
                    Type = TicketType.Consumed,
                    Date = DateTime.Now,
                    User = Environment.UserName,
                    Observations = string.Empty,
                    Requisition = string.Empty,
                    Lines = validLines.Select(l => new TicketLine
                    {
                        ProductCode = (string.IsNullOrWhiteSpace(l.Process) ? Guid.NewGuid().ToString("N").Substring(0,8).ToUpperInvariant() : l.Process.ToUpperInvariant().Replace(' ', '_').Replace('/','_')),
                        ProductName = l.Process,
                        Quantity = l.Quantity,
                        UnitPrice = 0,
                        Context = l.Notes ?? string.Empty,
                        Observations = l.Notes ?? string.Empty
                    }).ToList()
                };

                _service.CreateTicketConsumed(ticket);

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving activities: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Simple editable line model
        public class OtherActivityLine : BaseViewModel
        {
            private string _process = string.Empty;
            public string Process
            {
                get => _process;
                set => SetProperty(ref _process, value);
            }

            private double _quantity;
            public double Quantity
            {
                get => _quantity;
                set => SetProperty(ref _quantity, value);
            }

            private string _notes = string.Empty;
            public string Notes
            {
                get => _notes;
                set => SetProperty(ref _notes, value);
            }
        }
    }
}

