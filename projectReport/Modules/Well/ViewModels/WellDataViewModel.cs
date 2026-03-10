using Microsoft.Win32;
using ProjectReport.Models;
using ProjectReport.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ClosedXML.Excel; // <-- ClosedXML

namespace ProjectReport.ViewModels
{
    public class WellDataViewModel : BaseViewModel
    {
        private readonly Project _project;
        private Well _currentWell;
        private bool _isAutoSaveEnabled = true;
        private DateTime _lastSaved = DateTime.Now;
        private System.Timers.Timer? _autoSaveTimer;
        private readonly string _projectFilePath;

        public WellDataViewModel(Project project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _currentWell = new Well();
            _projectFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project_data.json");

            // Cargar fluidos desde Excel
            LoadFluidsFromExcel();

            // Inicializa dropdown options
            InitializeDropdownOptions();

            // Inicializa comandos
            SaveAndOpenDashboardCommand = new RelayCommand(async _ => await SaveAndOpenDashboardAsync());
            CancelCommand = new RelayCommand(_ => Cancel());
            UploadLogoCommand = new RelayCommand(_ => UploadLogo());
            RemoveLogoCommand = new RelayCommand(_ => RemoveLogo(), _ => !string.IsNullOrEmpty(CurrentWell.OperatorLogoPath));

            // Setup auto-save timer (500ms debounce)
            _autoSaveTimer = new System.Timers.Timer(500);
            _autoSaveTimer.Elapsed += async (s, e) => await AutoSaveAsync();
            _autoSaveTimer.AutoReset = false;

            // Subscribe to property changes for auto-save
            _currentWell.PropertyChanged += (s, e) => TriggerAutoSave();

            // Establecer CurrentWell.LoadFluid al SelectedFluid inicial
            if (!string.IsNullOrEmpty(SelectedFluid))
                CurrentWell.LoadFluid = SelectedFluid;
        }

        #region Properties

        public Well CurrentWell
        {
            get => _currentWell;
            set
            {
                if (SetProperty(ref _currentWell, value))
                {
                    OnPropertyChanged(nameof(IsSection1Complete));
                    OnPropertyChanged(nameof(IsSection2Complete));
                    OnPropertyChanged(nameof(IsSection3Complete));
                    OnPropertyChanged(nameof(ValidationSummary));

                    _currentWell.PropertyChanged -= (s, e) => TriggerAutoSave();
                    _currentWell.PropertyChanged += (s, e) => TriggerAutoSave();
                }
            }
        }

        private string _lastSavedText = string.Empty;
        public string LastSavedText
        {
            get => _lastSavedText;
            set => SetProperty(ref _lastSavedText, value);
        }

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        #endregion

        #region Dropdown Options

        public ObservableCollection<string> LoadFluid { get; } = new ObservableCollection<string>
        {
            "OBM", "WBM", "SBM", "Brine", "Other"
        };

        private List<FluidData> _allFluidData = new List<FluidData>();
        public ObservableCollection<FluidData> FilteredFluids { get; } = new ObservableCollection<FluidData>();

        private string? _selectedFluid;
        public string SelectedFluid
        {
            get => _selectedFluid;
            set
            {
                if (_selectedFluid != value)
                {
                    _selectedFluid = value;
                    OnPropertyChanged(nameof(SelectedFluid));
                    FilterFluids();
                }
            }
        }

        private FluidData? _selectedFluidStock;
        public FluidData SelectedFluidStock
        {
            get => _selectedFluidStock;
            set
            {
                if (_selectedFluidStock != value)
                {
                    _selectedFluidStock = value;
                    OnPropertyChanged(nameof(SelectedFluidStock));

                    if (CurrentWell != null)
                        CurrentWell.LoadFluidStock = _selectedFluidStock?.Name;
                }
            }
        }

        #endregion

        #region Excel Loading with ClosedXML

        private void LoadFluidsFromExcel()
        {
            try
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ListaFluidos.xlsx");

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Excel no encontrado en: {filePath}");
                    return;
                }

                _allFluidData.Clear();

                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    foreach (var row in worksheet.RowsUsed().Skip(1)) // saltamos encabezado
                    {
                        var fluidSetName = row.Cell(1).GetValue<string>();
                        var baseFluid = row.Cell(2).GetValue<string>();
                        var category = row.Cell(3).GetValue<string>();
                        var system = row.Cell(4).GetValue<string>();
                        var brineType = row.Cell(5).GetValue<string>();

                        System.Diagnostics.Debug.WriteLine($"Fila {row.RowNumber()}: '{fluidSetName}' | '{baseFluid}' | '{category}' | '{system}' | '{brineType}'");

                        if (!string.IsNullOrWhiteSpace(fluidSetName) && !string.IsNullOrWhiteSpace(baseFluid))
                        {
                            _allFluidData.Add(new FluidData
                            {
                                Name = fluidSetName.Trim(),
                                Type = baseFluid.Trim(),
                                Category = category?.Trim(),
                                System = system?.Trim(),
                                BrineType = brineType?.Trim()
                            });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(SelectedFluid))
                    FilterFluids();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando fluidos desde Excel: {ex.Message}");
            }
        }

        private void FilterFluids()
        {
            FilteredFluids.Clear();

            if (!string.IsNullOrEmpty(SelectedFluid))
            {
                var filtered = _allFluidData
                    .Where(f => string.Equals(f.Type?.Trim(), SelectedFluid?.Trim(), StringComparison.OrdinalIgnoreCase));

                foreach (var item in filtered)
                    FilteredFluids.Add(item);
            }

            SelectedFluidStock = null;

            if (CurrentWell != null)
                CurrentWell.LoadFluid = SelectedFluid;
        }





        public ObservableCollection<string> TrajectoryTypes { get; } = new ObservableCollection<string>
        {
            "Vertical", "Deviated", "Directional", "Horizontal", "Multilateral"
        };

        public ObservableCollection<string> WellTypes { get; } = new ObservableCollection<string>
        {
            "Exploration", "Development", "Appraisal", "Re-Entry", "Workover"
        };

        public ObservableCollection<string> RigTypes { get; } = new ObservableCollection<string>
        {
            "Land","Workover","Coiled Tubing"
        };

        public ObservableCollection<string> LocationTypes { get; } = new ObservableCollection<string>
        {
            "Onshore", "Offshore"
        };

        public ObservableCollection<string> Countries { get; } = new ObservableCollection<string>
        {
            "United States", "Canada", "Mexico", "Brazil", "Colombia", "Argentina",
            "Norway", "United Kingdom", "Saudi Arabia", "United Arab Emirates",
            "Nigeria", "Angola", "Australia", "Other"
        };

        public ObservableCollection<string> Basins { get; } = new ObservableCollection<string>
        {
            "Llanos Orientales","Valle Superior del Magdalena","Valle Medio del Magdalena","Valle Inferior del Magdalena",
            "Caguan-Putumayo","Catatumbo","Guajira"
        };

        public List<string> ColombianStates { get; } = new List<string>
        {
            "Amazonas", "Antioquia", "Arauca", "Atlántico",
            "Bolívar", "Boyacá", "Caldas", "Caquetá", "Casanare",
            "Cauca", "Cesar", "Chocó", "Córdoba", "Cundinamarca",
            "Bogotá D.C.", "Guainía", "Guaviare", "Huila",
            "La Guajira", "Magdalena", "Meta", "Nariño",
            "Norte de Santander", "Putumayo", "Quindío", "Risaralda",
            "San Andrés y Providencia", "Santander", "Sucre",
            "Tolima", "Valle del Cauca", "Vaupés", "Vichada"
        };

        /// <summary>
        /// Standardized Present Activity options - grouped by category
        /// </summary>
        public ObservableCollection<string> PresentActivityOptions { get; } = new ObservableCollection<string>
        {
            // Drilling
            "Drilling",
            "Reaming",
            "Underreaming",
            "Directional Drilling",
            // Tripping
            "Tripping In",
            "Tripping Out",
            "Laying Down Pipe",
            "Picking up BHA",
            // Casing & Cement
            "Running Casing",
            "Cementing",
            "WOC (Waiting on Cement)",
            "Nipple Up BOP",
            // Evaluation
            "Wireline Logging",
            "MWD/LWD Survey",
            "Circulating for Samples",
            // Maintenance/Other
            "Rig Repairs",
            "Function Test BOP",
            "Safety Meeting",
            "WOW (Waiting on Weather)"
        };

        /// <summary>
        /// Standardized Well Section options
        /// </summary>
        public ObservableCollection<string> WellSectionOptions { get; } = new ObservableCollection<string>
        {
            "Conductor",
            "Surface",
            "Intermediate 1",
            "Intermediate 2",
            "Production",
            "Liner",
            "Open Hole"
        };

        private void InitializeDropdownOptions()
        {
            // Dropdown options are initialized in property declarations above
        }

        #endregion

        #region Section Completion Properties

        public bool IsSection1Complete =>
            !string.IsNullOrWhiteSpace(CurrentWell.WellName) &&
            !string.IsNullOrWhiteSpace(CurrentWell.Operator) &&
            !string.IsNullOrWhiteSpace(CurrentWell.LoadFluid) &&
            CurrentWell.SpudDate.HasValue;

        public bool IsSection2Complete =>
            !string.IsNullOrWhiteSpace(CurrentWell.Trajectory) &&
            !string.IsNullOrWhiteSpace(CurrentWell.WellType) &&
            !string.IsNullOrWhiteSpace(CurrentWell.RigType);

        public bool IsSection3Complete =>
            !string.IsNullOrWhiteSpace(CurrentWell.Location) &&
            !string.IsNullOrWhiteSpace(CurrentWell.Country);

        public string ValidationSummary
        {
            get
            {
                var missing = CurrentWell.GetMissingRequiredFields();
                if (missing.Count == 0)
                    return "✓ All required fields complete";
                
                return $"⚠ {missing.Count} required field{(missing.Count > 1 ? "s" : "")} incomplete: {string.Join(", ", missing)}";
            }
        }

        #endregion

        #region Commands

        public ICommand SaveAndOpenDashboardCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand UploadLogoCommand { get; }
        public ICommand RemoveLogoCommand { get; }

        public class FluidData
        {
            public string Name { get; set; }        // FLUID SET NAME
            public string Type { get; set; }        // BASE FLUID TYPE
            public string Category { get; set; }    // FLUID CATEGORY
            public string System { get; set; }      // FLUID SYSTEM
            public string BrineType { get; set; }   // BRINE TYPE
        }


        #endregion




        #region Command Implementations

        private async Task SaveAndOpenDashboardAsync()
        {
            try
            {
                IsSaving = true;
                
                // Ensure the well is in the project
                if (!_project.Wells.Contains(CurrentWell))
                {
                    if (CurrentWell.Id == 0)
                    {
                        CurrentWell.Id = _project.Wells.Any() ? _project.Wells.Max(w => w.Id) + 1 : 1;
                    }
                    
                    if (!_project.Wells.Any(w => w.Id == CurrentWell.Id))
                    {
                        _project.AddWell(CurrentWell);
                    }
                }

                

                await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);
                
                _lastSaved = DateTime.Now;
                LastSavedText = $"✓ Saved at {_lastSaved:h:mm:ss tt}";
                
                ToastNotificationService.Instance.ShowSuccess("Well data saved successfully");
                
                // Update Context and navigate to Dashboard
                WellContextService.Instance.CurrentWell = CurrentWell;
                NavigationService.Instance.NavigateToWellDashboard(CurrentWell.Id);
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error saving: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void Cancel()
        {
            // Navigate back to Home Module
            NavigationService.Instance.NavigateToHome();
        }

        private void UploadLogo()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg;*.svg)|*.png;*.jpg;*.jpeg;*.svg|All files (*.*)|*.*",
                    Title = "Select Operator Logo"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    
                    // Validate file size (max 2MB)
                    if (fileInfo.Length > 2 * 1024 * 1024)
                    {
                        ToastNotificationService.Instance.ShowError("Logo file size must be less than 2MB");
                        return;
                    }

                    // In a real app, we'd copy this to a local assets folder
                    // For now, we'll just use the path
                    CurrentWell.OperatorLogoPath = openFileDialog.FileName;
                    
                    OnPropertyChanged(nameof(CurrentWell));
                    ToastNotificationService.Instance.ShowSuccess("Logo uploaded successfully");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error uploading logo: {ex.Message}");
            }
        }

        private void RemoveLogo()
        {
            CurrentWell.OperatorLogoPath = string.Empty;
            OnPropertyChanged(nameof(CurrentWell));
            ToastNotificationService.Instance.ShowInfo("Logo removed");
        }

        #endregion

        #region Auto-Save

        private void TriggerAutoSave()
        {
            if (!_isAutoSaveEnabled) return;

            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Start();

            // Update validation summary
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(IsSection1Complete));
            OnPropertyChanged(nameof(IsSection2Complete));
            OnPropertyChanged(nameof(IsSection3Complete));
        }

        private async Task AutoSaveAsync()
        {
            if (!_isAutoSaveEnabled) return;

            try
            {
                // Ensure the well is in the project before auto-saving
                if (!_project.Wells.Contains(CurrentWell))
                {
                     if (CurrentWell.Id == 0)
                    {
                        CurrentWell.Id = _project.Wells.Any() ? _project.Wells.Max(w => w.Id) + 1 : 1;
                    }
                    
                    if (!_project.Wells.Any(w => w.Id == CurrentWell.Id))
                    {
                        _project.AddWell(CurrentWell);
                    }
                }

                await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);
                
                _lastSaved = DateTime.Now;
                // We need to dispatch this to the UI thread since it's called from a timer
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    LastSavedText = $"✓ Auto-saved at {_lastSaved:h:mm:ss tt}";
                });
            }
            catch (Exception ex)
            {
                // Silent fail for auto-save
                System.Diagnostics.Debug.WriteLine($"Auto-save error: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a well for editing
        /// </summary>
        public void LoadWell(Well well)
        {
            if (well == null) return;

            CurrentWell = well;

            if (!string.IsNullOrEmpty(CurrentWell.LoadFluid))
            {
                SelectedFluid = CurrentWell.LoadFluid; 
            }

            if (!string.IsNullOrEmpty(CurrentWell.LoadFluidStock))
            {
                SelectedFluidStock = FilteredFluids.FirstOrDefault(f => f.Name == CurrentWell.LoadFluidStock);
            }
        }



        /// <summary>
        /// Creates a new well
        /// </summary>
        public void CreateNewWell()
        {
            CurrentWell = new Well
            {
                Status = WellStatus.Draft,
                CreatedDate = DateTime.Now,
                SpudDate = DateTime.Now
            };
            SelectedFluid = LoadFluid.FirstOrDefault();
        }
    


        #endregion

        #region Cleanup

        public void Dispose()
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
        }

        #endregion
    }
}
