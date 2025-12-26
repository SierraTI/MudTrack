using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Services;
using ProjectReport.Services.Wellbore;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Geometry.Wellbore
{
    /// <summary>
    /// ViewModel específico para Wellbore Geometry.
    /// Gestiona secciones de wellbore, validaciones, cálculos y continuidad.
    /// </summary>
    public class WellboreGeometryViewModel : BaseViewModel
    {
        private readonly WellboreValidationService _validationService;
        private readonly WellboreCalculationService _calculationService;
        private int _nextWellboreId = 1;
        private bool _isProcessingCollectionChange = false;

        public ObservableCollection<WellboreComponent> WellboreComponents { get; }
        public ObservableCollection<WellboreSectionType> WellboreSectionTypes { get; }

        private double _totalWellboreMD;
        public double TotalWellboreMD
        {
            get => _totalWellboreMD;
            set => SetProperty(ref _totalWellboreMD, value);
        }

        private double _totalVolume;
        public double TotalVolume
        {
            get => _totalVolume;
            set => SetProperty(ref _totalVolume, value);
        }

        public WellboreGeometryViewModel(WellboreValidationService validationService, WellboreCalculationService calculationService)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _calculationService = calculationService ?? throw new ArgumentNullException(nameof(calculationService));

            WellboreComponents = new ObservableCollection<WellboreComponent>();
            WellboreSectionTypes = new ObservableCollection<WellboreSectionType>(
                Enum.GetValues(typeof(WellboreSectionType)).Cast<WellboreSectionType>());

            WellboreComponents.CollectionChanged += OnWellboreCollectionChanged;
        }

        #region Commands

        public ICommand AddWellboreSectionCommand => new RelayCommand(AddWellboreSection);
        public ICommand DeleteWellboreSectionCommand => new RelayCommand(DeleteWellboreSection);

        #endregion

        #region Add/Delete Operations

        /// <summary>
        /// Agrega una nueva sección de wellbore completamente en blanco.
        /// </summary>
        private void AddWellboreSection(object? parameter)
        {
            var newSection = new WellboreComponent
            {
                Id = GetNextWellboreId(),
                Name = string.Empty,
                SectionType = WellboreSectionType.Casing,
                TopMD = null,
                BottomMD = null,
                OD = null,
                ID = null,
                Washout = null
            };

            // If this is the first row, set it as first row (TopMD = 0)
            if (WellboreComponents.Count == 0)
            {
                newSection.SetAsFirstRow(true);
            }
            else
            {
                // If not the first row, auto-link Top MD to previous Bottom MD
                var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
                var lastComponent = sorted.LastOrDefault();
                if (lastComponent != null && lastComponent.BottomMD.HasValue)
                {
                    newSection.SetPreviousBottomMD(lastComponent.BottomMD.Value);
                }
            }

            WellboreComponents.Add(newSection);
            newSection.PropertyChanged += OnWellboreComponentChanged;
            RecalculateTotals();
        }

        /// <summary>
        /// Elimina una sección de wellbore.
        /// </summary>
        private void DeleteWellboreSection(object? parameter)
        {
            if (parameter is WellboreComponent section)
            {
                WellboreComponents.Remove(section);
            }
        }

        #endregion

        #region Collection Management

        private void OnWellboreCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isProcessingCollectionChange) return;

            if (e.NewItems != null)
            {
                foreach (WellboreComponent component in e.NewItems)
                {
                    component.PropertyChanged += OnWellboreComponentChanged;
                    ValidateWellboreComponent(component);
                }
            }
            if (e.OldItems != null)
            {
                foreach (WellboreComponent component in e.OldItems)
                {
                    component.PropertyChanged -= OnWellboreComponentChanged;
                }
                RenumberWellboreSections();
            }

            // Update continuity for all components
            UpdateWellboreContinuity();

            foreach (var component in WellboreComponents)
            {
                ValidateWellboreComponent(component);
            }

            RecalculateTotals();
        }

        /// <summary>
        /// Actualiza la continuidad de Top MD para todas las secciones.
        /// Primera fila: TopMD = 0
        /// Filas posteriores: TopMD = BottomMD anterior
        /// </summary>
        private void UpdateWellboreContinuity()
        {
            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i == 0)
                {
                    sorted[i].SetAsFirstRow(true);
                }
                else
                {
                    sorted[i].SetAsFirstRow(false);
                    var previousComponent = sorted[i - 1];
                    sorted[i].SetPreviousBottomMD(previousComponent.BottomMD);
                }
            }
        }

        /// <summary>
        /// Renumera los IDs de las secciones de wellbore después de una eliminación.
        /// </summary>
        private void RenumberWellboreSections()
        {
            _isProcessingCollectionChange = true;
            try
            {
                int idCounter = 1;
                foreach (var component in WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue))
                {
                    component.Id = idCounter++;
                }
                _nextWellboreId = idCounter;
            }
            finally
            {
                _isProcessingCollectionChange = false;
            }
        }

        #endregion

        #region Property Change Handling

        private void OnWellboreComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WellboreComponent.TopMD) || 
                e.PropertyName == nameof(WellboreComponent.BottomMD) ||
                e.PropertyName == nameof(WellboreComponent.ID) ||
                e.PropertyName == nameof(WellboreComponent.OD) ||
                e.PropertyName == nameof(WellboreComponent.SectionType) ||
                e.PropertyName == nameof(WellboreComponent.Washout))
            {
                if (sender is WellboreComponent component)
                {
                    var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
                    int index = sorted.IndexOf(component);
                    var prev = index > 0 ? sorted[index - 1] : null;

                    // If Bottom MD changed, update continuity for following components
                    if (e.PropertyName == nameof(WellboreComponent.BottomMD))
                    {
                        // Update Top MD of next component
                        if (index >= 0 && index < sorted.Count - 1)
                        {
                            var nextComponent = sorted[index + 1];
                            nextComponent.SetPreviousBottomMD(component.BottomMD);
                        }
                    }

                    _calculationService.CalculateWellboreComponentVolume(component, "Imperial", prev);
                    ValidateWellboreComponent(component);
                    CheckForCasingOverwrite(component, prev);
                }
                RecalculateTotals();
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Valida un componente de wellbore contra todas las reglas.
        /// </summary>
        private void ValidateWellboreComponent(WellboreComponent component)
        {
            if (component == null) return;

            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            int index = sorted.IndexOf(component);

            if (index < 0) return;

            var previousComponent = index > 0 ? sorted[index - 1] : null;

            _calculationService.CalculateWellboreComponentVolume(component, "Imperial", previousComponent);
            component.ValidateTelescopicDiameter(previousComponent);
            component.ValidateCasingDepthProgression(previousComponent);
            CheckForCasingOverwrite(component, previousComponent);
        }

        private void CheckForCasingOverwrite(WellboreComponent current, WellboreComponent? previous)
        {
            if (previous != null && 
                (current.SectionType == WellboreSectionType.Casing || current.SectionType == WellboreSectionType.Liner) &&
                (previous.SectionType == WellboreSectionType.Casing || previous.SectionType == WellboreSectionType.Liner))
            {
                bool isSameType = current.SectionType == previous.SectionType;
                bool isSameOD = Math.Abs(current.OD.GetValueOrDefault() - previous.OD.GetValueOrDefault()) < 0.001;
                bool isSameTop = current.TopMD.HasValue && previous.TopMD.HasValue && 
                                 Math.Abs(current.TopMD.Value - previous.TopMD.Value) < 0.01;
                bool isExtension = current.BottomMD.GetValueOrDefault() > previous.BottomMD.GetValueOrDefault();

                if (isSameType && isSameOD && isSameTop && isExtension)
                {
                    // Lógica de overwrite detectada
                }
            }
        }

        /// <summary>
        /// Valida la continuidad de profundidades entre secciones.
        /// </summary>
        public List<string> ValidateWellboreContinuity()
        {
            return _validationService.ValidateWellboreContinuity(WellboreComponents);
        }

        #endregion

        #region Calculations

        /// <summary>
        /// Recalcula el MD total del wellbore y volumen total basado en las secciones.
        /// </summary>
        public void RecalculateTotals()
        {
            if (WellboreComponents.Count == 0)
            {
                TotalWellboreMD = 0;
                TotalVolume = 0;
                return;
            }

            var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
            var lastComponent = sorted.LastOrDefault();

            if (lastComponent != null && lastComponent.BottomMD.HasValue)
            {
                TotalWellboreMD = lastComponent.BottomMD.Value;
            }
            else
            {
                TotalWellboreMD = 0;
            }

            // Calculate total volume
            TotalVolume = WellboreComponents.Sum(c => c.Volume);
        }

        #endregion

        #region Helpers

        private int GetNextWellboreId()
        {
            return _nextWellboreId++;
        }

        #endregion
    }
}
