using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.DrillString;

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
            // RULE: Sorting - Order by Top MD (Ascending) and then by OD (Descending)
            var list = components
                .OrderBy(c => c.TopMD ?? double.MaxValue)
                .ThenByDescending(c => c.OD ?? 0)
                .ToList();

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

            // B5: Primera Sección Comienza en 0.00
            if (list[0].TopMD.GetValueOrDefault() != 0)
            {
                result.Items.Add(new ValidationError 
                { 
                    ComponentId = list[0].Id.ToString(), 
                    ComponentName = list[0].Name, 
                    Message = "La primera sección debe comenzar en 0.00 ft", 
                    Severity = ValidationSeverity.Warning 
                });
            }

            // B6: Total Depth Sync - Última Sección debe coincidir con Report MD
            // Esta validación compara el BottomMD de la última sección contra el Report MD (totalWellboreMD)
            var last = list.Last();
            if (totalWellboreMD > 0)
            {
                if (!last.BottomMD.HasValue)
                {
                    result.Items.Add(new ValidationError 
                    { 
                        ComponentId = last.Id.ToString(), 
                        ComponentName = last.Name, 
                        Message = $"Error B6: La última sección debe tener Bottom MD igual al Report MD ({totalWellboreMD:F2} ft)", 
                        Severity = ValidationSeverity.Error 
                    });
                }
                else if (Math.Abs(last.BottomMD.Value - totalWellboreMD) > 0.01)
                {
                    double difference = Math.Abs(last.BottomMD.Value - totalWellboreMD);
                    result.Items.Add(new ValidationError 
                    { 
                        ComponentId = last.Id.ToString(), 
                        ComponentName = last.Name, 
                        Message = $"Error B6: La última sección termina en {last.BottomMD.Value:F2} ft pero el Report MD es {totalWellboreMD:F2} ft. Diferencia: {difference:F2} ft", 
                        Severity = ValidationSeverity.Error 
                    });
                }
            }

            // Validaciones generales por sección
            for (int i = 0; i < list.Count; i++)
            {
                var cur = list[i];
                var prev = i > 0 ? list[i - 1] : null;

                // Categoría A: Diámetros
                ValidateDiameters(cur, prev, result);

                // Categoría B: Profundidades
                ValidateDepths(cur, prev, totalWellboreMD, result, list);

                // Categoría C: Tipo de sección
                ValidateComponent(cur, prev, result);

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
                string msg = cur.Component == ComponentType.OpenHole
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

            // A6: ID Validation
            // For OpenHole: ID must be exactly 0.000 (no inner pipe)
            if (cur.Component == ComponentType.OpenHole)
            {
                if (cur.ID.GetValueOrDefault() > 0.001)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Error A6: OpenHole debe tener ID = 0.000 (no hay tubería interna)",
                        Severity = ValidationSeverity.Error
                    });
                }
            }
            // For Casing/Liner: ID must be greater than 0
            else if (cur.Component != ComponentType.OpenHole && cur.ID.GetValueOrDefault() <= 0.001)
            {
                result.Items.Add(new ValidationError
                {
                    ComponentId = cur.Id.ToString(),
                    ComponentName = cur.Name,
                    Message = "Error A6: ID cannot be 0.000 for tubular sections (Casing/Liner). An internal diameter is required to calculate annular volume.",
                    Severity = ValidationSeverity.Error
                });
            }

            // A1: ID < OD
            if (cur.Component != ComponentType.OpenHole && 
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
            // Skip this rule if the current section is an OVERRIDE of the previous one
            // Or if it's a CASING/LINER inside another CASING (Stacking allowed)
            bool isOverlap = prev != null && cur.TopMD < prev.BottomMD;
            bool isStackingAllowed = (cur.Component == ComponentType.Casing || cur.Component == ComponentType.Liner) && 
                                    (prev?.Component == ComponentType.Casing || prev?.Component == ComponentType.Liner);

            if (prev != null && !isStackingAllowed && isOverlap && cur.OD.GetValueOrDefault() >= prev.ID.GetValueOrDefault() && prev.ID.GetValueOrDefault() > 0.001)
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
        private void ValidateDepths(WellboreComponent cur, WellboreComponent? prev, double totalWellboreMD, ValidationResult result, List<WellboreComponent> list)
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
                // B3: No solapamientos (regla depende del componente)
                // Componentes que PERMITEN solaparse (Stacking): Casing
                bool allowsOverlap = cur.Component == ComponentType.Casing;
                
                if (!allowsOverlap && cur.TopMD.HasValue && prev.BottomMD.HasValue && cur.TopMD.Value < prev.BottomMD.Value - 0.01)
                {
                    // Excepción para Liner: Permite solapamiento parcial únicamente en la cima (Liner Lap)
                    if (cur.Component == ComponentType.Liner)
                    {
                        // La validación específica del Liner se hace más abajo
                    }
                    else
                    {
                        result.Items.Add(new ValidationError
                        {
                            ComponentId = cur.Id.ToString(),
                            ComponentName = cur.Name,
                            Message = $"Error B3: '{cur.Component}' no permite solapamientos con secciones anteriores.",
                            Severity = ValidationSeverity.Error
                        });
                    }
                }
                
                // Liner specific validation: Top MD must be within a previous Casing
                if (cur.Component == ComponentType.Liner)
                {
                    var previousCasings = list.Take(list.IndexOf(cur))
                        .Where(p => p.Component == ComponentType.Casing)
                        .ToList();

                    var containingCasing = previousCasings.FirstOrDefault(p => cur.TopMD >= p.TopMD && cur.TopMD < p.BottomMD);
                    
                    if (containingCasing == null)
                    {
                        result.Items.Add(new ValidationError
                        {
                            ComponentId = cur.Id.ToString(),
                            ComponentName = cur.Name,
                            Message = "Error Liner: El tope (Top MD) del Liner debe estar dentro de un Casing existente.",
                            Severity = ValidationSeverity.Error
                        });
                    }
                    else
                    {
                        // Check for Lap (300-500 ft overlap with previous casing shoe)
                        double lap = containingCasing.BottomMD.GetValueOrDefault() - cur.TopMD.GetValueOrDefault();
                        if (lap < 100)
                        {
                            result.Items.Add(new ValidationError
                            {
                                ComponentId = cur.Id.ToString(),
                                ComponentName = cur.Name,
                                Message = $"Error Liner Lap: El solapamiento ({lap:F1} ft) es insuficiente. El Liner requiere un Lap físico con el Casing previo.",
                                Severity = ValidationSeverity.Error
                            });
                        }
                        else if (lap < 300 || lap > 600)
                        {
                             result.Items.Add(new ValidationError
                            {
                                ComponentId = cur.Id.ToString(),
                                ComponentName = cur.Name,
                                Message = $"Liner Lap: Solapamiento de {lap:F1} ft. Rango recomendado: 300-500 ft.",
                                Severity = ValidationSeverity.Warning
                            });
                        }
                    }
                }

                // B2: No Gaps - Top MD debe ser igual al Bottom MD de la sección anterior (si no permite solapamientos)
                if (!allowsOverlap && cur.TopMD.HasValue && prev.BottomMD.HasValue)
                {
                    double gap = cur.TopMD.Value - prev.BottomMD.Value;
                    if (Math.Abs(gap) > 0.01)
                    {
                        if (gap > 0)
                        {
                            result.Items.Add(new ValidationError
                            {
                                ComponentId = cur.Id.ToString(),
                                ComponentName = cur.Name,
                                Message = $"Error B2: Gap de {gap:F2} ft detectado. Top MD ({cur.TopMD.Value:F2} ft) debe ser igual al Bottom MD anterior ({prev.BottomMD.Value:F2} ft)",
                                Severity = ValidationSeverity.Error
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// CATEGORÍA C: Validaciones de tipo de sección (C1-C4).
        /// </summary>
        private void ValidateComponent(WellboreComponent cur, WellboreComponent? prev, ValidationResult result)
        {
            bool allowsOverlap = cur.Component == ComponentType.Casing || cur.Component == ComponentType.Liner;

            // C1: Casing Depth Progression (Only for strictly sequential components)
            if (!allowsOverlap && prev != null &&
                (cur.Component == ComponentType.Casing || cur.Component == ComponentType.Liner) &&
                (prev.Component == ComponentType.Casing || prev.Component == ComponentType.Liner))
            {
                if (cur.BottomMD.HasValue && prev.BottomMD.HasValue && 
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

            // C5: Surface Start Rule
            if (cur.TopMD.GetValueOrDefault() < 0.01)
            {
                if (cur.Component == ComponentType.Liner)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Error: Un Liner no puede empezar en superficie (0.00).",
                        Severity = ValidationSeverity.Error
                    });
                }
                else if (cur.Component == ComponentType.OpenHole)
                {
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Warning: Open Hole empezando en superficie es inusual (excepto conductor artesanal).",
                        Severity = ValidationSeverity.Warning
                    });
                }
                // Riser and Casing are allowed at surface (0.00)
            }

            // C3/C4: OpenHole Washout Validation
            if (cur.Component == ComponentType.OpenHole)
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
                else if (Math.Abs(cur.Washout.Value) < 0.01)
                {
                    // C3: Washout of 0.00% is physically unlikely - warning
                    result.Items.Add(new ValidationError
                    {
                        ComponentId = cur.Id.ToString(),
                        ComponentName = cur.Name,
                        Message = "Warning C3: Washout de 0.00% es físicamente improbable. Establezca un valor realista entre 5% y 25%",
                        Severity = ValidationSeverity.Warning
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
