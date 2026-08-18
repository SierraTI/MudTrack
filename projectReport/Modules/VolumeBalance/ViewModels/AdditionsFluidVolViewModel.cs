using ProjectReport.Models;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.Services;
using ProjectReport.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class AdditionsFluidVolViewModel : BaseViewModel
    {
        
        // =========================
        // SERVICES
        // =========================
        private readonly AdditionsFluidService _service = AdditionsFluidService.Instance;
        private readonly VolSystemService _volSystemService = VolSystemService.Instance;
        private readonly WellContextService _context = WellContextService.Instance;

        // =========================
        // LIVE COLLECTION
        // =========================
        public ObservableCollection<AdditionsFluidVol> AdditionsFluidVolumes
            => _service.LiveAdditionsFluidVolumes;

        // =========================
        // FLUID TYPE OPTIONS
        // =========================
        public List<FluidOption> FluidTypeOptions
        {
            get
            {
                var well = _context.CurrentWell;

                if (well == null)
                    return new List<FluidOption>();

                return _volSystemService
                    .GetFluidOptionsForWell(well.Id);
            }
        }

        // =========================
        // COMMANDS
        // =========================
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }

        public AdditionsFluidVolViewModel()
        {
            AddCommand = new RelayCommand(AddRow);
            DeleteCommand = new RelayCommand<AdditionsFluidVol>(DeleteRow);
        }

        // =========================
        // ADD NEW ROW
        // =========================
        private void AddRow()
        {
            AdditionsFluidVolumes.Add(new AdditionsFluidVol
            {
                FluidName = string.Empty,
                Volume = null,
                FluidType = string.Empty,
                Concen = null
            });

            OnPropertyChanged(nameof(AdditionsFluidVolumes));
        }

        private void DeleteRow(AdditionsFluidVol item)
        {
            if (item == null)
                return;

            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to delete this record?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AdditionsFluidVolumes.Remove(item);
            }
        }
    }
}