using ProjectReport.Models;
using ProjectReport.Modules.VolumeBalance.Data.Additions;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class AdditionsChemicalVolViewModel : INotifyPropertyChanged
    {
        // =====================================================
        // CONSTANTE DE CONVERSIÓN
        // =====================================================

        private const double BBL_CONVERSION = 349.86;


        // =====================================================
        // LIVE COLLECTION
        // Colección compartida en memoria
        // =====================================================

        public ObservableCollection<AdditionsChemicalVol>
            AdditionsChemicalVolumes =>
            AdditionsChemicalService.Instance
                .LiveAdditionsChemicalVolumes;


        // =====================================================
        // PRODUCTOS DEL INVENTARIO
        // =====================================================

        public ObservableCollection<InventoryProduct>
            InventoryProducts
        { get; }
            = new ObservableCollection<InventoryProduct>();


        // =====================================================
        // FLUID COMBINATIONS
        // =====================================================

        public ObservableCollection<VolSystemPit>
            FluidCombinations
        { get; }
            = new ObservableCollection<VolSystemPit>();


        // =====================================================
        // SELECTED FLUID COMBINATION
        // =====================================================

        private VolSystemPit? _selectedFluidCombination;

        public VolSystemPit? SelectedFluidCombination
        {
            get => _selectedFluidCombination;

            set
            {
                if (_selectedFluidCombination == value)
                    return;

                _selectedFluidCombination = value;

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(
                        nameof(SelectedFluidCombination)));
            }
        }


        // =====================================================
        // REPOSITORY
        // =====================================================

        private readonly AdditionsChemicalRepository _repository;


        // =====================================================
        // COMMANDS
        // =====================================================

        public ICommand AddCommand { get; }

        public ICommand DeleteCommand { get; }


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public AdditionsChemicalVolViewModel()
        {
            _repository =
                new AdditionsChemicalRepository(
                    new DatabaseService());

            AddCommand =
                new RelayCommand(AddRow);

            DeleteCommand =
                new RelayCommand(DeleteRow);


            foreach (var product in
                _repository.GetAllProducts())
            {
                InventoryProducts.Add(product);
            }


            //foreach (var fluid in
            //    VolSystemService.Instance.GetCurrentFluids())
            //{
            //    FluidCombinations.Add(fluid);
            //}

            foreach (var row in AdditionsChemicalVolumes)
            {
                row.PropertyChanged +=
                    Row_PropertyChanged;
            }

            AdditionsChemicalVolumes.CollectionChanged +=
                (s, e) =>
                {

                    if (e.NewItems != null)
                    {
                        foreach (
                            AdditionsChemicalVol row
                            in e.NewItems)
                        {
                            row.PropertyChanged +=
                                Row_PropertyChanged;
                        }
                    }

                    if (e.OldItems != null)
                    {
                        foreach (
                            AdditionsChemicalVol row
                            in e.OldItems)
                        {
                            row.PropertyChanged -=
                                Row_PropertyChanged;
                        }
                    }
                };
        }


        // =====================================================
        // ADD ROW
        // =====================================================

        private void AddRow()
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

            var row = new AdditionsChemicalVol
            {
                SelectedProduct = null,

                Cod = "",

                Chemical = "",

                Used = 0,

                FluidType =
                    SelectedFluidCombination.FluidType,

                FluidSubtype =
                    SelectedFluidCombination.FluidSubtype,

                Volume = 0
            };

            row.PropertyChanged +=
                Row_PropertyChanged;

            AdditionsChemicalVolumes.Add(row);
            SelectedFluidCombination = null;
        }


        // =====================================================
        // DELETE ROW
        // =====================================================

        private void DeleteRow(object parameter)
        {

            if (parameter is not AdditionsChemicalVol item)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this chemical addition?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);


            if (result != MessageBoxResult.Yes)
                return;

            item.PropertyChanged -=
                Row_PropertyChanged;

            AdditionsChemicalVolumes.Remove(item);
        }


        // =====================================================
        // CAMBIOS EN UNA FILA
        // =====================================================

        private void Row_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {

            if (sender is not AdditionsChemicalVol row)
                return;

            if (e.PropertyName ==
                nameof(AdditionsChemicalVol.SelectedProduct))
            {
                if (row.SelectedProduct != null)
                {

                    row.Cod =
                        row.SelectedProduct.Code;

                    row.Chemical =
                        row.SelectedProduct.Name;
                }
                else
                {
                    row.Cod = "";

                    row.Chemical = "";
                }

                CalculateVolume(row);
            }

            if (e.PropertyName ==
                nameof(AdditionsChemicalVol.Used))
            {

                CalculateVolume(row);
            }
        }


        // =====================================================
        // CALCULAR VOLUME
        //
        // FORMULA:
        //
        // Volume =
        // Used * PackageQuantity / 349.86 * SG
        //
        // =====================================================

        private void CalculateVolume(
    AdditionsChemicalVol row)
        {
            if (row.SelectedProduct == null)
            {
                row.Volume = 0;

                return;
            }

            double used =
                row.Used;

            double packageQuantity =
                row.SelectedProduct.PackageQuantity;

            double sg =
                row.SelectedProduct.SG ?? 0;

            if (used <= 0 ||
                packageQuantity <= 0 ||
                sg <= 0)
            {
                row.Volume = 0;

                return;
            }

            row.Volume =
                (used * packageQuantity)
                / (sg * BBL_CONVERSION);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}