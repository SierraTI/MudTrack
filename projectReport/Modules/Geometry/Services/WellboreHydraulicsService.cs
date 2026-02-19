using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Services
{
    /// <summary>
    /// Service for calculating Wellbore Hydraulics Integration
    /// Calculates annular volumes by crossing Wellbore Geometry and Drill String
    /// </summary>
    public class WellboreHydraulicsService
    {
        private const double BBL_CONVERSION_FACTOR = 1029.4; // Conversion factor for bbl/ft using inches

        public List<AnnularVolumeDetail> CalculateAnnularSegments(
            IEnumerable<WellboreComponent> wellboreComponents,
            IEnumerable<DrillStringComponent> drillStringComponents)
        {
            var segments = new List<AnnularVolumeDetail>();

            if (wellboreComponents == null || !wellboreComponents.Any())
                return segments;

            // 1. Unify Nodes: Create a master list of depths from all BottomMD values (and surfaces)
            var depthNodes = new HashSet<double> { 0 }; 
            foreach (var wb in wellboreComponents)
            {
                if (wb.TopMD.HasValue) depthNodes.Add(wb.TopMD.Value);
                if (wb.BottomMD.HasValue) depthNodes.Add(wb.BottomMD.Value);
            }
            foreach (var ds in drillStringComponents)
            {
                if (ds.TopMD.HasValue) depthNodes.Add(ds.TopMD.Value);
                if (ds.BottomMD.HasValue) depthNodes.Add(ds.BottomMD.Value);
            }

            var sortedNodes = depthNodes.Where(d => d >= 0).OrderBy(d => d).ToList();

            // 2. Iteration of Segments: For each pair of nodes (Top, Bottom)
            int segmentId = 1;
            for (int i = 0; i < sortedNodes.Count - 1; i++)
            {
                double segmentTop = sortedNodes[i];
                double segmentBottom = sortedNodes[i + 1];
                double segmentLength = segmentBottom - segmentTop;

                if (segmentLength <= 0.001) continue;

                // Identify the wellbore section (wb) present in this range
                // Rule: If multiple sections overlap (e.g. Casing Override or Conductor/Surface overlap), 
                // we pick the INNERMOST one (the one with the smallest ID).
                var wbCandidates = wellboreComponents.Where(w => 
                    w.TopMD <= segmentTop + 0.001 && w.BottomMD >= segmentBottom - 0.001)
                    .Select(w => new { 
                        Component = w, 
                        EffectiveID = w.Component == ComponentType.OpenHole 
                            ? w.OD.GetValueOrDefault() * Math.Sqrt(1 + (w.Washout.GetValueOrDefault(0) / 100.0))
                            : w.ID.GetValueOrDefault()
                    })
                    .Where(x => x.EffectiveID > 0)
                    .OrderBy(x => x.EffectiveID)
                    .ToList();

                var bestCandidate = wbCandidates.FirstOrDefault();
                if (bestCandidate == null) continue; // No wellbore section here

                var wb = bestCandidate.Component;
                double wbID = bestCandidate.EffectiveID;

                // Identify the drill string component (ds) present in this range
                var ds = drillStringComponents.FirstOrDefault(d => 
                    d.TopMD.HasValue && d.BottomMD.HasValue &&
                    d.TopMD.Value <= segmentTop && d.BottomMD.Value >= segmentBottom);

                double dsOD = ds?.OD ?? 0;

                // Formula: V_ann = (ID_wb² - OD_ds²) / 1029.4 * Length
                double volume = 0;
                if (wbID > dsOD)
                {
                    volume = ((wbID * wbID) - (dsOD * dsOD)) / BBL_CONVERSION_FACTOR * segmentLength;
                }

                segments.Add(new AnnularVolumeDetail
                {
                    Id = segmentId++,
                    Name = ds != null ? $"{ds.ComponentTypeString} in {wb.Name}" : $"Empty {wb.Name}",
                    TopMD = segmentTop,
                    BottomMD = segmentBottom,
                    WellboreID = wbID,
                    DrillStringOD = dsOD,
                    Volume = volume,
                    SectionType = wb.Component?.ToString() ?? string.Empty,
                    Stage = wb.Stage?.ToString() ?? string.Empty,
                    ElementDescription = ds != null ? $"{ds.Name} / {wb.Name}" : wb.Name
                });
            }

            return segments;
        }

        /// <summary>
        /// Calculates total annular volume from segments
        /// </summary>
        public double CalculateTotalAnnularVolume(List<AnnularVolumeDetail> segments)
        {
            return segments.Sum(s => s.Volume);
        }

        /// <summary>
        /// Calculates total strokes to surface based on annular volume and pump displacement
        /// </summary>
        /// <param name="annularVolume">Total annular volume in bbl</param>
        /// <param name="pumpDisplacement">Pump displacement per stroke in bbl/stroke</param>
        /// <returns>Total strokes to circulate from bottom to surface</returns>
        public double CalculateTotalStrokes(double annularVolume, double pumpDisplacement)
        {
            if (pumpDisplacement <= 0)
                return 0;

            return Math.Ceiling(annularVolume / pumpDisplacement);
        }

        /// <summary>
        /// Calculates bottoms up time (time for fluid to travel from bottom to surface)
        /// </summary>
        /// <param name="annularVolume">Total annular volume in bbl</param>
        /// <param name="pumpRate">Pump rate in bbl/min</param>
        /// <returns>Time in minutes</returns>
        public double CalculateBottomsUpTime(double annularVolume, double pumpRate)
        {
            if (pumpRate <= 0)
                return 0;

            return annularVolume / pumpRate;
        }

        /// <summary>
        /// Builds a depth map of drill string components (from surface downward)
        /// </summary>
        private List<(DrillStringComponent Component, double TopMD, double BottomMD)> BuildDrillStringDepthMap(
            IEnumerable<DrillStringComponent> drillStringComponents)
        {
            var depthMap = new List<(DrillStringComponent Component, double TopMD, double BottomMD)>();

            if (drillStringComponents == null || !drillStringComponents.Any())
                return depthMap;

            // Use the TopMD and BottomMD already calculated in the component
            foreach (var component in drillStringComponents)
            {
                if (component.TopMD.HasValue && component.BottomMD.HasValue)
                {
                    depthMap.Add((component, component.TopMD.Value, component.BottomMD.Value));
                }
            }

            return depthMap.OrderBy(d => d.TopMD).ToList();
        }

        /// <summary>
        /// Gets all depth change points from wellbore and drill string
        /// </summary>
        private List<double> GetDepthChangePoints(
            List<WellboreComponent> wellboreComponents,
            List<(DrillStringComponent Component, double TopMD, double BottomMD)> drillStringDepthMap)
        {
            var depthPoints = new HashSet<double> { 0 }; // Always start at surface

            // Add wellbore section boundaries
            foreach (var section in wellboreComponents)
            {
                if (section.TopMD.HasValue)
                    depthPoints.Add(section.TopMD.Value);
                if (section.BottomMD.HasValue)
                    depthPoints.Add(section.BottomMD.Value);
            }

            // Add drill string component boundaries
            foreach (var (_, top, bottom) in drillStringDepthMap)
            {
                depthPoints.Add(top);
                depthPoints.Add(bottom);
            }

            return depthPoints.OrderBy(d => d).ToList();
        }

        /// <summary>
        /// Gets the drill string component at a specific depth range
        /// </summary>
        private DrillStringComponent? GetDrillStringAtDepth(
            List<(DrillStringComponent Component, double TopMD, double BottomMD)> drillStringDepthMap,
            double segmentTop,
            double segmentBottom)
        {
            // Find component that overlaps with this segment
            var component = drillStringDepthMap.FirstOrDefault(d =>
                d.BottomMD > segmentTop && d.TopMD < segmentBottom);

            return component.Component;
        }

        /// <summary>
        /// Calculates annular volume for a single segment
        /// Formula: V_ann (bbl) = (ID_wb² - OD_ds²) / 1029.4 × L
        /// </summary>
        private double CalculateSegmentAnnularVolume(double wellboreID, double drillStringOD, double length)
        {
            if (wellboreID <= 0 || length <= 0)
                return 0;

            // If no drill string, return wellbore capacity
            if (drillStringOD <= 0)
            {
                return (wellboreID * wellboreID / BBL_CONVERSION_FACTOR) * length;
            }

            // Annular volume = (ID² - OD²) / 1029.4 × Length
            double idSquared = wellboreID * wellboreID;
            double odSquared = drillStringOD * drillStringOD;

            if (idSquared <= odSquared)
                return 0; // Invalid: wellbore ID must be greater than drill string OD

            return ((idSquared - odSquared) / BBL_CONVERSION_FACTOR) * length;
        }

        /// <summary>
        /// Builds element description string (e.g., "Drill Pipe / Surface Casing")
        /// </summary>
        private string BuildElementDescription(WellboreComponent wellboreSection, DrillStringComponent? drillStringComponent)
        {
            string wellboreName = wellboreSection.Name ?? wellboreSection.SectionType?.ToString() ?? "Unknown";
            
            if (drillStringComponent == null)
                return wellboreName;

            string drillStringName = drillStringComponent.Name ?? drillStringComponent.ComponentTypeString ?? "Unknown";
            return $"{drillStringName} / {wellboreName}";
        }
    }
}
