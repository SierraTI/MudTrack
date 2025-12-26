using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.ThermalGradient;

namespace ProjectReport.Services
{
    /// <summary>
    /// Service for thermal gradient calculations and interpolation
    /// </summary>
    public class ThermalGradientService
    {
        /// <summary>
        /// Calculates temperature at a given TVD using linear interpolation
        /// </summary>
        /// <param name="points">Thermal gradient points (must be sorted by TVD)</param>
        /// <param name="tvd">True Vertical Depth to calculate temperature at</param>
        /// <returns>Interpolated temperature in °F</returns>
        public double InterpolateTemperature(List<ThermalGradientPoint> points, double tvd)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("No thermal gradient points provided");

            // Sort points by TVD
            var sortedPoints = points.OrderBy(p => p.TVD).ToList();

            // If TVD is at or before first point, return first point temperature
            if (tvd <= sortedPoints.First().TVD)
                return sortedPoints.First().Temperature;

            // If TVD is at or after last point, extrapolate from last two points
            if (tvd >= sortedPoints.Last().TVD)
            {
                if (sortedPoints.Count < 2)
                    return sortedPoints.Last().Temperature;

                // Extrapolate using last two points
                var p1 = sortedPoints[sortedPoints.Count - 2];
                var p2 = sortedPoints[sortedPoints.Count - 1];
                return LinearInterpolation(p1.TVD, p1.Temperature, p2.TVD, p2.Temperature, tvd);
            }

            // Find the two points that bracket the target TVD
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];

                if (tvd >= p1.TVD && tvd <= p2.TVD)
                {
                    return LinearInterpolation(p1.TVD, p1.Temperature, p2.TVD, p2.Temperature, tvd);
                }
            }

            // Should never reach here
            return sortedPoints.Last().Temperature;
        }

        /// <summary>
        /// Linear interpolation formula
        /// T(TVD) = T₁ + [(T₂ - T₁) / (TVD₂ - TVD₁)] × (TVD - TVD₁)
        /// </summary>
        private double LinearInterpolation(double tvd1, double temp1, double tvd2, double temp2, double tvd)
        {
            if (Math.Abs(tvd2 - tvd1) < 0.001) // Avoid division by zero
                return temp1;

            return temp1 + ((temp2 - temp1) / (tvd2 - tvd1)) * (tvd - tvd1);
        }

        /// <summary>
        /// Calculates the geothermal gradient between two points
        /// </summary>
        /// <returns>Gradient in °F per 100 feet</returns>
        public double CalculateGradient(double tvd1, double temp1, double tvd2, double temp2)
        {
            if (Math.Abs(tvd2 - tvd1) < 0.001)
                return 0;

            return ((temp2 - temp1) / (tvd2 - tvd1)) * 100.0;
        }

        /// <summary>
        /// Calculates average geothermal gradient across all points
        /// </summary>
        public double CalculateAverageGradient(List<ThermalGradientPoint> points)
        {
            if (points == null || points.Count < 2)
                return 0;

            var sortedPoints = points.OrderBy(p => p.TVD).ToList();
            var first = sortedPoints.First();
            var last = sortedPoints.Last();

            return CalculateGradient(first.TVD, first.Temperature, last.TVD, last.Temperature);
        }

        /// <summary>
        /// Computes linear regression (slope and intercept) for TVD vs Temperature
        /// Returns slope in °F/ft and intercept in °F
        /// </summary>
        public (double Slope, double Intercept) ComputeLinearRegression(List<ThermalGradientPoint> points)
        {
            if (points == null || points.Count < 2)
                return (0, 0);

            var xs = points.Select(p => p.TVD).ToList();
            var ys = points.Select(p => p.Temperature).ToList();

            double meanX = xs.Average();
            double meanY = ys.Average();

            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < xs.Count; i++)
            {
                double dx = xs[i] - meanX;
                numerator += dx * (ys[i] - meanY);
                denominator += dx * dx;
            }

            if (Math.Abs(denominator) < 1e-9)
                return (0, meanY);

            double slope = numerator / denominator;
            double intercept = meanY - slope * meanX;
            return (slope, intercept);
        }

        /// <summary>
        /// Validates TVD ordering in thermal gradient points
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> ValidateTVDOrdering(List<ThermalGradientPoint> points)
        {
            var errors = new List<string>();

            if (points == null || points.Count == 0)
                return errors;

            var sortedPoints = points.OrderBy(p => p.TVD).ToList();

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].TVD != sortedPoints[i].TVD)
                {
                    errors.Add($"Point ID {points[i].Id}: TVD values are out of order. Expected increasing order.");
                    break; // Only report once
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates that TVD values are within wellbore depth
        /// </summary>
        public List<string> ValidateTVDRange(List<ThermalGradientPoint> points, double maxWellboreTVD)
        {
            var errors = new List<string>();

            if (points == null || points.Count == 0)
                return errors;

            foreach (var point in points)
            {
                if (point.TVD > maxWellboreTVD)
                {
                    errors.Add($"Point ID {point.Id}: TVD ({point.TVD:F2} ft) exceeds total wellbore depth ({maxWellboreTVD:F2} ft)");
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates temperature gradient logic (temperature should generally increase with depth)
        /// </summary>
        public List<string> ValidateTemperatureGradient(List<ThermalGradientPoint> points)
        {
            var warnings = new List<string>();

            if (points == null || points.Count < 2)
                return warnings;

            var sortedPoints = points.OrderBy(p => p.TVD).ToList();

            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];

                if (p2.Temperature < p1.Temperature)
                {
                    warnings.Add($"Error T4: La temperatura está disminuyendo a medida que la TVD aumenta (ID {p2.Id}: {p2.Temperature:F1}°F < {p1.Temperature:F1}°F). Verifique los datos.");
                }
            }

            return warnings;
        }

        /// <summary>
        /// Validates surface temperature reasonableness (TVD = 0)
        /// </summary>
        public string? ValidateSurfaceTemperature(ThermalGradientPoint surfacePoint)
        {
            if (surfacePoint == null) return null;
            if (Math.Abs(surfacePoint.TVD) > 0.001) return null;

            if (surfacePoint.Temperature < 32 || surfacePoint.Temperature > 120)
            {
                return "Advertencia: Temperatura superficial fuera de rango típico (32°F - 120°F).";
            }
            return null;
        }

        /// <summary>
        /// Classifies temperature zones based on temperature ranges
        /// </summary>
        public Dictionary<string, (double StartTVD, double EndTVD)> ClassifyTemperatureZones(List<ThermalGradientPoint> points)
        {
            var zones = new Dictionary<string, (double StartTVD, double EndTVD)>();

            if (points == null || points.Count == 0)
                return zones;

            var sortedPoints = points.OrderBy(p => p.TVD).ToList();

            // Define zone boundaries
            // Cool: < 150°F, Moderate: 150-250°F, Hot: 250-350°F, Very Hot: > 350°F

            double? coolStart = null, coolEnd = null;
            double? moderateStart = null, moderateEnd = null;
            double? hotStart = null, hotEnd = null;
            double? veryHotStart = null, veryHotEnd = null;

            foreach (var point in sortedPoints)
            {
                if (point.Temperature < 150)
                {
                    if (!coolStart.HasValue) coolStart = point.TVD;
                    coolEnd = point.TVD;
                }
                else if (point.Temperature >= 150 && point.Temperature < 250)
                {
                    if (!moderateStart.HasValue) moderateStart = point.TVD;
                    moderateEnd = point.TVD;
                }
                else if (point.Temperature >= 250 && point.Temperature < 350)
                {
                    if (!hotStart.HasValue) hotStart = point.TVD;
                    hotEnd = point.TVD;
                }
                else // >= 350
                {
                    if (!veryHotStart.HasValue) veryHotStart = point.TVD;
                    veryHotEnd = point.TVD;
                }
            }

            if (coolStart.HasValue && coolEnd.HasValue)
                zones["Cool"] = (coolStart.Value, coolEnd.Value);

            if (moderateStart.HasValue && moderateEnd.HasValue)
                zones["Moderate"] = (moderateStart.Value, moderateEnd.Value);

            if (hotStart.HasValue && hotEnd.HasValue)
                zones["Hot"] = (hotStart.Value, hotEnd.Value);

            if (veryHotStart.HasValue && veryHotEnd.HasValue)
                zones["VeryHot"] = (veryHotStart.Value, veryHotEnd.Value);

            return zones;
        }

        /// <summary>
        /// Calculates gradient for each segment between thermal points
        /// </summary>
        /// <returns>List of segment gradients with color coding</returns>
        public List<SegmentGradient> CalculateSegmentGradients(List<ThermalGradientPoint> points)
        {
            var segments = new List<SegmentGradient>();
            
            if (points == null || points.Count < 2)
                return segments;

            var sortedPoints = points.OrderBy(p => p.TVD).ToList();

            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];
                
                var gradient = CalculateGradient(p1.TVD, p1.Temperature, p2.TVD, p2.Temperature);
                
                var segment = new SegmentGradient(
                    p1.TVD,
                    p2.TVD,
                    p1.Temperature,
                    p2.Temperature,
                    gradient
                );
                
                segments.Add(segment);
            }

            return segments;
        }

        /// <summary>
        /// Auto-sorts thermal gradient points by TVD in ascending order
        /// </summary>
        public List<ThermalGradientPoint> SortByTVD(List<ThermalGradientPoint> points)
        {
            if (points == null || points.Count == 0)
                return new List<ThermalGradientPoint>();

            return points.OrderBy(p => p.TVD).ToList();
        }
    }
}
