using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Services;

namespace ProjectReport.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly Project _project;
        private ICollectionView _wellsView;
        private readonly string _projectFilePath;

        public HomeViewModel(Project project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _projectFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project_data.json");
            
            // Initialize collection view for filtering/sorting
            _wellsView = CollectionViewSource.GetDefaultView(_project.Wells);
            _wellsView.Filter = FilterWells;

            // Initialize commands
            NewWellCommand = new RelayCommand(async _ => await CreateNewWellAsync());
            OpenWellCommand = new RelayCommand(OpenWell, CanOpenWell);
            EditWellDataCommand = new RelayCommand(EditWellData, CanEditWell);
            DuplicateWellCommand = new RelayCommand(async p => await DuplicateWellAsync(p), CanDuplicateWell);
            ArchiveWellCommand = new RelayCommand(async p => await ArchiveWellAsync(p), CanArchiveWell);
            DeleteWellCommand = new RelayCommand(async p => await DeleteWellAsync(p), CanDeleteWell);
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            ToggleViewCommand = new RelayCommand(_ => ToggleView());

            // Calculate dashboard statistics
            UpdateDashboardStatistics();

            // Subscribe to collection changes
            _project.Wells.CollectionChanged += (s, e) => UpdateDashboardStatistics();
        }

        #region Properties

        public ICollectionView WellsView => _wellsView;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _wellsView.Refresh();
                }
            }
        }

        private bool _isCardView = true;
        public bool IsCardView
        {
            get => _isCardView;
            set => SetProperty(ref _isCardView, value);
        }

        private string _selectedSortOption = "Last Modified (Newest)";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    ApplySorting();
                }
            }
        }

        public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string>
        {
            "Well Name (A-Z)",
            "Well Name (Z-A)",
            "Last Modified (Newest)",
            "Last Modified (Oldest)",
            "Spud Date (Newest)",
            "Spud Date (Oldest)",
            "Status",
            "Operator (A-Z)"
        };

        // Filter properties
        private ObservableCollection<WellStatus> _selectedStatuses = new ObservableCollection<WellStatus>();
        public ObservableCollection<WellStatus> SelectedStatuses
        {
            get => _selectedStatuses;
            set
            {
                if (SetProperty(ref _selectedStatuses, value))
                {
                    _wellsView.Refresh();
                }
            }
        }

        #endregion

        #region Dashboard Statistics

        private int _totalWells;
        public int TotalWells
        {
            get => _totalWells;
            set => SetProperty(ref _totalWells, value);
        }

        private int _draftWells;
        public int DraftWells
        {
            get => _draftWells;
            set => SetProperty(ref _draftWells, value);
        }

        private int _inProgressWells;
        public int InProgressWells
        {
            get => _inProgressWells;
            set => SetProperty(ref _inProgressWells, value);
        }

        private int _completedWells;
        public int CompletedWells
        {
            get => _completedWells;
            set => SetProperty(ref _completedWells, value);
        }

        private int _archivedWells;
        public int ArchivedWells
        {
            get => _archivedWells;
            set => SetProperty(ref _archivedWells, value);
        }

        private int _activeOperators;
        public int ActiveOperators
        {
            get => _activeOperators;
            set => SetProperty(ref _activeOperators, value);
        }

        private void UpdateDashboardStatistics()
        {
            TotalWells = _project.Wells.Count;
            DraftWells = _project.Wells.Count(w => w.Status == WellStatus.Draft);
            InProgressWells = _project.Wells.Count(w => w.Status == WellStatus.InProgress);
            CompletedWells = _project.Wells.Count(w => w.Status == WellStatus.Completed);
            ArchivedWells = _project.Wells.Count(w => w.Status == WellStatus.Archived);
            ActiveOperators = _project.Wells.Select(w => w.Operator).Distinct().Count();
        }

        #endregion

        #region Commands

        public ICommand NewWellCommand { get; }
        public ICommand OpenWellCommand { get; }
        public ICommand EditWellDataCommand { get; }
        public ICommand DuplicateWellCommand { get; }
        public ICommand ArchiveWellCommand { get; }
        public ICommand DeleteWellCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ToggleViewCommand { get; }

        #endregion

        #region Command Implementations

        private async Task CreateNewWellAsync()
        {
            try
            {
                var newWell = new Well
                {
                    Id = _project.Wells.Count > 0 ? _project.Wells.Max(w => w.Id) + 1 : 1,
                    WellName = $"New Well {_project.Wells.Count + 1}",
                    Status = WellStatus.Draft,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now,
                    SpudDate = DateTime.Now
                };

                _project.AddWell(newWell);
                _project.SetActiveWell(newWell.Id);

                await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);

                // Navigate to Well Data module
                NavigationService.Instance.NavigateToWellData(newWell.Id);
                ToastNotificationService.Instance.ShowSuccess($"Created new well: {newWell.WellName}");
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error creating well: {ex.Message}");
            }
        }

        private void OpenWell(object? parameter)
        {
            if (parameter is Well well)
            {
                _project.SetActiveWell(well.Id);
                
                // Navigate to Well Dashboard
                NavigationService.Instance.NavigateToWellDashboard(well.Id);
                ToastNotificationService.Instance.ShowInfo($"Opening well dashboard: {well.WellName}");
            }
        }

        private bool CanOpenWell(object? parameter)
        {
            return parameter is Well;
        }

        private void EditWellData(object? parameter)
        {
            if (parameter is Well well)
            {
                _project.SetActiveWell(well.Id);
                
                // Navigate to Well Data module
                NavigationService.Instance.NavigateToWellData(well.Id);
                ToastNotificationService.Instance.ShowInfo($"Editing well data: {well.WellName}");
            }
        }

        private bool CanEditWell(object? parameter)
        {
            return parameter is Well;
        }

        private async Task DuplicateWellAsync(object? parameter)
        {
            if (parameter is Well well)
            {
                try
                {
                    var duplicate = well.Duplicate();
                    duplicate.Id = _project.Wells.Count > 0 ? _project.Wells.Max(w => w.Id) + 1 : 1;
                    
                    _project.AddWell(duplicate);
                    
                    await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);

                    ToastNotificationService.Instance.ShowSuccess($"Duplicated well: {duplicate.WellName}");
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Error duplicating well: {ex.Message}");
                }
            }
        }

        private bool CanDuplicateWell(object? parameter)
        {
            return parameter is Well;
        }

        private async Task ArchiveWellAsync(object? parameter)
        {
            if (parameter is Well well)
            {
                try
                {
                    well.Status = WellStatus.Archived;
                    
                    await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);
                    
                    _wellsView.Refresh();
                    UpdateDashboardStatistics();
                    
                    ToastNotificationService.Instance.ShowInfo($"Archived well: {well.WellName}");
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Error archiving well: {ex.Message}");
                }
            }
        }

        private bool CanArchiveWell(object? parameter)
        {
            return parameter is Well well && well.Status != WellStatus.Archived;
        }

        private async Task DeleteWellAsync(object? parameter)
        {
            if (parameter is Well well)
            {
                try
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Are you sure you want to delete {well.WellName}? This action cannot be undone.",
                        "Delete Well",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);

                    if (result == System.Windows.MessageBoxResult.No)
                    {
                        return;
                    }

                    _project.RemoveWell(well.Id);
                    
                    await DataPersistenceService.SaveProjectAsync(_projectFilePath, _project);
                    
                    UpdateDashboardStatistics();
                    
                    ToastNotificationService.Instance.ShowSuccess($"Deleted well: {well.WellName}");
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Error deleting well: {ex.Message}");
                }
            }
        }

        private bool CanDeleteWell(object? parameter)
        {
            return parameter is Well;
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatuses.Clear();
            _wellsView.Refresh();
            
            ToastNotificationService.Instance.ShowInfo("Filters cleared");
        }

        private void ToggleView()
        {
            IsCardView = !IsCardView;
        }

        #endregion

        #region Filtering

        private bool FilterWells(object obj)
        {
            if (obj is not Well well)
                return false;

            // Hide archived wells by default unless specifically filtered
            if (well.Status == WellStatus.Archived && !SelectedStatuses.Contains(WellStatus.Archived))
                return false;

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!well.WellName.ToLower().Contains(searchLower) &&
                    !well.Operator.ToLower().Contains(searchLower) &&
                    !well.Location.ToLower().Contains(searchLower) &&
                    !well.Field.ToLower().Contains(searchLower) &&
                    !well.Basin.ToLower().Contains(searchLower) &&
                    !well.Municipality.ToLower().Contains(searchLower)) 
                {
                    return false;
                }
            }

            // Status filter
            if (SelectedStatuses.Count > 0 && !SelectedStatuses.Contains(well.Status))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Sorting

        private void ApplySorting()
        {
            _wellsView.SortDescriptions.Clear();

            switch (SelectedSortOption)
            {
                case "Well Name (A-Z)":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.WellName), ListSortDirection.Ascending));
                    break;
                case "Well Name (Z-A)":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.WellName), ListSortDirection.Descending));
                    break;
                case "Last Modified (Newest)":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.LastModified), ListSortDirection.Descending));
                    break;
                case "Last Modified (Oldest)":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.LastModified), ListSortDirection.Ascending));
                    break;
                case "Spud Date (Newest)":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.SpudDate), ListSortDirection.Descending));
                    break;
                case "Spud Date (Oldest)":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.SpudDate), ListSortDirection.Ascending));
                    break;
                case "Status":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.Status), ListSortDirection.Ascending));
                    break;
                case "Operator (A-Z)":
                    _wellsView.SortDescriptions.Add(new SortDescription(nameof(Well.Operator), ListSortDirection.Ascending));
                    break;
            }

            _wellsView.Refresh();
        }

        #endregion
    }
}
