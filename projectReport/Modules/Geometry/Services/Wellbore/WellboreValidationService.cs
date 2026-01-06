using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Services.Wellbore
{
    /// <summary>
    /// Servicio de validación específico para Wellbore Geometry.
    /// Implementa todas las reglas de validación (categorías A, B, C, D).
    /// 
    /// CATEGORÍAS:
    /// - A: Validaciones de diámetros (OD, ID, telescoping)
    /// - B: Validaciones de profundidad (MD, continuidad, gaps)
    /// - C: Validaciones de tipo de sección (Casing override, OpenHole)
    /// - D: Validaciones de volumen
    /// </summary>
    public class WellboreValidationService
    {
        public enum ValidationSeverity
        {
            Error,      // 🔴 Bloquea guardado
            Warning     // 🟡 Permite guardar con confirmación
        }

        public class ValidationError
        {
            public required string ComponentId { get; set; }
            public required string ComponentName { get; set; }
            public required string Message { get; set; }
            public ValidationSeverity Severity { get; set; }
        }

        public class ValidationResult
        {
            public List<ValidationError> Items { get; set; } = new List<ValidationError>();
            public bool HasCriticalErrors => Items.Any(x => x.Severity == ValidationSeverity.Error);
            public bool HasWarnings => Items.Any(x => x.Severity == ValidationSeverity.Warning);
            public bool IsValid => !HasCriticalErrors;
        }

        /// <summary>
        /// Valida todo el conjunto de componentes de wellbore.
        /// </summary>
        public ValidationResult ValidateWellbore(IEnumerable<WellboreComponent> components, double totalWellboreMD)
        {
            var result = new ValidationResult();
            var list = components.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();

            if (!list.Any())
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = "-",
                    ComponentName = "General",
                    Message = "Debe agregar al menos una sección al wellbore",
                    Severity = ValidationSeverity.Error
                });
                return result;
            }

            // Validar IDs únicos
            ValidateUniqueIds(list, result);

            // Validar secuencia de IDs
            ValidateIdSequence(list, result);

            // Validaciones generales por sección
            for (int i = 0; i < list.Count; i++)
            {
                var cur = list[i];
                var prev = i > 0 ? list[i - 1] : null;

                // Categoría A: Diámetros
                ValidateDiameters(cur, prev, result);

                // Categoría B: Profundidades
                ValidateDepths(cur, prev, totalWellboreMD, result);

                // Categoría C: Tipo de sección
                ValidateSectionType(cur, prev, result);

                // Categoría D: Volumen
                ValidateVolume(cur, result);
            }

            return result;
        }

        /// <summary>
        /// Valida la continuidad de profundidades (Rule BR-WG-002).
        /// </summary>
        public List<string> ValidateWellboreContinuity(IEnumerable<WellboreComponent> components)
        {
            var errors = new List<string>();
            var sorted = components.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var curr = sorted[i];
                var next = sorted[i + 1];

                if (curr.BottomMD.HasValue && next.TopMD.HasValue)
                {
                    if (Math.Abs(curr.BottomMD.Value - next.TopMD.Value) > 0.01)
                    {
                        errors.Add($"Continuity Error: '{curr.Name}' termina en {curr.BottomMD.Value:F2} ft, " +
                                 $"pero '{next.Name}' comienza en {next.TopMD.Value:F2} ft.");
                    }
                }
            }

            return errors;
        }

        #region Validation Methods

        private void ValidateUniqueIds(List<WellboreComponent> list, ValidationResult result)
        {
            var duplicateIds = list.GroupBy(x => x.Id)
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key)
                                   .ToList();

            foreach (var id in duplicateIds)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = "-",
                    ComponentName = "General",
                    Message = $"El ID {id} ya existe. Los IDs deben ser únicos",
                    Severity = ValidationSeverity.Error
                });
            }
        }

        private void ValidateIdSequence(List<WellboreComponent> list, ValidationResult result)
        {
            bool idsAreSequential = true;
            for (int k = 0; k < list.Count; k++)
            {
                if (list[k].Id != k + 1) idsAreSequential = false;
            }

            if (!idsAreSequential)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = "-",
                    ComponentName = "General",
                    Message = "Los IDs no son secuenciales. Se recomienda mantener orden",
                    Severity = ValidationSeverity.Warning
                });
            }
        }

        /// <summary>
        /// CATEGORÍA A: Validaciones de diámetros (A1-A6).
        /// </summary>
        private void ValidateDiameters(WellboreComponent cur, WellboreComponent? prev, ValidationResult result)
        {
            // A5: OD no puede ser cero
            if (cur.OD.GetValueOrDefault() <= 0.001)
            {
                string msg = cur.SectionType == WellboreSectionType.OpenHole
                    ? "Error A5: OD (Hole Diameter) no puede ser 0.000"
                    : "Error A5: OD no puede ser 0.000";
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = msg,
                    Severity = ValidationSeverity.Error
                });
            }

            // A6: ID no puede ser cero (excepto OpenHole)
            if (cur.SectionType != WellboreSectionType.OpenHole && cur.ID.GetValueOrDefault() <= 0.001)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = "Error A6: ID no puede ser 0.000 para secciones tubulares",
                    Severity = ValidationSeverity.Error
                });
            }

            // A1: ID < OD
            if (cur.SectionType != WellboreSectionType.OpenHole && 
                cur.ID.GetValueOrDefault() >= cur.OD.GetValueOrDefault() && 
                cur.OD.GetValueOrDefault() > 0.001)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = "Error A1: ID debe ser menor que OD",
                    Severity = ValidationSeverity.Error
                });
            }

            // A2: Telescopic Diameter (OD[n] < ID[n-1])
            if (prev != null && cur.OD.GetValueOrDefault() >= prev.ID.GetValueOrDefault() && prev.ID.GetValueOrDefault() > 0.001)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = $"Error A2: Progresión telescópica violada. OD ({cur.OD.GetValueOrDefault():F3}) >= ID anterior ({prev.ID.GetValueOrDefault():F3})",
                    Severity = ValidationSeverity.Error
                });
            }
        }

        /// <summary>
        /// CATEGORÍA B: Validaciones de profundidad (B1-B6).
        /// </summary>
        private void ValidateDepths(WellboreComponent cur, WellboreComponent? prev, double totalWellboreMD, ValidationResult result)
        {
            // B1: Bottom > Top
            if (cur.BottomMD.HasValue && cur.TopMD.HasValue && cur.BottomMD.Value <= cur.TopMD.Value)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = "Error B1: Bottom MD debe ser mayor que Top MD",
                    Severity = ValidationSeverity.Error
                });
            }

            // B4: No exceder profundidad total
            if (cur.BottomMD.HasValue && cur.BottomMD.Value > totalWellboreMD + 0.001)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = $"Error B4: Bottom MD ({cur.BottomMD.Value:F2} ft) excede profundidad total ({totalWellboreMD:F2} ft)",
                    Severity = ValidationSeverity.Error
                });
            }

            if (prev != null)
            {
                // B3: No solapamientos
                if (cur.TopMD.HasValue && prev.BottomMD.HasValue && cur.TopMD.Value < prev.BottomMD.Value)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Error B3: Las secciones se solapan",
                        Severity = ValidationSeverity.Error
                    });
                }

                // B2: Detectar gaps
                if (cur.TopMD.HasValue && prev.BottomMD.HasValue && cur.TopMD.Value > prev.BottomMD.Value + 0.01)
                {
                    double gap = cur.TopMD.Value - prev.BottomMD.Value;
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = $"Warning B2: Gap de {gap:F2} ft detectado entre secciones",
                        Severity = ValidationSeverity.Warning
                    });
                }
            }
        }

        /// <summary>
        /// CATEGORÍA C: Validaciones de tipo de sección (C1-C4).
        /// </summary>
        private void ValidateSectionType(WellboreComponent cur, WellboreComponent? prev, ValidationResult result)
        {
            // C1: Casing Depth Progression
            if (prev != null &&
                (cur.SectionType == WellboreSectionType.Casing || cur.SectionType == WellboreSectionType.Liner) &&
                (prev.SectionType == WellboreSectionType.Casing || prev.SectionType == WellboreSectionType.Liner))
            {
                bool isCasingOverride = cur.TopMD.HasValue && prev.TopMD.HasValue &&
                                       Math.Abs(cur.TopMD.Value - prev.TopMD.Value) < 0.01 &&
                                       cur.BottomMD.GetValueOrDefault() >= prev.BottomMD.GetValueOrDefault();

                if (!isCasingOverride && cur.BottomMD.HasValue && prev.BottomMD.HasValue && 
                    cur.BottomMD.Value < prev.BottomMD.Value)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Error C1: Bottom MD de casing no puede disminuir",
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            // C3/C4: OpenHole Washout Requerido
            if (cur.SectionType == WellboreSectionType.OpenHole)
            {
                if (!cur.Washout.HasValue)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Error C4: Washout es requerido para OpenHole",
                        Severity = ValidationSeverity.Error
                    });
                }
                else if (cur.Washout.Value < 0 || cur.Washout.Value > 100)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Error C3: Washout debe estar entre 0% y 100%",
                        Severity = ValidationSeverity.Error
                    });
                }
                else if (cur.Washout.Value > 50)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Warning C3: Washout excesivo (>50%) - verificar medición",
                        Severity = ValidationSeverity.Warning
                    });
                }
            }
        }

        /// <summary>
        /// CATEGORÍA D: Validaciones de volumen (D1-D4).
        /// </summary>
        private void ValidateVolume(WellboreComponent cur, ValidationResult result)
        {
            if (cur.Volume <= 0)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = "Error D1: Volumen debe ser mayor que 0 bbl",
                    Severity = ValidationSeverity.Error
                });
            }

            if (cur.Volume > 100000)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = "Error D4: Volumen indica errores graves en diámetros",
                    Severity = ValidationSeverity.Error
                });
            }
            else if (cur.Volume > 10000)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = "Warning D2: Volumen parece excesivo - verificar diámetros",
                    Severity = ValidationSeverity.Warning
                });
            }
        }

        #endregion
    }
}
