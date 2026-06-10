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

            // ✅ NUEVO: Propiedad asociada al error
            public string PropertyName { get; set; } = string.Empty;

            public string Message { get; set; } = string.Empty;

            public string ErrorCode { get; set; } = string.Empty;
        }

        /// <summary>
        /// Valida todos los componentes de drill string según las reglas S1-S5.
        /// </summary>
        public List<ValidationError> ValidateDrillString(
            IEnumerable<DrillStringComponent> components,
            double? totalWellboreMD = null)
        {
            var errors = new List<ValidationError>();

            if (components == null || !components.Any())
                return errors;

            var sortedComponents = components
                .OrderBy(c => c.Id)
                .ToList();

            // IDs únicos
            ValidateUniqueIds(sortedComponents, errors);

            // Reglas S1 y S2
            foreach (var component in sortedComponents)
            {
                ValidateDiameters(component, errors); // S1
                ValidateLengths(component, errors);   // S2
            }

            // Regla S3
            if (totalWellboreMD.HasValue)
            {
                ValidateContinuity(
                    sortedComponents,
                    totalWellboreMD.Value,
                    errors);
            }

            // Regla S4
            ValidateBitPosition(sortedComponents, errors);

            // Compatibilidad
            ValidateDrillStringContinuity(sortedComponents, errors);

            return errors;
        }

        // =====================================================
        // VALIDACIONES
        // =====================================================

        private void ValidateUniqueIds(
            List<DrillStringComponent> components,
            List<ValidationError> errors)
        {
            var duplicateIds = components
                .GroupBy(c => c.Id)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicateIds)
            {
                foreach (var component in group)
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = component.Id,
                        ComponentName = component.ComponentType.ToString(),
                        PropertyName = string.Empty,
                        Message = $"Duplicate ID {component.Id} found",
                        ErrorCode = "E001"
                    });
                }
            }
        }

        // =========================
        // S1 - DIÁMETROS
        // =========================
        private void ValidateDiameters(
            DrillStringComponent component,
            List<ValidationError> errors)
        {
            // OD
            if (!component.OD.HasValue || component.OD <= 0)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    PropertyName = nameof(DrillStringComponent.OD),
                    Message = "S1: OD must be greater than 0",
                    ErrorCode = "S1-OD"
                });
                return;
            }

            // ID
            if (!component.ID.HasValue || component.ID <= 0)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    PropertyName = nameof(DrillStringComponent.ID),
                    Message = "S1: ID must be greater than 0",
                    ErrorCode = "S1-ID"
                });
                return;
            }

            // OD > ID
            if (component.OD <= component.ID)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    PropertyName = nameof(DrillStringComponent.OD),
                    Message =
                        $"S1: OD ({component.OD:F3}) must be greater than ID ({component.ID:F3})",
                    ErrorCode = "S1"
                });
            }
        }

        // =========================
        // S2 - LONGITUD
        // =========================
        private void ValidateLengths(
            DrillStringComponent component,
            List<ValidationError> errors)
        {
            if (!component.Length.HasValue || component.Length <= 0)
            {
                errors.Add(new ValidationError
                {
                    ComponentId = component.Id,
                    ComponentName = component.ComponentType.ToString(),
                    PropertyName = nameof(DrillStringComponent.Length),
                    Message = "S2: Length must be greater than 0",
                    ErrorCode = "S2"
                });
            }
        }

        // =========================
        // S3 - CONTINUIDAD
        // =========================
        private void ValidateContinuity(
            List<DrillStringComponent> components,
            double totalWellboreMD,
            List<ValidationError> errors)
        {
            double totalLength =
                components.Sum(c => c.Length.GetValueOrDefault());

            double tolerance = 0.01;

            if (totalLength > totalWellboreMD + tolerance)
            {
                foreach (var component in components)
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = component.Id,
                        ComponentName = component.ComponentType.ToString(),
                        PropertyName = string.Empty,
                        Message =
                            $"Total Length ({totalLength:F2}) exceeds Wellbore MD ({totalWellboreMD:F2})",
                        ErrorCode = "S3"
                    });
                }
            }
        }

        // =========================
        // S4 - BIT
        // =========================
        private void ValidateBitPosition(
            List<DrillStringComponent> components,
            List<ValidationError> errors)
        {
            if (!components.Any()) return;

            var last = components.Last();

            var bits = components
                .Where(c => c.ComponentType == ComponentType.Bit)
                .ToList();

            // Bit último
            if (bits.Any() && last.ComponentType != ComponentType.Bit)
            {
                foreach (var bit in bits)
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = bit.Id,
                        ComponentName = bit.ComponentType.ToString(),
                        PropertyName = nameof(DrillStringComponent.ComponentType),
                        Message = "S4: Bit must be the last component",
                        ErrorCode = "S4"
                    });
                }
            }

            // Solo un bit
            if (bits.Count > 1)
            {
                foreach (var bit in bits.Skip(1))
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = bit.Id,
                        ComponentName = bit.ComponentType.ToString(),
                        PropertyName = nameof(DrillStringComponent.ComponentType),
                        Message = "S4: Only one Bit allowed",
                        ErrorCode = "S4-MULTI"
                    });
                }
            }
        }

        // =========================
        // COMPATIBILIDAD
        // =========================
        private void ValidateDrillStringContinuity(
            List<DrillStringComponent> components,
            List<ValidationError> errors)
        {
            for (int i = 0; i < components.Count - 1; i++)
            {
                var current = components[i];
                var next = components[i + 1];

                if (current.OD.HasValue &&
                    next.OD.HasValue &&
                    current.OD < next.OD)
                {
                    errors.Add(new ValidationError
                    {
                        ComponentId = current.Id,
                        ComponentName =
                            $"{current.ComponentType} → {next.ComponentType}",
                        PropertyName = nameof(DrillStringComponent.OD),
                        Message =
                            $"Next component ({next.OD:F3}) is thicker than current ({current.OD:F3})",
                        ErrorCode = "C001"
                    });
                }
            }
        }

        // =========================
        // CRÍTICOS
        // =========================
        public bool HasCriticalErrors(
            DrillStringComponent component)
        {
            if (!component.OD.HasValue || component.OD <= 0) return true;
            if (!component.ID.HasValue || component.ID <= 0) return true;
            if (!component.Length.HasValue || component.Length <= 0) return true;
            if (component.OD <= component.ID) return true;

            return false;
        }
    }
}
