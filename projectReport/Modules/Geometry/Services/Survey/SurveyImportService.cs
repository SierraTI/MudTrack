using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ProjectReport.Models.Geometry.Survey;
using ClosedXML.Excel;

namespace ProjectReport.Services.Survey
{
    public class SurveyImportService
    {
        public class ImportResult
        {


            public bool Success { get; set; }
            public List<SurveyPoint> SurveyPoints { get; set; } = new();
            public string ErrorMessage { get; set; } = string.Empty;
            public int ImportedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> DetailedErrors { get; set; } = new();
        }

        /// <summary>
        /// Import survey data from CSV file
        /// Expected columns: MD, TVD, Hole Angle, Azimuth, Horizontal Displacement
        /// </summary>
        public ImportResult ImportFromCsv(string filePath)
        {
            var result = new ImportResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "File not found.";
                    return result;
                }

                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0)
                {
                    result.ErrorMessage = "File is empty.";
                    return result;
                }

                // Skip header row
                var dataLines = lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));

                foreach (var line in dataLines)
                {
                    try
                    {
                        var surveyPoint = ParseCsvLine(line);
                        if (surveyPoint != null)
                        {
                            result.SurveyPoints.Add(surveyPoint);
                            result.ImportedCount++;
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.DetailedErrors.Add($"Row {result.ImportedCount + result.ErrorCount + 1}: Invalid data format");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.DetailedErrors.Add($"Row {result.ImportedCount + result.ErrorCount + 1}: {ex.Message}");
                    }
                }

                result.Success = result.ImportedCount > 0;
                if (!result.Success && result.ErrorCount > 0)
                {
                    result.ErrorMessage = $"Failed to import any valid survey points. {result.ErrorCount} errors encountered.";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Parse a single CSV line into a SurveyPoint object.
        /// Expected format: MD,TVD,HoleAngle,Azimuth,HorizontalDisplacement
        /// </summary>
        private SurveyPoint? ParseCsvLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            var parts = line.Split(',');
            if (parts.Length < 2) return null;

            var surveyPoint = new SurveyPoint();

            try
            {
                if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var md))
                    surveyPoint.MD = md;
                else
                    return null;

                if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var tvd))
                    surveyPoint.TVD = tvd;
                else
                    return null;

                // Optional fields
                if (parts.Length > 2 && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var holeAngle))
                    surveyPoint.HoleAngle = holeAngle;

                if (parts.Length > 3 && double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var azimuth))
                    surveyPoint.Azimuth = azimuth;

                if (parts.Length > 4 && double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var horizontalDisplacement))
                    surveyPoint.Northing = horizontalDisplacement;

                return surveyPoint;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Import survey data from Excel file (.xlsx)
        /// Expected columns: MD, TVD, Hole Angle, Azimuth, Horizontal Displacement
        /// </summary>
        public ImportResult ImportFromExcel(string filePath)
        {
            var result = new ImportResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "File not found.";
                    return result;
                }

                using (var workbook = new XLWorkbook(filePath))
                {
                    if (workbook.Worksheets.Count == 0)
                    {
                        result.ErrorMessage = "No worksheets found in Excel file.";
                        return result;
                    }

                    var worksheet = workbook.Worksheets.First();
                    if (worksheet.RowsUsed().Count() == 0)
                    {
                        result.ErrorMessage = "Worksheet is empty.";
                        return result;
                    }

                    var rows = worksheet.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        try
                        {
                            var surveyPoint = new SurveyPoint();

                            if (row.Cell(1).TryGetValue(out double md))
                                surveyPoint.MD = md;
                            else
                                continue;

                            if (row.Cell(2).TryGetValue(out double tvd))
                                surveyPoint.TVD = tvd;
                            else
                                continue;

                            if (surveyPoint.MD < surveyPoint.TVD)
                                throw new InvalidOperationException($"MD ({surveyPoint.MD:F2}) must be >= TVD ({surveyPoint.TVD:F2})");

                            if (row.Cell(3).TryGetValue(out double holeAngle))
                                surveyPoint.HoleAngle = holeAngle;

                            if (surveyPoint.HoleAngle > 93)
                                throw new InvalidOperationException($"Hole Angle ({surveyPoint.HoleAngle:F2}°) cannot exceed 93°");

                            if (row.Cell(4).TryGetValue(out double azimuth))
                                surveyPoint.Azimuth = azimuth;

                            if (surveyPoint.Azimuth > 360)
                                throw new InvalidOperationException($"Azimuth ({surveyPoint.Azimuth:F2}°) cannot exceed 360°");

                            if (row.Cell(5).TryGetValue(out double northing))
                                surveyPoint.Northing = northing;

                            result.SurveyPoints.Add(surveyPoint);
                            result.ImportedCount++;
                        }
                        catch (Exception ex)
                        {
                            result.ErrorCount++;
                            result.DetailedErrors.Add($"Row {row.RowNumber()}: {ex.Message}");
                        }
                    }

                    result.Success = result.ImportedCount > 0;
                    if (!result.Success && result.ErrorCount > 0)
                    {
                        result.ErrorMessage = $"Failed to import any valid survey points. {result.ErrorCount} errors encountered.";
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
    }
}
