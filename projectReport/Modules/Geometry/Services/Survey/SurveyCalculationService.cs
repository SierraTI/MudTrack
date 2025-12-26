using System;
using ProjectReport.Models.Geometry.Survey;

namespace ProjectReport.Services.Survey
{
    /// <summary>
    /// Service for calculating wellbore trajectory using industry-standard Minimum Curvature Method.
    /// Computes TVD, Northing, Easting, Vertical Section, Dogleg Severity, Build Rate, and Turn Rate.
    /// </summary>
    public class SurveyCalculationService
    {
        private const double DegreesToRadians = Math.PI / 180.0;
        private const double RadiansToDegrees = 180.0 / Math.PI;

        /// <summary>
        /// Calculates trajectory coordinates for a survey point using Minimum Curvature Method.
        /// Updates TVD, Northing, Easting, VerticalSection, DoglegSeverity, BuildRate, and TurnRate.
        /// </summary>
        /// <param name="current">Current survey point to calculate</param>
        /// <param name="previous">Previous survey point (null for first point)</param>
        public void CalculateTrajectory(SurveyPoint current, SurveyPoint? previous)
        {
            if (current == null) return;

            // First point (tie-in) - typically at surface
            if (previous == null)
            {
                current.SetCalculatedValues(
                    tvd: 0,
                    northing: 0,
                    easting: 0,
                    verticalSection: 0,
                    doglegSeverity: 0,
                    buildRate: 0,
                    turnRate: 0
                );
                return;
            }

            // Calculate delta MD
            double deltaMD = current.MD - previous.MD;
            
            if (deltaMD <= 0)
            {
                // Invalid: MD must increase
                current.SetCalculatedValues(
                    tvd: previous.TVD,
                    northing: previous.Northing,
                    easting: previous.Easting,
                    verticalSection: previous.VerticalSection,
                    doglegSeverity: 0,
                    buildRate: 0,
                    turnRate: 0
                );
                return;
            }

            // Convert angles to radians
            double inc1 = previous.HoleAngle * DegreesToRadians;
            double inc2 = current.HoleAngle * DegreesToRadians;
            double az1 = previous.Azimuth * DegreesToRadians;
            double az2 = current.Azimuth * DegreesToRadians;

            // Calculate dogleg angle (3D angle between survey stations)
            double cosDoglegg = Math.Cos(inc2 - inc1) - Math.Sin(inc1) * Math.Sin(inc2) * (1 - Math.Cos(az2 - az1));
            double doglegAngle = Math.Acos(Math.Max(-1, Math.Min(1, cosDoglegg))); // Clamp to [-1, 1] for numerical stability

            // Calculate Ratio Factor (RF) for Minimum Curvature Method
            double ratioFactor;
            if (Math.Abs(doglegAngle) < 1e-8) // Essentially straight
            {
                ratioFactor = 1.0;
            }
            else
            {
                ratioFactor = (2.0 / doglegAngle) * Math.Tan(doglegAngle / 2.0);
            }

            // Calculate incremental changes using Minimum Curvature
            double deltaNorth = (deltaMD / 2.0) * (Math.Sin(inc1) * Math.Cos(az1) + Math.Sin(inc2) * Math.Cos(az2)) * ratioFactor;
            double deltaEast = (deltaMD / 2.0) * (Math.Sin(inc1) * Math.Sin(az1) + Math.Sin(inc2) * Math.Sin(az2)) * ratioFactor;
            double deltaTVD = (deltaMD / 2.0) * (Math.Cos(inc1) + Math.Cos(inc2)) * ratioFactor;

            // Calculate cumulative coordinates
            double tvd = previous.TVD + deltaTVD;
            double northing = previous.Northing + deltaNorth;
            double easting = previous.Easting + deltaEast;

            // Calculate Vertical Section (horizontal displacement from origin)
            double verticalSection = Math.Sqrt(northing * northing + easting * easting);

            // Calculate Dogleg Severity (degrees per 100 ft)
            double doglegSeverity = (doglegAngle * RadiansToDegrees / deltaMD) * 100.0;

            // Calculate Build Rate and Turn Rate
            double buildRate = ((current.HoleAngle - previous.HoleAngle) / deltaMD) * 100.0; // deg/100ft
            
            // Turn rate calculation (handle azimuth wrap-around)
            double azimuthChange = current.Azimuth - previous.Azimuth;
            if (azimuthChange > 180) azimuthChange -= 360;
            if (azimuthChange < -180) azimuthChange += 360;
            double turnRate = (azimuthChange / deltaMD) * 100.0; // deg/100ft

            // Update current point with calculated values
            current.SetCalculatedValues(
                tvd: tvd,
                northing: northing,
                easting: easting,
                verticalSection: verticalSection,
                doglegSeverity: doglegSeverity,
                buildRate: buildRate,
                turnRate: turnRate
            );
        }

        /// <summary>
        /// Recalculates trajectory for all survey points in sequence.
        /// </summary>
        /// <param name="surveyPoints">Ordered list of survey points (by MD)</param>
        public void RecalculateAllTrajectories(System.Collections.Generic.List<SurveyPoint> surveyPoints)
        {
            if (surveyPoints == null || surveyPoints.Count == 0) return;

            // Sort by MD to ensure correct order
            surveyPoints.Sort((a, b) => a.MD.CompareTo(b.MD));

            // Calculate first point (tie-in)
            CalculateTrajectory(surveyPoints[0], null);

            // Calculate subsequent points
            for (int i = 1; i < surveyPoints.Count; i++)
            {
                CalculateTrajectory(surveyPoints[i], surveyPoints[i - 1]);
            }
        }

        /// <summary>
        /// Validates if a survey point's calculated values are within acceptable ranges.
        /// </summary>
        public bool ValidateCalculatedValues(SurveyPoint point)
        {
            if (point == null) return false;

            // TVD should never exceed MD
            if (point.TVD > point.MD + 0.01) return false;

            // Dogleg severity warning threshold (typically > 3 deg/100ft is high)
            // This is informational, not a hard validation failure
            
            return true;
        }

        /// <summary>
        /// Generates a template survey point with typical values for a vertical well.
        /// </summary>
        public SurveyPoint CreateTemplatePoint(double md)
        {
            return new SurveyPoint
            {
                MD = md,
                HoleAngle = 0,
                Azimuth = 0
                // TVD, Northing, Easting, etc. will be auto-calculated
            };
        }
    }
}
