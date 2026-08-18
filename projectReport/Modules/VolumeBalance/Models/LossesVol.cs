using ProjectReport.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class LossesVol : INotifyPropertyChanged
    {

        private string _pitSystem = string.Empty;
        private string _fluidType = string.Empty;
        private string _fluidSubtype = string.Empty;


        private VolSystemPit? _selectedPit;

        private LossesType? _selectedLossesType;

        private LossesSubType? _selectedLossesSubType;
        private ObservableCollection<LossesSubType> _filteredLossesSubTypes
    = new();

        private double _volume;



        //=========================
        // PIT SYSTEM COMBO
        //=========================

        public VolSystemPit? SelectedPit
        {
            get => _selectedPit;

            set
            {
                if (SetProperty(ref _selectedPit, value))
                {
                    if (value != null)
                    {
                        //PitSystem = value.PitSystem;
                        FluidType = value.FluidType;
                        FluidSubtype = value.FluidSubtype;
                    }
                }
            }
        }



        //=========================
        // PIT
        //=========================

        public string PitSystem
        {
            get => _pitSystem;

            set => SetProperty(
                ref _pitSystem,
                value);
        }



        //=========================
        // FLUID
        //=========================

        public string FluidType
        {
            get => _fluidType;

            set => SetProperty(
                ref _fluidType,
                value);
        }



        public string FluidSubtype
        {
            get => _fluidSubtype;

            set => SetProperty(
                ref _fluidSubtype,
                value);
        }




        //=========================
        // LOSSES TYPE COMBO
        //=========================

        public LossesType? SelectedLossesType
        {
            get => _selectedLossesType;

            set
            {
                if (SetProperty(ref _selectedLossesType, value))
                {

                    // limpiar subtipo seleccionado
                    SelectedLossesSubType = null;


                    // limpiar lista actual
                    _filteredLossesSubTypes.Clear();


                    // cargar nuevos subtipos
                    if (value != null)
                    {
                        foreach (var subtype in value.SubTypes)
                        {
                            _filteredLossesSubTypes.Add(subtype);
                        }
                    }


                    OnPropertyChanged(
                        nameof(FilteredLossesSubTypes));


                    OnPropertyChanged(
                        nameof(LossesSubTypeDisplay));


                }
            }
        }



        //=========================
        // LOSSES SUBTYPE COMBO
        //=========================

        public LossesSubType? SelectedLossesSubType
        {
            get => _selectedLossesSubType;

            set
            {
                if (SetProperty(
                    ref _selectedLossesSubType,
                    value))
                {

                    OnPropertyChanged(
                        nameof(LossesSubTypeName));

                    OnPropertyChanged(
                        nameof(LossesSubTypeDisplay));

                }
            }
        }




        //=========================
        // SUBTIPOS FILTRADOS
        //=========================

        public ObservableCollection<LossesSubType> FilteredLossesSubTypes
        {
            get => _filteredLossesSubTypes;
        }




        //=========================
        // VOLUME
        //=========================

        public double Volume
        {
            get => _volume;

            set => SetProperty(
                ref _volume,
                value);
        }





        //=========================
        // DISPLAY
        //=========================

        public string FluidDisplay =>
    $"{PitSystem} - {FluidType} - {FluidSubtype}";
        public string LossesTypeName =>
            SelectedLossesType?.Name ?? string.Empty;



        public string LossesSubTypeName =>
            SelectedLossesSubType?.Name ?? string.Empty;

        public string LossesSubTypeDisplay
        {
            get
            {
                if (SelectedLossesType == null)
                    return "Seleccione Losses Type";


                if (FilteredLossesSubTypes.Count == 0)
                    return "Sin subtipos disponibles";


                return SelectedLossesSubType?.Name
                    ?? "Seleccione Losses SubType";
            }
        }


        //=========================
        // NOTIFY
        //=========================

        public event PropertyChangedEventHandler? PropertyChanged;



        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {

            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;


            field = value;


            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));


            return true;

        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

    }
}