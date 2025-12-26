using System;
using ProjectReport.Models.Geometry.Wellbore;

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
        /// Para OpenHole: Volume = (ID² / 1029.4) × Length × (1 + Washout/100)
        /// Para Casing/Liner: Volume anular o capacidad interna según contexto.
        /// </summary>
        public void CalculateWellboreComponentVolume(WellboreComponent component, string units, WellboreComponent? previousComponent)
        {
            if (component == null) return;

            double volume = 0;

            if (component.SectionType == WellboreSectionType.OpenHole)
            {
                // OpenHole: Volumen = Diámetro del hoyo con washout
                // Formula: (ID² / 1029.4) × Length × (1 + Washout%)
                // Donde ID = OD (diámetro del hoyo en Open Hole)
                if (component.OD.HasValue && component.OD.Value > 0)
                {
                    double length = (component.BottomMD ?? 0) - (component.TopMD ?? 0);
                    
                    if (length > 0)
                    {
                        double washoutFactor = 1.0 + ((component.Washout ?? 0) / 100.0);
                        double idSquared = Math.Pow(component.OD.Value, 2);
                        volume = (idSquared / FEET_TO_BBL_DIVISOR) * length * washoutFactor;
                    }
                }
            }
            else if (component.SectionType == WellboreSectionType.Casing || 
                     component.SectionType == WellboreSectionType.Liner)
            {
                // Casing/Liner: Volumen anular entre sección anterior e actual
                // Formula: π/4 * (ID_anterior² - OD_actual²) * Length / 1029.4
                if (previousComponent != null && previousComponent.ID.HasValue && 
                    component.OD.HasValue && previousComponent.ID.Value > 0 && component.OD.Value > 0)
                {
                    double length = (component.BottomMD ?? 0) - (component.TopMD ?? 0);
                    
                    if (length > 0)
                    {
                        double idPrev2 = Math.Pow(previousComponent.ID.Value, 2);
                        double odCur2 = Math.Pow(component.OD.Value, 2);
                        volume = (Math.PI / 4.0) * (idPrev2 - odCur2) * length / FEET_TO_BBL_DIVISOR;
                    }
                }
                else if (component.ID.HasValue && component.ID.Value > 0)
                {
                    // No previous component: capacidad interna del string actual
                    double length = (component.BottomMD ?? 0) - (component.TopMD ?? 0);

                    if (length > 0)
                    {
                        double id2 = Math.Pow(component.ID.Value, 2);
                        volume = (id2 / FEET_TO_BBL_DIVISOR) * length;
                    }
                }
            }

            component.Volume = Math.Max(0, volume);
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
        public double GetHydraulicRoughness(WellboreSectionType sectionType)
        {
            return sectionType == WellboreSectionType.OpenHole ? 0.006 : 0.0006;
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
