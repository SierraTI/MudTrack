using System;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Services.Wellbore
{
    /// <summary>
    /// Servicio de cálculos específico para Wellbore Geometry.
    /// Calcula: volumen, longitud, propiedades hidráulicas.
    /// </summary>
    public class WellboreCalculationService
    {
        private const double BBL_TO_CUBIC_FEET = 5.615;
        private const double CUBIC_FEET_TO_BBL = 1.0 / 5.615;
        private const double FEET_TO_BBL_DIVISOR = 1029.4;

        /// <summary>
        /// Calcula el volumen de un componente de wellbore.
        /// Para OpenHole: Volume = (OD * (1 + Washout/100))^2 * Length / 1029.4
        /// Para Casing/Liner: Volumen ANULAR con el casing/agujero previo que lo contiene.
        /// </summary>
        public void CalculateWellboreComponentVolume(WellboreComponent component, IEnumerable<WellboreComponent> allComponents)
        {
            if (component == null) return;

            double volume = 0;
            double top = component.TopMD ?? 0;
            double bottom = component.BottomMD ?? 0;
            double length = bottom - top;

            if (length <= 0)
            {
                component.Volume = 0;
                return;
            }

            if (component.Component == ComponentType.OpenHole || component.Component == ComponentType.Riser)
            {
                // OpenHole: Volumen = Diámetro del hoyo con washout
                if (component.OD.HasValue && component.OD.Value > 0)
                {
                    double washoutMultiplier = 1.0 + ((component.Washout ?? 0) / 100.0);
                    double effectiveDiameter = component.OD.Value * washoutMultiplier;
                    volume = (Math.Pow(effectiveDiameter, 2) / FEET_TO_BBL_DIVISOR) * length;
                }
            }
            else if (component.Component == ComponentType.Casing || component.Component == ComponentType.Liner)
            {
                // Casing/Liner: Volumen anular con el componente que lo contiene
                // Si hay múltiples contenedores (ej: Liner que cruza de Casing a Open Hole), 
                // idealmente deberíamos segmentar, pero aquí buscaremos el componente con mayor ID que lo solapa.
                
                var containers = allComponents
                    .Where(c => c != component && 
                               c.TopMD < component.BottomMD && 
                               c.BottomMD > component.TopMD &&
                               c.ID.GetValueOrDefault() > component.OD.GetValueOrDefault())
                    .OrderBy(c => c.ID) // El más cercano es el de menor ID que sea mayor al OD actual
                    .FirstOrDefault();

                if (containers != null)
                {
                    double idPrev2 = Math.Pow(containers.ID.GetValueOrDefault(), 2);
                    double odCur2 = Math.Pow(component.OD.GetValueOrDefault(), 2);
                    // Nota: Se calcula el volumen anular para TODA la longitud del componente actual
                    // asumiendo que el contenedor lo cubre. Para Liners esto suele ser cierto en su tramo superior.
                    volume = (Math.PI / 4.0) * (idPrev2 - odCur2) * length / FEET_TO_BBL_DIVISOR;
                }
                else
                {
                    // No hay contenedor (Casing de superficie): usamos capacidad interna? 
                    // El usuario pide "Volumen para LINER y CASING: Anular con el casing anterior". 
                    // Si no hay anterior, el volumen anular no existe o es con el exterior del riser.
                    // Por ahora, devolver 0 o capacidad si es el primero.
                    if (component.TopMD == 0)
                    {
                        double id2 = Math.Pow(component.ID.GetValueOrDefault(), 2);
                        volume = (id2 / FEET_TO_BBL_DIVISOR) * length;
                    }
                }
            }

            component.Volume = Math.Max(0, volume);
        }

        /// <summary>
        /// Calcula el volumen total del pozo (fluido disponible).
        /// Solo cuenta las secciones "Active" (innermost).
        /// </summary>
        public double CalculateTotalWellboreVolume(IEnumerable<WellboreComponent> components)
        {
            // Sumar solo la capacidad interna de los componentes Activos
            double total = 0;
            foreach (var c in components.Where(c => !c.IsHistory))
            {
                double len = (c.BottomMD ?? 0) - (c.TopMD ?? 0);
                if (len > 0)
                {
                    if (c.Component == ComponentType.OpenHole)
                    {
                         double washoutMultiplier = 1.0 + ((c.Washout ?? 0) / 100.0);
                         double effectiveOD = c.OD.GetValueOrDefault() * washoutMultiplier;
                         total += (Math.Pow(effectiveOD, 2) / FEET_TO_BBL_DIVISOR) * len;
                    }
                    else
                    {
                        total += (Math.Pow(c.ID.GetValueOrDefault(), 2) / FEET_TO_BBL_DIVISOR) * len;
                    }
                }
            }
            return total;
        }

        /// <summary>
        /// Calcula el volumen anular entre dos componentes.
        /// </summary>
        public double CalculateAnnularVolume(WellboreComponent inner, WellboreComponent outer)
        {
            if (inner?.ID == null || outer?.OD == null || inner.ID.Value <= 0 || outer.OD.Value <= 0)
                return 0;

            double length = (inner.BottomMD ?? 0) - 
                          (inner.TopMD ?? 0);
            
            if (length <= 0) return 0;

            double id2 = Math.Pow(inner.ID.Value, 2);
            double od2 = Math.Pow(outer.OD.Value, 2);
            return (Math.PI / 4.0) * (id2 - od2) * length / FEET_TO_BBL_DIVISOR;
        }

        /// <summary>
        /// Calcula la rugosidad hidráulica basada en el tipo de sección.
        /// </summary>
        public double GetHydraulicRoughness(ComponentType sectionType)
        {
            return sectionType == ComponentType.OpenHole ? 0.006 : 0.0006;
        }

        /// <summary>
        /// Obtiene el desplazamiento volumétrico (capacidad interna).
        /// </summary>
        public double GetInternalCapacity(double? id, double length)
        {
            if (!id.HasValue || id.Value <= 0 || length <= 0)
                return 0;

            return (Math.PI / 4.0) * Math.Pow(id.Value, 2) * length / FEET_TO_BBL_DIVISOR;
        }
    }
}
