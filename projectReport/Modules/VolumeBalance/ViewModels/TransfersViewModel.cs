using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class TransfersViewModel : INotifyPropertyChanged
    {
        // =========================
        // LISTA DE TRANSFERENCIAS
        // =========================

        public ObservableCollection<TransfersVol> Transfers =>
    TransfersService.Instance.LiveTransfers;

        // =========================
        // OPCIONES DE LOS COMBOS
        // =========================

        public ObservableCollection<TransfersVol.TransferOption> FromOptions { get; }
        = new();

        public ObservableCollection<TransfersVol.TransferOption> ToOptions { get; }
            = new();

        // =========================
        // CAMPOS
        // =========================

        private TransfersVol.TransferOption? _selectedFrom;
        private TransfersVol.TransferOption? _selectedTo;
        private double _transferVolume;
        // =========================
        // COMMANDS
        // =========================

        public ICommand AddTransferCommand { get; }

        public ICommand DeleteTransferCommand { get; }

        // =========================
        // FROM
        // =========================

        public TransfersVol.TransferOption? SelectedFrom
        {
            get => _selectedFrom;
            set
            {
                if (SetProperty(ref _selectedFrom, value))
                {
                    LoadToOptions();
                }
            }
        }

        // =========================
        // TO
        // =========================

        public TransfersVol.TransferOption? SelectedTo
        {
            get => _selectedTo;
            set => SetProperty(ref _selectedTo, value);
        }

        // =========================
        // VOLUMEN
        // =========================

        public double TransferVolume
        {
            get => _transferVolume;
            set => SetProperty(ref _transferVolume, value);
        }

        // =========================
        // CONSTRUCTOR
        // =========================

        public TransfersViewModel()
        {
            AddTransferCommand = new RelayCommand(AddTransfer);
            DeleteTransferCommand = new RelayCommand(DeleteTransfer);

            LoadTransferOptions();


            //VolSystemService.Instance.PitsUpdated += (_, __) =>
            //{
            //    LoadTransferOptions();
            //};
        }

        // =========================
        // CARGAR OPCIONES
        // =========================

        private void LoadTransferOptions()
        {
            FromOptions.Clear();
            ToOptions.Clear();

            // Placeholder FROM
            FromOptions.Add(new TransfersVol.TransferOption
            {
                PitSystem = "Seleccione FROM...",
                IsPlaceholder = true
            });

            // Placeholder TO
            ToOptions.Add(new TransfersVol.TransferOption
            {
                PitSystem = "Seleccione TO...",
                IsPlaceholder = true
            });

            //var pits = VolSystemService.Instance
            //    .GetCurrentTransferPits()
            //    .GroupBy(p => new
            //    {
            //       // p.PitSystem,
            //        p.FluidType,
            //        p.FluidSubtype
            //    })
            //    .Select(g => g.First())
            //    .OrderBy(p => 1)
            //    .ThenBy(p => p.FluidType)
            //    .ThenBy(p => p.FluidSubtype)
            //    .ToList();

            //foreach (var pit in pits)
            //{
            //    FromOptions.Add(new TransfersVol.TransferOption
            //    {
            //        //PitSystem = pit,
            //        FluidType = pit.FluidType,
            //        FluidSubtype = pit.FluidSubtype
            //    });
            //}

            SelectedFrom = FromOptions.FirstOrDefault();
            TransferVolume = 0;
        }

        private void LoadToOptions()
        {
            ToOptions.Clear();

            // Placeholder
            ToOptions.Add(new TransfersVol.TransferOption
            {
                PitSystem = "Seleccione TO...",
                IsPlaceholder = true
            });

            if (SelectedFrom == null || SelectedFrom.IsPlaceholder)
            {
                SelectedTo = ToOptions.FirstOrDefault();
                return;
            }

            //var pits = VolSystemService.Instance
            //    .GetCurrentTransferPits()
            //    .GroupBy(p => new
            //    {
            //        //p.PitSystem,
            //        p.FluidType,
            //        p.FluidSubtype
            //    })
            //    .Select(g => g.First())
            //    .OrderBy(p => 1)
            //    .ThenBy(p => p.FluidType)
            //    .ThenBy(p => p.FluidSubtype);

            //foreach (var pit in pits)
            //{
            //    /*
            //    // No permitir el mismo origen
            //    //if (1== SelectedFrom.PitSystem &&
            //        pit.FluidType == SelectedFrom.FluidType &&
            //        pit.FluidSubtype == SelectedFrom.FluidSubtype)
            //        continue;

            //    ToOptions.Add(new TransfersVol.TransferOption
            //    {
            //        PitSystem = pit.PitSystem,
            //        FluidType = pit.FluidType,
            //        FluidSubtype = pit.FluidSubtype
            //    });
            //    */
            //}

            SelectedTo = ToOptions.FirstOrDefault();

        }

        private void AddTransfer()
        {
            if (SelectedFrom == null || SelectedFrom.IsPlaceholder)
            {
                MessageBox.Show("Please select a FROM option.");
                return;
            }

            if (SelectedTo == null || SelectedTo.IsPlaceholder)
            {
                MessageBox.Show("Please select a TO option.");
                return;
            }

            if (TransferVolume <= 0)
            {
                MessageBox.Show("Volume must be greater than zero.");
                return;
            }

            Transfers.Add(new TransfersVol
            {
                From = SelectedFrom,
                To = SelectedTo,
                Vol = TransferVolume
            });
           
            // Limpiar controles
            SelectedFrom = FromOptions.FirstOrDefault();
            TransferVolume = 0;
        }

        private void DeleteTransfer(object parameter)
        {
            if (parameter is not TransfersVol item)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this transfer?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            Transfers.Remove(item);
       
        }

        // =========================
        // INotifyPropertyChanged
        // =========================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        
    }
}