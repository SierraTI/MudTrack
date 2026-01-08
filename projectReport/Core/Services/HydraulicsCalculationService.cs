using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Rig;

namespace ProjectReport.Services
{
    public class HydraulicsCalculationService
    {
        /// <summary>
        /// Calculates the total pressure loss in the surface equipment.
        /// Formula: P = sum( (0.0000765 * rho * L * Q^1.86) / (D^4.86) )
        /// This is a common empirical formula for surface losses.
        /// </summary>
        /// <param name="rigProfile">The rig profile containing surface equipment specs.</param>
        /// <param name="mudDensity">Mud density in ppg.</param>
        /// <param name="flowRate">Flow rate in gpm.</param>
        /// <returns>Total pressure loss in psi.</returns>
        public double CalculateTotalSurfacePressureLoss(RigProfile rigProfile, double mudDensity, double flowRate)
        {
            if (rigProfile?.SurfaceEquipment == null || rigProfile.SurfaceEquipment.Count == 0 || flowRate <= 0)
                return 0;

            double totalLoss = 0;

            foreach (var eq in rigProfile.SurfaceEquipment)
            {
                if (eq.InternalDiameter <= 0 || eq.Length <= 0) continue;

                // Typical formula for mud pressure loss in surface pipes:
                // Pressure Loss (psi) = (0.0000765 * Density * Length * GPM^1.86) / (ID^4.86)
                // Note: constants can vary based on roughness/friction factor.
                
                double frictionAdjustment = eq.FrictionCoefficient > 0 ? eq.FrictionCoefficient : 1.0;
                
                double loss = (0.0000765 * mudDensity * eq.Length * Math.Pow(flowRate, 1.86)) / Math.Pow(eq.InternalDiameter, 4.86);
                
                totalLoss += loss * frictionAdjustment;
            }

            return Math.Round(totalLoss, 2);
        }

        /// <summary>
        /// Calculates the pump constant (gal/stroke) for a triplex pump.
        /// </summary>
        public double CalculateTriplexPumpConstant(double linerSize, double strokeLength)
        {
            // Gal/Stroke = 0.0102 * D^2 * L
            return 0.0102 * Math.Pow(linerSize, 2) * strokeLength;
        }
    }
}
