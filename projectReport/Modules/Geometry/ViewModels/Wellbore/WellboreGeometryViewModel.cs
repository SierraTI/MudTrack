using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.DrillString;
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
        public ObservableCollection<ComponentType> WellboreSectionTypes { get; }
        public ObservableCollection<WellSectionType> WellSectionTypes { get; }

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
            WellboreSectionTypes = new ObservableCollection<ComponentType>(
                Enum.GetValues(typeof(ComponentType)).Cast<ComponentType>().Where(c => c == ComponentType.Casing || c == ComponentType.Liner || c == ComponentType.OpenHole || c == ComponentType.Riser));
            WellSectionTypes = new ObservableCollection<WellSectionType>(
                Enum.GetValues(typeof(WellSectionType)).Cast<WellSectionType>());

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
                Component = ComponentType.Casing,
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
            // RULE: Sorting - Order by Top MD (Ascending) and then by OD (Descending)
            var sorted = WellboreComponents
                .OrderBy(c => c.TopMD ?? double.MaxValue)
                .ThenByDescending(c => c.OD ?? 0)
                .ToList();
            
            for (int i = 0; i < sorted.Count; i++)
            {
                var component = sorted[i];
                bool isCasingOrLiner = component.Component == ComponentType.Casing || component.Component == ComponentType.Liner;

                // Set History/Active state for Casings/Liners
                // Logic: In any group of overlapping tubulars, the one with the smallest OD is Active (for that interval)
                // However, the rule says "El Casing más ancho pasa a estado History". 
                // So if there is ANY other casing/liner that overlaps this one AND has a smaller OD, this one is History.
                if (isCasingOrLiner)
                {
                    // Logic: The casing with the smallest OD for an interval is the "Active" one.
                    // Wider casings behind it are "History".
                    component.IsHistory = sorted.Any(other => other != component && 
                                                             (other.Component == ComponentType.Casing || other.Component == ComponentType.Liner) &&
                                                             (other.TopMD <= component.TopMD + 0.01 && other.BottomMD >= component.BottomMD - 0.01) &&
                                                             other.OD < component.OD);
                }
                else if (component.Component == ComponentType.Riser)
                {
                    // Riser is active unless replaced
                    component.IsHistory = false;
                }
                else
                {
                    component.IsHistory = false;
                }

                if (i == 0)
                {
                    component.SetAsFirstRow(true);
                }
                else
                {
                    component.SetAsFirstRow(false);
                    
                    // Continuity only forced if NOT a casing/liner/riser that allows start at surface
                    // Or if it's the first of its kind in that depth
                    if (!isCasingOrLiner && component.Component != ComponentType.Riser)
                    {
                        // Special case: If user manually set it to 0 (or it's close to 0), don't force link
                        // unless it's intended to be sequential. 
                        // But for now, let's respect the "Muñecas Rusas" rule.
                        
                        // Find the previous "Active" boundary to link to
                        var previousActive = sorted.Take(i).LastOrDefault(c => !c.IsHistory);
                        if (previousActive != null && component.TopMD > 0.01)
                        {
                            component.SetPreviousBottomMD(previousActive.BottomMD);
                        }
                    }
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
                e.PropertyName == nameof(WellboreComponent.Component) ||
                e.PropertyName == nameof(WellboreComponent.Washout))
            {
                if (sender is WellboreComponent component)
                {
                    if (e.PropertyName == nameof(WellboreComponent.Component) || 
                        e.PropertyName == nameof(WellboreComponent.TopMD) || 
                        e.PropertyName == nameof(WellboreComponent.BottomMD))
                    {
                        HandleComponentLogic(component);
                    }

                    var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
                    int index = sorted.IndexOf(component);
                    var prev = index > 0 ? sorted[index - 1] : null;

                    // If Bottom MD changed, update continuity for following components
                    if (e.PropertyName == nameof(WellboreComponent.BottomMD))
                    {
                        // Update Top MD of next component (if not allowing overlap)
                        if (index >= 0 && index < sorted.Count - 1)
                        {
                            var nextComponent = sorted[index + 1];
                            if (nextComponent.Component != ComponentType.Casing && nextComponent.Component != ComponentType.Liner)
                            {
                                nextComponent.SetPreviousBottomMD(component.BottomMD);
                            }
                        }
                    }

                    _calculationService.CalculateWellboreComponentVolume(component, WellboreComponents);
                    ValidateWellboreComponent(component);
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

            _calculationService.CalculateWellboreComponentVolume(component, WellboreComponents);
            component.ValidateTelescopicDiameter(index > 0 ? sorted[index - 1] : null);
            component.ValidateCasingDepthProgression(index > 0 ? sorted[index - 1] : null);
            CheckForCasingOverwrite(component, index > 0 ? sorted[index - 1] : null);
        }

        private void HandleComponentLogic(WellboreComponent component)
        {
            if (component == null || !component.TopMD.HasValue || !component.BottomMD.HasValue) return;

            // Rule: OPEN HOLE - Overwrite (No Overlap)
            if (component.Component == ComponentType.OpenHole || component.Component == ComponentType.Riser)
            {
                var others = WellboreComponents.Where(c => c != component && 
                                                         (c.Component == ComponentType.OpenHole || c.Component == ComponentType.Riser))
                                               .ToList();

                foreach (var other in others)
                {
                    bool overlaps = component.TopMD < other.BottomMD && component.BottomMD > other.TopMD;
                    if (overlaps)
                    {
                        // Overwrite logic: Adjust or remove previous conflicting hole/riser
                        if (component.TopMD <= other.TopMD && component.BottomMD >= other.BottomMD)
                        {
                            // Completely covered -> remove later to avoid loop issues
                            _isProcessingCollectionChange = true;
                            WellboreComponents.Remove(other);
                            _isProcessingCollectionChange = false;
                        }
                        else if (component.TopMD > other.TopMD && component.BottomMD < other.BottomMD)
                        {
                            // Splits the previous one in two? 
                            // This is complex, usually we just truncate or let user decide.
                            // The user said "overwrite the previous data in that range".
                            other.BottomMD = component.TopMD;
                        }
                        else if (component.TopMD <= other.TopMD && component.BottomMD < other.BottomMD)
                        {
                            // Overlaps top part of previous
                            other.TopMD = component.BottomMD;
                        }
                        else if (component.TopMD > other.TopMD && component.BottomMD >= other.BottomMD)
                        {
                            // Overlaps bottom part of previous
                            other.BottomMD = component.TopMD;
                        }
                    }
                }
            }
            // Rule: CASING - Stacking (Already handled by allowing overlap in validation and sorting)
        }

        private void CheckForCasingOverwrite(WellboreComponent current, WellboreComponent? previous)
        {
            // Already handled by HandleComponentLogic and validation
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
        /// Sincroniza con Report MD desde WellContextService si está disponible.
        /// </summary>
        public void RecalculateTotals()
        {
            // Calculate total volume using the new logic (sum of active sections)
            TotalVolume = _calculationService.CalculateTotalWellboreVolume(WellboreComponents);

            // TotalWellboreMD Logic (Report Sync)
            // Al guardar o actualizar el "Report MD" en la cabecera del reporte,
            // este valor debe pasar al totalWellboreMD del servicio de validación
            double reportMD = WellContextService.Instance.CurrentDepth;
            if (reportMD > 0)
            {
                TotalWellboreMD = reportMD;
            }
            else
            {
                // Fallback: usar el BottomMD de la última sección si no hay Report MD
                if (WellboreComponents.Count == 0)
                {
                    TotalWellboreMD = 0;
                }
                else
                {
                    var sorted = WellboreComponents.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();
                    var lastComponent = sorted.LastOrDefault();
                    TotalWellboreMD = lastComponent?.BottomMD ?? 0;
                }
            }
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
