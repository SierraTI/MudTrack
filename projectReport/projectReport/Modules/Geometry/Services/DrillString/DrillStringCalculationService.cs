using System;
using System.Collections.Generic;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Services.DrillString
{
    /// <summary>
    /// Servicio de cálculos específico para Drill String Geometry.
    /// Calcula: volumen, longitud, desplazamiento, y propiedades hidráulicas.
    /// </summary>
    public class DrillStringCalculationService
    {
        private const double FEET_TO_BBL_DIVISOR = 1029.4;

        /// <summary>
        /// Calcula el volumen total de componentes de drill string.
        /// </summary>
        public double CalculateTotalDrillStringVolume(IEnumerable<DrillStringComponent> components, bool useDisplacement)
        {
            if (components == null) return 0;

            double total = 0;
            foreach (var component in components)
            {
                if (useDisplacement)
                {
                    total += component.DisplacementVolume;
                }
                else
                {
                    total += component.InternalVolume;
                }
            }
            return total;
        }

        /// <summary>
        /// Calcula la longitud total de la cadena de perforación.
        /// </summary>
        public double CalculateTotalDrillStringLength(IEnumerable<DrillStringComponent> components)
        {
            if (components == null) return 0;

            double total = 0;
            foreach (var component in components)
            {
                total += component.Length.GetValueOrDefault();
            }
            return total;
        }

        /// <summary>
        /// Calcula el volumen interno (capacidad) de un componente.
        /// Formula: π/4 * ID² * Length / 1029.4
        /// </summary>
        public double CalculateInternalVolume(double? id, double? length)
        {
            if (!id.HasValue || !length.HasValue || id.Value <= 0 || length.Value <= 0)
                return 0;

            return (Math.PI / 4.0) * Math.Pow(id.Value, 2) * length.Value / FEET_TO_BBL_DIVISOR;
        }

        /// <summary>
        /// Calcula el volumen de desplazamiento (diferencia entre OD e ID).
        /// Formula: π/4 * (OD² - ID²) * Length / 1029.4
        /// </summary>
        public double CalculateDisplacementVolume(double? od, double? id, double? length)
        {
            if (!od.HasValue || !id.HasValue || !length.HasValue || od.Value <= 0 || id.Value <= 0 || length.Value <= 0)
                return 0;

            if (od.Value <= id.Value)
                return 0; // OD debe ser mayor que ID

            return (Math.PI / 4.0) * (Math.Pow(od.Value, 2) - Math.Pow(id.Value, 2)) * length.Value / FEET_TO_BBL_DIVISOR;
        }

        /// <summary>
        /// Calcula la rugosidad hidráulica según el tipo de componente.
        /// </summary>
        public double GetHydraulicRoughness(ComponentType componentType)
        {
            return componentType switch
            {
                ComponentType.DrillPipe => 0.0006,
                ComponentType.HWDP => 0.0008,
                ComponentType.DC => 0.001,
                ComponentType.Jar => 0.0005,
                ComponentType.Accelerator => 0.0006,
                _ => 0.0006
            };
        }

        /// <summary>
        /// Calcula la velocidad de flujo dado el caudal.
        /// Formula: Velocity = Flow Rate / Area
        /// </summary>
        public double CalculateFlowVelocity(double flowRate, double? id)
        {
            if (!id.HasValue || id.Value <= 0 || flowRate <= 0)
                return 0;

            // Área interna = π/4 * ID²
            double area = (Math.PI / 4.0) * Math.Pow(id.Value, 2);
            return flowRate / area; // velocity en ft/min (if flowRate is in gpm and area adjusted)
        }
    }
}
