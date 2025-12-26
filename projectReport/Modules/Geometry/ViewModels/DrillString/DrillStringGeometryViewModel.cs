using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Services.DrillString;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Geometry.DrillString
{
    /// <summary>
    /// ViewModel específico para Drill String Geometry.
    /// Gestiona componentes de drill string, validaciones, cálculos y continuidad.
    /// </summary>
    public class DrillStringGeometryViewModel : BaseViewModel
    {
        private readonly DrillStringValidationService _validationService;
        private readonly DrillStringCalculationService _calculationService;
        private int _nextDrillStringId = 1;
        private bool _isProcessingCollectionChange = false;

        public ObservableCollection<DrillStringComponent> DrillStringComponents { get; }
        public ObservableCollection<ComponentType> DrillStringComponentTypes { get; }

        private double _totalDrillStringVolume;
        public double TotalDrillStringVolume
        {
            get => _totalDrillStringVolume;
            set => SetProperty(ref _totalDrillStringVolume, value);
        }

        private double _totalDrillStringLength;
        public double TotalDrillStringLength
        {
            get => _totalDrillStringLength;
            set => SetProperty(ref _totalDrillStringLength, value);
        }

        public DrillStringGeometryViewModel(DrillStringValidationService validationService, DrillStringCalculationService calculationService)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _calculationService = calculationService ?? throw new ArgumentNullException(nameof(calculationService));

            DrillStringComponents = new ObservableCollection<DrillStringComponent>();
            DrillStringComponentTypes = new ObservableCollection<ComponentType>(
                Enum.GetValues(typeof(ComponentType)).Cast<ComponentType>());

            DrillStringComponents.CollectionChanged += OnDrillStringCollectionChanged;
        }

        #region Commands

        public ICommand AddDrillStringComponentCommand => new RelayCommand(AddDrillStringComponent);
        public ICommand DeleteDrillStringComponentCommand => new RelayCommand(DeleteDrillStringComponent);

        #endregion

        #region Add/Delete Operations

        /// <summary>
        /// Agrega un nuevo componente de drill string completamente en blanco.
        /// </summary>
        private void AddDrillStringComponent(object? parameter)
        {
            // Create completely empty component
            var newComponent = new DrillStringComponent
            {
                Id = GetNextDrillStringId(),
                ComponentType = default,  // No default type - user must select
                Length = null,
                OD = null,
                ID = null,
                Name = string.Empty  // Auto-name will be generated after type is selected
            };

            DrillStringComponents.Add(newComponent);
            newComponent.PropertyChanged += OnDrillStringComponentChanged;
            RecalculateTotals();
        }

        /// <summary>
        /// Elimina un componente de drill string.
        /// </summary>
        private void DeleteDrillStringComponent(object? parameter)
        {
            if (parameter is DrillStringComponent component)
            {
                DrillStringComponents.Remove(component);
            }
        }

        #endregion

        #region Collection Management

        private void OnDrillStringCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isProcessingCollectionChange) return;

            if (e.NewItems != null)
            {
                foreach (DrillStringComponent component in e.NewItems)
                {
                    component.PropertyChanged += OnDrillStringComponentChanged;
                    ValidateDrillStringComponent(component);
                }
            }
            if (e.OldItems != null)
            {
                foreach (DrillStringComponent component in e.OldItems)
                {
                    component.PropertyChanged -= OnDrillStringComponentChanged;
                }
                RenumberDrillStringComponents();
            }

            foreach (var component in DrillStringComponents)
            {
                ValidateDrillStringComponent(component);
            }

            RecalculateTotals();
        }

        /// <summary>
        /// Renumera los IDs de los componentes de drill string después de una eliminación.
        /// </summary>
        private void RenumberDrillStringComponents()
        {
            _isProcessingCollectionChange = true;
            try
            {
                int idCounter = 1;
                foreach (var component in DrillStringComponents.OrderBy(c => c.Id))
                {
                    component.Id = idCounter++;
                }
                _nextDrillStringId = idCounter;

                // Auto-rename components to maintain sequence (e.g., "Drill Pipe 1", "Drill Pipe 2")
                DrillStringNamingService.AutoRenameSequence((IList<DrillStringComponent>)DrillStringComponents);
            }
            finally
            {
                _isProcessingCollectionChange = false;
            }
        }

        #endregion

        #region Property Change Handling

        private void OnDrillStringComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrillStringComponent.Length) ||
                e.PropertyName == nameof(DrillStringComponent.OD) ||
                e.PropertyName == nameof(DrillStringComponent.ID) ||
                e.PropertyName == nameof(DrillStringComponent.ComponentType))
            {
                if (sender is DrillStringComponent component)
                {
                    // Auto-generate name when ComponentType is selected
                    if (e.PropertyName == nameof(DrillStringComponent.ComponentType) && component.ComponentType != default)
                    {
                        if (string.IsNullOrEmpty(component.Name))
                        {
                            component.Name = DrillStringNamingService.GenerateComponentName(
                                component.ComponentType,
                                DrillStringComponents
                            );
                        }
                    }

                    ValidateDrillStringComponent(component);
                }

                RecalculateTotals();
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Valida un componente de drill string contra todas las reglas.
        /// </summary>
        private void ValidateDrillStringComponent(DrillStringComponent component)
        {
            if (component == null) return;

            var errors = _validationService.ValidateDrillString(new[] { component });
            // Aplicar errores al componente si es necesario
        }

        /// <summary>
        /// Valida la continuidad del drill string.
        /// </summary>
        public List<string> ValidateDrillStringContinuity()
        {
            var errors = new List<string>();
            var validationErrors = _validationService.ValidateDrillString(DrillStringComponents);

            foreach (var error in validationErrors)
            {
                errors.Add($"{error.ErrorCode}: {error.Message}");
            }

            return errors;
        }

        #endregion

        #region Calculations

        /// <summary>
        /// Recalcula longitud y volumen total de drill string.
        /// </summary>
        public void RecalculateTotals()
        {
            TotalDrillStringLength = _calculationService.CalculateTotalDrillStringLength(DrillStringComponents);
            TotalDrillStringVolume = _calculationService.CalculateTotalDrillStringVolume(DrillStringComponents, false);
        }

        #endregion

        #region Helpers

        private int GetNextDrillStringId()
        {
            return _nextDrillStringId++;
        }

        #endregion
    }
}
