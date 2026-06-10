using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.Survey;

namespace ProjectReport.Services.Survey
{
    /// <summary>
    /// Service for validating survey data according to industry standards
    /// Validates: Point 0 existence, MD progression, duplicate MDs, inclination jumps, DLS warnings
    /// </summary>
    public class SurveyValidationService
    {
        /// <summary>
        /// Validates all survey points and returns a list of validation errors
        /// </summary>
        public List<SurveyValidationError> ValidateSurvey(List<SurveyPoint> surveyPoints)
        {
            var errors = new List<SurveyValidationError>();

            if (surveyPoints == null || surveyPoints.Count == 0)
            {
                errors.Add(new SurveyValidationError
                {
                    PointId = 0,
                    Severity = ValidationSeverity.Error,
                    Message = "No survey points found. Add at least one point starting at MD=0."
                });
                return errors;
            }

            var sorted = surveyPoints.OrderBy(p => p.MD).ToList();

            // Rule 1: First point must be at surface (MD=0, Incl=0, Azim=0)
            ValidateSurfacePoint(sorted, errors);

            // Rule 2: MD must be strictly increasing (no duplicates, no decreases)
            ValidateMDProgression(sorted, errors);

            // Rule 3: Inclination must be between 0-180 degrees
            ValidateInclinationRange(sorted, errors);

            // Rule 4: Azimuth must be between 0-360 degrees
            ValidateAzimuthRange(sorted, errors);

            // Rule 5: Check for physically impossible inclination jumps (>90° between close points)
            ValidateInclinationJumps(sorted, errors);

            // Rule 6: Check DLS warnings (>3°/100ft is high, >10°/100ft is critical)
            ValidateDoglegSeverity(sorted, errors);

            // Rule 7: TVD should never exceed MD
            ValidateTVDvsMD(sorted, errors);

            return errors;
        }

        /// <summary>
        /// Validates that the first point is at surface (MD=0, Incl=0, Azim=0)
        /// </summary>
        private void ValidateSurfacePoint(List<SurveyPoint> sorted, List<SurveyValidationError> errors)
        {
            if (sorted.Count == 0) return;

            var firstPoint = sorted[0];
            bool hasError = false;

            if (Math.Abs(firstPoint.MD) > 0.01)
            {
                errors.Add(new SurveyValidationError
                {
                    PointId = firstPoint.Id,
                    Severity = ValidationSeverity.Error,
                    Message = $"❌ CRITICAL: First survey point must be at surface (MD=0). Current MD: {firstPoint.MD:F2} ft. The graph cannot render without a surface anchor point."
                });
                hasError = true;
            }

            if (Math.Abs(firstPoint.HoleAngle) > 0.01)
            {
                errors.Add(new SurveyValidationError
                {
                    PointId = firstPoint.Id,
                    Severity = ValidationSeverity.Warning,
                    Message = $"⚠️ Warning: Surface point should have Inclination=0°. Current: {firstPoint.HoleAngle:F2}°"
                });
            }

            if (Math.Abs(firstPoint.Azimuth) > 0.01)
            {
                errors.Add(new SurveyValidationError
                {
                    PointId = firstPoint.Id,
                    Severity = ValidationSeverity.Warning,
                    Message = $"⚠️ Warning: Surface point should have Azimuth=0°. Current: {firstPoint.Azimuth:F2}°"
                });
            }

            if (!hasError && sorted.Count > 0)
            {
                // Ensure first point is marked as tie-in
                firstPoint.IsTieInPoint = true;
            }
        }

        /// <summary>
        /// Validates that MD is strictly increasing (no duplicates, no decreases)
        /// </summary>
        private void ValidateMDProgression(List<SurveyPoint> sorted, List<SurveyValidationError> errors)
        {
            for (int i = 1; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var previous = sorted[i - 1];

                // Check for duplicate MD
                if (Math.Abs(current.MD - previous.MD) < 0.01)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = current.Id,
                        Severity = ValidationSeverity.Error,
                        Message = $"❌ ERROR: Duplicate MD detected. Point ID {current.Id} has MD={current.MD:F2} ft, same as Point ID {previous.Id}. MD must be unique and increasing."
                    });
                }

                // Check for decreasing MD
                if (current.MD < previous.MD)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = current.Id,
                        Severity = ValidationSeverity.Error,
                        Message = $"❌ ERROR B1: MD decreases. Point ID {current.Id} has MD={current.MD:F2} ft, which is less than previous point ({previous.MD:F2} ft). The well cannot go backwards in depth."
                    });
                }
            }
        }

        /// <summary>
        /// Validates that inclination is within valid range (0-180 degrees)
        /// </summary>
        private void ValidateInclinationRange(List<SurveyPoint> sorted, List<SurveyValidationError> errors)
        {
            foreach (var point in sorted)
            {
                if (point.HoleAngle < 0 || point.HoleAngle > 180)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = point.Id,
                        Severity = ValidationSeverity.Error,
                        Message = $"❌ ERROR S3: Hole Angle ({point.HoleAngle:F2}°) is outside valid range (0° - 180°)."
                    });
                }
            }
        }

        /// <summary>
        /// Validates that azimuth is within valid range (0-360 degrees)
        /// </summary>
        private void ValidateAzimuthRange(List<SurveyPoint> sorted, List<SurveyValidationError> errors)
        {
            foreach (var point in sorted)
            {
                if (point.Azimuth < 0 || point.Azimuth > 360)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = point.Id,
                        Severity = ValidationSeverity.Error,
                        Message = $"❌ ERROR S3: Azimuth ({point.Azimuth:F2}°) is outside valid range (0° - 360°)."
                    });
                }
            }
        }

        /// <summary>
        /// Validates for physically impossible inclination jumps (>90° between close points)
        /// </summary>
        private void ValidateInclinationJumps(List<SurveyPoint> sorted, List<SurveyValidationError> errors)
        {
            for (int i = 1; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var previous = sorted[i - 1];
                double deltaMD = current.MD - previous.MD;

                if (deltaMD <= 0) continue; // Skip if MD not increasing

                double inclinationChange = Math.Abs(current.HoleAngle - previous.HoleAngle);

                // If points are very close (< 10 ft) and inclination changes by >90°, it's physically impossible
                if (deltaMD < 10 && inclinationChange > 90)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = current.Id,
                        Severity = ValidationSeverity.Error,
                        Message = $"❌ CRITICAL ERROR: Physically impossible inclination jump detected. Point ID {current.Id} changes inclination by {inclinationChange:F2}° over only {deltaMD:F2} ft. This is physically impossible."
                    });
                }
                // Warning for large changes over short distances
                else if (deltaMD < 50 && inclinationChange > 45)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = current.Id,
                        Severity = ValidationSeverity.Warning,
                        Message = $"⚠️ WARNING: Large inclination change ({inclinationChange:F2}°) over short distance ({deltaMD:F2} ft). Verify this is correct."
                    });
                }
            }
        }

        /// <summary>
        /// Validates Dogleg Severity and adds warnings for high DLS values
        /// </summary>
        private void ValidateDoglegSeverity(List<SurveyPoint> sorted, List<SurveyValidationError> errors)
        {
            foreach (var point in sorted)
            {
                if (point.DoglegSeverity > 10)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = point.Id,
                        Severity = ValidationSeverity.Error,
                        Message = $"❌ CRITICAL: Very high Dogleg Severity ({point.DoglegSeverity:F2}°/100ft) at MD={point.MD:F2} ft. This may cause drill string damage."
                    });
                }
                else if (point.DoglegSeverity > 3)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = point.Id,
                        Severity = ValidationSeverity.Warning,
                        Message = $"⚠️ WARNING: High Dogleg Severity ({point.DoglegSeverity:F2}°/100ft) at MD={point.MD:F2} ft. Monitor for potential drill string issues."
                    });
                }
            }
        }

        /// <summary>
        /// Validates that TVD never exceeds MD (physical constraint)
        /// </summary>
        private void ValidateTVDvsMD(List<SurveyPoint> sorted, List<SurveyValidationError> errors)
        {
            foreach (var point in sorted)
            {
                if (point.TVD > point.MD + 0.01)
                {
                    errors.Add(new SurveyValidationError
                    {
                        PointId = point.Id,
                        Severity = ValidationSeverity.Error,
                        Message = $"❌ ERROR S2: TVD ({point.TVD:F2} ft) exceeds MD ({point.MD:F2} ft). This is physically impossible. TVD must always be ≤ MD."
                    });
                }
            }
        }

        /// <summary>
        /// Ensures that a surface point (MD=0) exists, creating it if necessary
        /// </summary>
        public SurveyPoint? EnsureSurfacePoint(List<SurveyPoint> surveyPoints)
        {
            if (surveyPoints == null) return null;

            var surfacePoint = surveyPoints.FirstOrDefault(p => Math.Abs(p.MD) < 0.01);

            if (surfacePoint == null)
            {
                // Create surface point
                surfacePoint = new SurveyPoint
                {
                    MD = 0,
                    HoleAngle = 0,
                    Azimuth = 0,
                    IsTieInPoint = true
                };
                surveyPoints.Insert(0, surfacePoint);
            }
            else
            {
                // Ensure it's at the beginning
                surveyPoints.Remove(surfacePoint);
                surveyPoints.Insert(0, surfacePoint);
                surfacePoint.IsTieInPoint = true;
            }

            return surfacePoint;
        }
    }

    /// <summary>
    /// Represents a survey validation error
    /// </summary>
    public class SurveyValidationError
    {
        public int PointId { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Severity level for validation errors
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }
}
