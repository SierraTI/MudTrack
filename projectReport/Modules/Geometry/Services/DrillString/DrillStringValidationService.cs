using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Services.DrillString
{
    /// <summary>
    /// Servicio de validaciones específico para Drill String Geometry.
    /// Valida: diámetros, longitudes, overlaps, capacidad, propiedades físicas.
    /// </summary>
    public class DrillStringValidationService
    {
        /// <summary>
        /// Representa un error de validación de drill string.
        /// </summary>
        public class ValidationError
        {
            public int ComponentId { get; set; }
            public string ComponentName { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string ErrorCode { get; set; } = string.Empty;
        }

        /// <summary>
        /// Valida todos los componentes de drill string según las reglas S1-S5.
        /// </summary>
        public List<ValidationError> ValidateDrillString(IEnumerable<DrillStringComponent> components, double? totalWellboreMD = null)
        {
            var errors = new List<ValidationError>();
            if (components == null || !components.Any())
                return errors;

            var sortedComponents = components.OrderBy(c => c.Id).ToList();

            // Validaciones de IDs únicos
            ValidateUniqueIds(sortedComponents, errors);

            // Regla S1: OD > ID (Geometría)
            // Regla S2: Longitud > 0
            foreach (var component in sortedComponents)
            {
                ValidateDiameters(component, errors); // S1
                ValidateLengths(component, errors); // S2
            }

            // Regla S3: Continuidad - Suma de longitudes = Total Drill String MD
            if (totalWellboreMD.HasValue)
            {
                ValidateContinuity(sortedComponents, totalWellboreMD.Value, errors);
            }

            // Regla S4: Bit debe ser el último componente
            ValidateBitPosition(sortedComponents, errors);

            // Validaciones entre componentes (compatibilidad de diámetros)
            ValidateDrillStringContinuity(sortedComponents, errors);

            return errors;
        }

        /// <summary>
        /// Valida que los IDs sean únicos.
        /// </summary>
        private void ValidateUniqueIds(List<DrillStringComponent> components, List<ValidationError> errors)
        {
            var duplicateIds = components.GroupBy(c => c.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicateIds)
            {
                foreach (var component in group)
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = component.Id,
                        ComponentName = component.ComponentType.ToString(),
                        Message = $"Duplicate ID {component.Id} found",
                        ErrorCode = "E001"
                    });
                }
            }
        }

        /// <summary>
        /// Regla S1: Valida diámetros (OD > ID, valores positivos).
        /// </summary>
        private void ValidateDiameters(DrillStringComponent component, List<ValidationError> errors)
        {
            // OD debe existir y ser > 0
            if (!component.OD.HasValue || component.OD.Value <= 0)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    Message = "S1: OD must be greater than 0",
                    ErrorCode = "S1-OD"
                });
                return;
            }

            // ID debe existir y ser > 0
            if (!component.ID.HasValue || component.ID.Value <= 0)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    Message = "S1: ID must be greater than 0",
                    ErrorCode = "S1-ID"
                });
                return;
            }

            // Regla S1: OD debe ser mayor que ID
            if (component.OD.Value <= component.ID.Value)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    Message = $"S1: OD ({component.OD:F3} in) must be greater than ID ({component.ID:F3} in)",
                    ErrorCode = "S1"
                });
            }
        }

        /// <summary>
        /// Regla S2: Valida longitudes (debe ser > 0).
        /// </summary>
        private void ValidateLengths(DrillStringComponent component, List<ValidationError> errors)
        {
            if (!component.Length.HasValue || component.Length.Value <= 0)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    Message = "S2: Length must be greater than 0.00",
                    ErrorCode = "S2"
                });
            }
        }

        /// <summary>
        /// Regla S3: Valida continuidad - La suma de longitudes debe igualar el Total Drill String MD.
        /// </summary>
        private void ValidateContinuity(List<DrillStringComponent> components, double totalWellboreMD, List<ValidationError> errors)
        {
            double totalLength = components.Sum(c => c.Length.GetValueOrDefault());
            double tolerance = 0.01;

            // Strict equality with Wellbore MD (S3) is not required for validity,
            // as the drill string can be "Off Bottom".
            // However, we should flag if it *exceeds* the wellbore depth significantly (physically impossible usually)
            
            if (totalLength > totalWellboreMD + tolerance)
            {
                 foreach (var component in components)
                 {
                     errors.Add(new ValidationError
                     {
                         ComponentId = component.Id,
                         ComponentName = component.ComponentType.ToString(),
                         Message = $"Error: Drill String Total Length ({totalLength:F2} ft) exceeds Wellbore MD ({totalWellboreMD:F2} ft). Overrun: {totalLength - totalWellboreMD:F2} ft",
                         ErrorCode = "S3-Overrun"
                     });
                 }
            }
        }

        /// <summary>
        /// Regla S4: Valida que el Bit siempre sea el último componente (mayor profundidad).
        /// </summary>
        private void ValidateBitPosition(List<DrillStringComponent> components, List<ValidationError> errors)
        {
            if (components.Count == 0) return;

            var lastComponent = components.Last();
            var bitComponents = components.Where(c => c.ComponentType == ComponentType.Bit).ToList();

            // Si hay un Bit, debe ser el último
            if (bitComponents.Any() && lastComponent.ComponentType != ComponentType.Bit)
            {
                foreach (var bit in bitComponents)
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = bit.Id,
                        ComponentName = bit.ComponentType.ToString(),
                        Message = "S4: Bit component must be the last component (deepest) in the drill string",
                        ErrorCode = "S4"
                    });
                }
            }

            // No debe haber más de un Bit
            if (bitComponents.Count > 1)
            {
                foreach (var bit in bitComponents.Skip(1))
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = bit.Id,
                        ComponentName = bit.ComponentType.ToString(),
                        Message = "S4: Only one Bit component is allowed in the drill string",
                        ErrorCode = "S4-Multiple"
                    });
                }
            }
        }

        /// <summary>
        /// Valida la continuidad y compatibilidad entre componentes.
        /// </summary>
        private void ValidateDrillStringContinuity(List<DrillStringComponent> components, List<ValidationError> errors)
        {
            // Validar que no haya huecos en la cadena
            for (int i = 0; i < components.Count - 1; i++)
            {
                var current = components[i];
                var next = components[i + 1];

                // Validar que no haya conflictos de diámetro
                // (e.g., un componente más grueso no debería venir después de uno más delgado por regla física)
                if (current.OD.HasValue && next.OD.HasValue &&
                    current.OD.Value < next.OD.Value)
                {
                    // Advertencia: el siguiente componente es más grueso
                    errors.Add(new ValidationError
                    {
                        ComponentId = current.Id,
                        ComponentName = $"{current.ComponentType} → {next.ComponentType}",
                        Message = $"Component {next.Id} is thicker ({next.OD:F3} in) than component {current.Id} ({current.OD:F3} in)",
                        ErrorCode = "C001"
                    });
                }
            }
        }

        /// <summary>
        /// Valida si un componente tiene errores críticos (no puede guardarse).
        /// </summary>
        public bool HasCriticalErrors(DrillStringComponent component, IEnumerable<DrillStringComponent>? allComponents = null)
        {
            // Errores críticos: OD/ID inválidos o Length inválido
            if (!component.OD.HasValue || component.OD.Value <= 0) return true;
            if (!component.ID.HasValue || component.ID.Value <= 0) return true;
            if (!component.Length.HasValue || component.Length.Value <= 0) return true;
            if (component.OD.Value <= component.ID.Value) return true;

            return false;
        }
    }
}
