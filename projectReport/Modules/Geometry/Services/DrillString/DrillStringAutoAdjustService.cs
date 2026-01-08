using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Services.DrillString
{
    /// <summary>
    /// Service for automatic drill string adjustment to bit depth.
    /// Implements auto-stretch logic: Length_DP = Bit_Depth - Σ Length_BHA
    /// </summary>
    public class DrillStringAutoAdjustService
    {
        /// <summary>
        /// Calculates required drill pipe length to reach bit depth.
        /// Formula: Length_DP = Bit_Depth - Σ Length_BHA
        /// </summary>
        /// <param name="bitDepth">Current bit depth from Daily Report</param>
        /// <param name="bhaComponents">All BHA components (excluding drill pipe)</param>
        /// <returns>Required drill pipe length in feet</returns>
        public double CalculateDrillPipeLength(double bitDepth, List<DrillStringComponent> bhaComponents)
        {
            if (bitDepth <= 0)
                return 0;

            var bhaLength = bhaComponents.Sum(c => c.Length.GetValueOrDefault());

            var requiredDrillPipeLength = bitDepth - bhaLength;

            return Math.Max(0, requiredDrillPipeLength);
        }

        /// <summary>
        /// Validates that BHA doesn't exceed bit depth (collision detection).
        /// </summary>
        /// <param name="bitDepth">Current bit depth from Daily Report</param>
        /// <param name="bhaComponents">All BHA components (excluding drill pipe)</param>
        /// <returns>Error message if collision detected, null if valid</returns>
        public string? ValidateBHADepth(double bitDepth, List<DrillStringComponent> bhaComponents)
        {
            if (bitDepth <= 0)
                return "Bit depth not set in Daily Report";

            var bhaLength = bhaComponents.Sum(c => c.Length.GetValueOrDefault());

            if (bhaLength > bitDepth)
            {
                return $"⚠️ BHA Collision Alert: BHA length ({bhaLength:F2} ft) exceeds bit depth ({bitDepth:F0} ft). " +
                       $"Reduce BHA component lengths by {(bhaLength - bitDepth):F2} ft.";
            }

            return null;
        }

        /// <summary>
        /// Identifies the drill pipe component (first component with type DrillPipe).
        /// </summary>
        /// <param name="components">All drill string components</param>
        /// <returns>Drill pipe component or null if not found</returns>
        public DrillStringComponent? GetDrillPipeComponent(List<DrillStringComponent> components)
        {
            // Drill pipe is typically the first component
            return components
                .OrderBy(c => c.Id)
                .FirstOrDefault(c => c.ComponentType == ComponentType.DrillPipe);
        }

        /// <summary>
        /// Gets all BHA components (everything except the first drill pipe).
        /// BHA includes: Bit, Motor, Collars, HWDP, Stabilizers, etc.
        /// </summary>
        /// <param name="components">All drill string components</param>
        /// <returns>List of BHA components</returns>
        public List<DrillStringComponent> GetBHAComponents(List<DrillStringComponent> components)
        {
            var drillPipe = GetDrillPipeComponent(components);
            if (drillPipe == null)
                return components.ToList();

            // Return all components except the drill pipe
            return components
                .Where(c => c.Id != drillPipe.Id)
                .ToList();
        }

        /// <summary>
        /// Gets the total length of all BHA components.
        /// </summary>
        public double GetBHATotalLength(List<DrillStringComponent> bhaComponents)
        {
            return bhaComponents.Sum(c => c.Length.GetValueOrDefault());
        }

        /// <summary>
        /// Validates that drill string configuration is suitable for auto-adjustment.
        /// </summary>
        public string? ValidateDrillStringConfiguration(List<DrillStringComponent> components)
        {
            if (components == null || components.Count == 0)
                return "No drill string components defined";

            var drillPipe = GetDrillPipeComponent(components);
            if (drillPipe == null)
                return "No Drill Pipe component found. Add a Drill Pipe as the first component.";

            var bhaComponents = GetBHAComponents(components);
            if (bhaComponents.Count == 0)
                return "No BHA components defined. Add at least a Bit component.";

            // Check if all BHA components have valid lengths
            var invalidComponents = bhaComponents.Where(c => !c.Length.HasValue || c.Length.Value <= 0).ToList();
            if (invalidComponents.Any())
            {
                var names = string.Join(", ", invalidComponents.Select(c => c.Name ?? c.ComponentType.ToString()));
                return $"BHA components missing length: {names}";
            }

            return null;
        }
    }
}
