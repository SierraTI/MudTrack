using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Services
{
    public class GeometryValidationService
    {
        public enum ValidationSeverity
        {
            Error,      // 🔴 Bloquea guardado
            Warning     // 🟡 Permite guardar con confirmación
        }

        public class ValidationError
        {
            public string ComponentId { get; set; } = string.Empty;
            public string ComponentName { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        }

        public class ValidationResult
        {
            public List<ValidationError> Items { get; set; } = new List<ValidationError>();
            public bool HasCriticalErrors => Items.Any(x => x.Severity == ValidationSeverity.Error);
            public bool HasWarnings => Items.Any(x => x.Severity == ValidationSeverity.Warning);
            public bool IsValid => !HasCriticalErrors;
        }

        public ValidationResult ValidateWellbore(IEnumerable<WellboreComponent> components, double totalWellboreMD)
        {
            var result = new ValidationResult();
            var list = components.OrderBy(c => c.TopMD ?? double.MaxValue).ToList();

            // F3: Número Mínimo de Secciones
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

            // F1: IDs Únicos
            var duplicateIds = list.GroupBy(x => x.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateIds.Any())
            {
                foreach (var id in duplicateIds)
                    result.Items.Add(new ValidationError { ComponentId = "-", ComponentName = "General", Message = $"El ID {id} ya existe. Los IDs deben ser únicos", Severity = ValidationSeverity.Error });
            }

            // F2: Secuencia de IDs (Warning)
            bool idsAreSequential = true;
            for (int k = 0; k < list.Count; k++)
            {
                int idVal = list[k].Id;
                if (idVal != k + 1) idsAreSequential = false;
            }
            if (!idsAreSequential)
            {
                result.Items.Add(new ValidationError { ComponentId = "-", ComponentName = "General", Message = "Los IDs no son secuenciales. Se recomienda mantener orden", Severity = ValidationSeverity.Warning });
            }

            // B5: Primera Sección Comienza en 0.00
            if (list[0].TopMD.HasValue && list[0].TopMD.Value != 0)
            {
                result.Items.Add(new ValidationError { ComponentId = list[0].Id.ToString(), ComponentName = list[0].Name, Message = "La primera sección debe comenzar en 0.00 ft", Severity = ValidationSeverity.Warning });
            }

            // B6: Última Sección Termina en Total MD (Warning)
            var last = list.Last();
            if (last.BottomMD.HasValue && Math.Abs(last.BottomMD.Value - totalWellboreMD) > 0.001)
            {
                result.Items.Add(new ValidationError { ComponentId = last.Id.ToString(), ComponentName = last.Name, Message = $"La última sección termina en {last.BottomMD.Value:F2} ft pero el Total Wellbore MD es {totalWellboreMD:F2} ft. ¿Es correcto?", Severity = ValidationSeverity.Warning });
            }

            for (int i = 0; i < list.Count; i++)
            {
                var cur = list[i];
                var prev = i > 0 ? list[i - 1] : null;

                // --- CATEGORÍA A: VALIDACIONES DE DIÁMETROS ---

                if (cur.OD.GetValueOrDefault() <= 0.001)
                {
                    string odMessage = cur.SectionType == WellboreSectionType.OpenHole
                        ? "Error A5: OD cannot be 0.000 (or empty). For OpenHole, enter the Hole Diameter (in)."
                        : "Error A5: OD cannot be 0.000 (or empty). Enter the outer diameter of the pipe.";
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = odMessage, Severity = ValidationSeverity.Error });
                }

                // A3: Rangos Físicos Razonables - OD
                if ((cur.OD < 2.0 || cur.OD > 60.0) && cur.OD > 0.001)
                {
                    string msg = $"OD ({cur.OD:F3} in) está fuera del rango razonable (2.0 - 60.0 in). Verifique el valor ingresado";
                    if (cur.OD > 1000) msg += $"\n¿Quiso decir {cur.OD / 1000:F3} in?";
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = msg, Severity = ValidationSeverity.Error });
                }

                // A6: ID No Puede Ser Cero (Excepto OpenHole)
                if (cur.SectionType != WellboreSectionType.OpenHole && cur.ID.GetValueOrDefault() <= 0.001)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "Error A6: ID cannot be 0.000. Pipe sections must have a valid ID.", Severity = ValidationSeverity.Error });
                }

                // Validar que OpenHole SÍ tenga ID = 0
                if (cur.SectionType == WellboreSectionType.OpenHole && cur.ID.GetValueOrDefault() > 0.001)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"OpenHole debe tener ID = 0.000 (no hay tubería interior). Valor actual: {cur.ID.GetValueOrDefault():F3} in", Severity = ValidationSeverity.Error });
                }

                // A4: Rangos Físicos Razonables - ID
                if ((cur.ID.GetValueOrDefault() < 1.5 || cur.ID.GetValueOrDefault() > 55.0) && cur.ID.GetValueOrDefault() > 0.001)
                {
                    string msg = $"ID ({cur.ID.GetValueOrDefault():F3} in) está fuera del rango razonable (1.5 - 55.0 in). Verifique el valor ingresado";
                    if (cur.ID.GetValueOrDefault() > 1000) msg += $"\n¿Quiso decir {cur.ID.GetValueOrDefault() / 1000:F3} in?";
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = msg, Severity = ValidationSeverity.Error });
                }

                // A1: Internal Diameter Logic - ID must always be smaller than OD
                if (cur.SectionType != WellboreSectionType.OpenHole && cur.ID.GetValueOrDefault() >= cur.OD.GetValueOrDefault() && cur.OD.GetValueOrDefault() > 0.001)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "ID must always be smaller than OD", Severity = ValidationSeverity.Error });
                }

                // A2: Telescopic Diameter Rule
                if (prev != null)
                {
                    if (cur.OD.GetValueOrDefault() >= prev.ID.GetValueOrDefault() && prev.ID.GetValueOrDefault() > 0.001)
                    {
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Error A2: Telescopic progression violated. OD ({cur.OD.GetValueOrDefault():F3}) >= Previous ID ({prev.ID.GetValueOrDefault():F3})", Severity = ValidationSeverity.Error });
                    }
                }

                // --- CATEGORÍA B: VALIDACIONES DE PROFUNDIDAD ---
                if (cur.BottomMD.HasValue && cur.TopMD.HasValue && cur.BottomMD.Value <= cur.TopMD.Value)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Bottom MD ({cur.BottomMD.Value:F2} ft) debe ser mayor que Top MD ({cur.TopMD.Value:F2} ft)", Severity = ValidationSeverity.Error });
                }

                if (cur.BottomMD.HasValue && cur.BottomMD.Value > totalWellboreMD + 0.001)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Bottom MD ({cur.BottomMD.Value:F2} ft) excede la profundidad total del pozo ({totalWellboreMD:F2} ft)", Severity = ValidationSeverity.Error });
                }

                if (prev != null)
                {
                    // B3: Solapamientos
                    if (cur.TopMD.HasValue && prev.BottomMD.HasValue && cur.TopMD.Value < prev.BottomMD.Value)
                    {
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Las secciones se solapan. La sección {cur.Id} comienza en {cur.TopMD.Value:F2} ft pero la sección anterior termina en {prev.BottomMD.Value:F2} ft", Severity = ValidationSeverity.Error });
                    }

                    // B2: Gaps (tolerancia)
                    if (cur.TopMD.HasValue && prev.BottomMD.HasValue && cur.TopMD.Value > prev.BottomMD.Value + 0.01)
                    {
                        double gap = cur.TopMD.Value - prev.BottomMD.Value;
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Gap of {gap:F2} ft detected between Sections. Top MD should equal previous Bottom MD.", Severity = ValidationSeverity.Warning });
                    }
                }

                // --- CATEGORÍA C: TIPO DE SECCIÓN ---
                if (prev != null &&
                    (cur.SectionType == WellboreSectionType.Casing || cur.SectionType == WellboreSectionType.Liner) &&
                    (prev.SectionType == WellboreSectionType.Casing || prev.SectionType == WellboreSectionType.Liner))
                {
                    // Check for Casing Override: Same TopMD, deeper or equal BottomMD (only when values present)
                    bool isCasingOverride = cur.TopMD.HasValue && prev.TopMD.HasValue && Math.Abs(cur.TopMD.Value - prev.TopMD.Value) < 0.01 && cur.BottomMD.GetValueOrDefault() >= prev.BottomMD.GetValueOrDefault();

                    if (isCasingOverride)
                    {
                        result.Items.Add(new ValidationError
                        {
                            ComponentId = cur.Id.ToString(),
                            ComponentName = cur.Name,
                            Message = "⚠ Casing Override detected → previous casing replaced.",
                            Severity = ValidationSeverity.Warning
                        });
                    }
                    else if (cur.BottomMD.HasValue && prev.BottomMD.HasValue && cur.BottomMD.Value < prev.BottomMD.Value)
                    {
                        result.Items.Add(new ValidationError
                        {
                            ComponentId = cur.Id.ToString(),
                            ComponentName = cur.Name,
                            Message = "Error D3: El Bottom MD de una sección de revestimiento anidada no puede ser menor que el Bottom MD de la sección superior inmediata.",
                            Severity = ValidationSeverity.Error
                        });
                    }
                }

                // C3/C4: OpenHole Washout
                if (cur.SectionType == WellboreSectionType.OpenHole)
                {
                    if (!cur.Washout.HasValue)
                    {
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "Washout is required for Open Hole.", Severity = ValidationSeverity.Error });
                    }
                    else if (cur.Washout.GetValueOrDefault() < 0 || cur.Washout.GetValueOrDefault() > 100)
                    {
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "Washout must be between 0% and 100%.", Severity = ValidationSeverity.Error });
                    }
                    else if (cur.Washout.GetValueOrDefault() > 50)
                    {
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "Excessive washout (>50%) detected - verify measurement.", Severity = ValidationSeverity.Warning });
                    }
                    else if (cur.Washout.GetValueOrDefault() > 30)
                    {
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "High washout (>30%) may affect cementing operations.", Severity = ValidationSeverity.Warning });
                    }
                }

                // --- CATEGORÍA D: VOLUMEN ---
                if (cur.Volume <= 0)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "El volumen calculado debe ser mayor que 0 bbl", Severity = ValidationSeverity.Error });
                }

                if (cur.Volume > 100000)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Volumen de {cur.Volume:F2} bbl indica errores graves en diámetros. Revise OD e ID", Severity = ValidationSeverity.Error });
                }
                else if (cur.Volume > 10000)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Volumen de {cur.Volume:F2} bbl parece excesivo. Verifique los diámetros ingresados", Severity = ValidationSeverity.Warning });
                }
            }

            return result;
        }
    }
}