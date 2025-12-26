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
        /// Valida todos los componentes de drill string.
        /// </summary>
        public List<ValidationError> ValidateDrillString(IEnumerable<DrillStringComponent> components)
        {
            var errors = new List<ValidationError>();
            if (components == null || !components.Any())
                return errors;

            var sortedComponents = components.OrderBy(c => c.Id).ToList();

            // Validaciones de IDs únicos
            ValidateUniqueIds(sortedComponents, errors);

            // Validaciones de cada componente
            foreach (var component in sortedComponents)
            {
                ValidateDiameters(component, errors);
                ValidateLengths(component, errors);
            }

            // Validaciones entre componentes
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
        /// Valida diámetros (OD > ID, valores positivos).
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
                    Message = "OD must be greater than 0",
                    ErrorCode = "D001"
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
                    Message = "ID must be greater than 0",
                    ErrorCode = "D002"
                });
                return;
            }

            // OD debe ser mayor que ID
            if (component.OD.Value <= component.ID.Value)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    Message = $"OD ({component.OD:F3} in) must be greater than ID ({component.ID:F3} in)",
                    ErrorCode = "D003"
                });
            }
        }

        /// <summary>
        /// Valida longitudes.
        /// </summary>
        private void ValidateLengths(DrillStringComponent component, List<ValidationError> errors)
        {
            if (!component.Length.HasValue || component.Length.Value <= 0)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    Message = "Length must be greater than 0",
                    ErrorCode = "L001"
                });
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
