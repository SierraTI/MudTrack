using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.Services;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class LossesViewModel
    {
        //=========================
        // TABLA LOSSES
        //=========================

        public ObservableCollection<LossesVol> Losses { get; }
            = new();

        //=========================
        // COMBO LOSS TYPE
        //=========================

        public ObservableCollection<LossesType> LossesTypes { get; }
            = new();

        //=========================
        // COMBO FLUID COMBINATIONS
        //=========================

        public ObservableCollection<VolSystemPit> FluidCombinations { get; }
            = new();

        private VolSystemPit? _selectedFluidCombination;

        public VolSystemPit? SelectedFluidCombination
        {
            get => _selectedFluidCombination;
            set => _selectedFluidCombination = value;
        }

        //=========================
        // SERVICES
        //=========================

        private readonly LossesDataService _service;

        private readonly DatabaseService _database;

        //=========================
        // COMMANDS
        //=========================

        public ICommand AddCommand { get; }

        public ICommand DeleteCommand { get; }

        //=========================
        // CONSTRUCTOR
        //=========================

        public LossesViewModel()
        {
            _database = new DatabaseService();

            _service = new LossesDataService(_database);

            AddCommand = new RelayCommand(Add);

            DeleteCommand = new RelayCommand(Delete);

            LoadLossesData();

            LoadCache();
        }

        //=========================
        // LOAD CACHE
        //=========================

        private void LoadCache()
        {
            if (!LossesService.Instance.HasCacheData)
                return;


            foreach (var loss in LossesService.Instance.LiveLosses)
            {

                var savedSubType = loss.SelectedLossesSubType;


                if (loss.SelectedLossesType != null)
                {
                    var type =
                        LossesTypes.FirstOrDefault(x =>
                            x.Id == loss.SelectedLossesType.Id);


                    if (type != null)
                    {
                        loss.SelectedLossesType = type;


                        // Restaurar lista de subtipos
                        loss.FilteredLossesSubTypes.Clear();

                        foreach (var subtype in type.SubTypes)
                        {
                            loss.FilteredLossesSubTypes.Add(subtype);
                        }


                        // Restaurar subtipo seleccionado
                        if (savedSubType != null)
                        {
                            loss.SelectedLossesSubType =
                                loss.FilteredLossesSubTypes
                                .FirstOrDefault(x =>
                                    x.Id == savedSubType.Id);
                        }
                    }
                }


                Losses.Add(loss);
            }
        }

        //=========================
        // LOAD COMBOS
        //=========================

        private void LoadLossesData()
        {
            LossesTypes.Clear();
            FluidCombinations.Clear();

            var types = _service.GetLossesTypes();

            var subtypes = _service.GetLossesSubTypes();

            foreach (var type in types)
            {
                type.SubTypes = subtypes
                    .Where(x => x.LossesTypeId == type.Id)
                    .ToList();

                LossesTypes.Add(type);
            }

            //foreach (var pit in VolSystemService.Instance.GetCurrentFluids())
            //{
            //    FluidCombinations.Add(pit);
            //}
        }

        //=========================
        // ADD ROW
        //=========================

        private void Add()
        {
            if (SelectedFluidCombination == null)
            {
                MessageBox.Show(
                    "Please select a fluid combination.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var loss = new LossesVol
            {
                SelectedPit = SelectedFluidCombination,
                Volume = 0
            };

            Losses.Add(loss);

            LossesService.Instance.LiveLosses.Add(loss);
        }



        //=========================
        // DELETE ROW
        //=========================

        private void Delete(object? obj)
        {
            if (obj is not LossesVol loss)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this loss?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
                return;

            Losses.Remove(loss);

            LossesService.Instance.LiveLosses.Remove(loss);
        }
    }
}