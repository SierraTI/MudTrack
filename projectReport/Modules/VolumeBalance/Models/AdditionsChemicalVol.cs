using ProjectReport.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class AdditionsChemicalVol : INotifyPropertyChanged
    {
        // =====================================================
        // CAMPOS PRIVADOS
        // =====================================================

        private string _cod = string.Empty;

        private string _chemical = string.Empty;

        private double _used;

        private string _fluidType = string.Empty;

        private string _fluidSubtype = string.Empty;

        private double _volume;

        private InventoryProduct? _selectedProduct;


        // =====================================================
        // COD
        // Se obtiene automáticamente del producto
        // =====================================================

        public string Cod
        {
            get => _cod;

            set
            {
                if (_cod == value)
                    return;

                _cod = value;

                OnPropertyChanged();
            }
        }


        // =====================================================
        // CHEMICAL
        // Se obtiene automáticamente del producto
        // =====================================================

        public string Chemical
        {
            get => _chemical;

            set
            {
                if (_chemical == value)
                    return;

                _chemical = value;

                OnPropertyChanged();
            }
        }


        // =====================================================
        // USED
        // Cantidad utilizada ingresada por el usuario
        // =====================================================

        public double Used
        {
            get => _used;

            set
            {
                if (_used == value)
                    return;

                _used = value;

                OnPropertyChanged();
            }
        }


        // =====================================================
        // FLUID TYPE
        // Viene de la combinación seleccionada
        // =====================================================

        public string FluidType
        {
            get => _fluidType;

            set
            {
                if (_fluidType == value)
                    return;

                _fluidType = value;

                OnPropertyChanged();
            }
        }


        // =====================================================
        // FLUID SUBTYPE
        // Viene de la combinación seleccionada
        // =====================================================

        public string FluidSubtype
        {
            get => _fluidSubtype;

            set
            {
                if (_fluidSubtype == value)
                    return;

                _fluidSubtype = value;

                OnPropertyChanged();
            }
        }


        // =====================================================
        // VOLUME
        // Calculado automáticamente
        // =====================================================

        public double Volume
        {
            get => _volume;

            set
            {
                if (_volume == value)
                    return;

                _volume = value;

                OnPropertyChanged();
            }
        }


        // =====================================================
        // SELECTED PRODUCT
        // Producto seleccionado desde el ComboBox
        // =====================================================

        public InventoryProduct? SelectedProduct
        {
            get => _selectedProduct;

            set
            {
                if (_selectedProduct == value)
                    return;

                _selectedProduct = value;


                // ---------------------------------------------
                // ACTUALIZAR DATOS DEL PRODUCTO
                // ---------------------------------------------

                if (value != null)
                {
                    Cod = value.Code;

                    Chemical = value.Name;
                }
                else
                {
                    Cod = string.Empty;

                    Chemical = string.Empty;
                }


                OnPropertyChanged();
            }
        }


        // =====================================================
        // PROPERTY CHANGED
        // =====================================================

        public event PropertyChangedEventHandler? PropertyChanged;


        // =====================================================
        // NOTIFICAR CAMBIOS
        // =====================================================

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}