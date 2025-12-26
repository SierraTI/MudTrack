using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ProjectReport.Models.Geometry.Survey;
using ClosedXML.Excel;

namespace ProjectReport.Services.Backup
{
    // Renamed backup of original import service to avoid duplicate type collision
    public class SurveyImportService_Backup
    {
        public class ImportResult
        {
            public bool Success { get; set; }
            public List<SurveyPoint> SurveyPoints { get; set; } = new();
            public string ErrorMessage { get; set; } = string.Empty;
            public int ImportedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> DetailedErrors { get; set; } = new(); // Track specific row errors
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
                result.ErrorMessage = $"Error reading file: {ex.Message}";
                return result;
            }
        }

        private SurveyPoint? ParseCsvLine(string line)
        {
            // Split by comma, handling potential quotes
            var values = SplitCsvLine(line);

            if (values.Length < 4)
            {
                return null; // Not enough columns
            }

            var surveyPoint = new SurveyPoint();

            // Parse MD (column 0)
            if (double.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double md))
            {
                surveyPoint.MD = md;
            }
            else
            {
                throw new FormatException("Invalid MD value");
            }

            // Parse TVD (column 1)
            if (double.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double tvd))
            {
                surveyPoint.TVD = tvd;
            }
            else
            {
                throw new FormatException("Invalid TVD value");
            }

            // BR-SV-001: Validate MD >= TVD
            if (md < tvd)
            {
                throw new InvalidOperationException($"MD ({md:F2}) must be >= TVD ({tvd:F2})");
            }

            // Parse Hole Angle (column 2)
            if (double.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double holeAngle))
            {
                surveyPoint.HoleAngle = holeAngle;
            }
            else
            {
                throw new FormatException("Invalid Hole Angle value");
            }

            // BR-SV-002: Validate Hole Angle <= 93°
            if (holeAngle > 93)
            {
                throw new InvalidOperationException($"Hole Angle ({holeAngle:F2}°) cannot exceed 93°");
            }

            // Parse Azimuth (column 3)
            if (double.TryParse(values[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double azimuth))
            {
                surveyPoint.Azimuth = azimuth;
            }
            else
            {
                throw new FormatException("Invalid Azimuth value");
            }

            // BR-SV-003: Validate Azimuth <= 360°
            if (azimuth > 360)
            {
                throw new InvalidOperationException($"Azimuth ({azimuth:F2}°) cannot exceed 360°");
            }

            // Parse Horizontal Displacement (column 4, optional)
            if (values.Length > 4 && double.TryParse(values[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double horizontalDisplacement))
            {
                surveyPoint.Northing = horizontalDisplacement;
            }

            return surveyPoint;
        }

        private string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentValue = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue.Trim());
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }

            result.Add(currentValue.Trim());
            return result.ToArray();
        }

        /// <summary>
        /// Import survey data from Excel file (.xlsx)
        /// Expected columns: MD, TVD, Hole Angle, Azimuth (optional: Northing, Easting)
        /// </summary>
        /// <summary>
        /// Import survey data from Excel file (.xlsx)
        /// Expected columns: MD, TVD, Hole Angle, Azimuth (optional: Northing, Easting)
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

                using (var workbook = new ClosedXML.Excel.XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        result.ErrorMessage = "No worksheets found in the Excel file.";
                        return result;
                    }

                    // Assume header is in row 1, data starts in row 2
                    var rows = worksheet.RowsUsed().Skip(1); // Skip header

                    foreach (var row in rows)
                    {
                        try
                        {
                            // Check if row is empty
                            if (row.IsEmpty()) continue;

                            var surveyPoint = new SurveyPoint();
                            
                            // Parse MD (Column 1)
                            if (row.Cell(1).TryGetValue(out double md))
                                surveyPoint.MD = md;
                            else
                                throw new FormatException("Invalid MD value");

                            // Parse TVD (Column 2)
                            if (row.Cell(2).TryGetValue(out double tvd))
                                surveyPoint.TVD = tvd;
                            else
                                throw new FormatException("Invalid TVD value");

                            // BR-SV-001: Validate MD >= TVD
                            if (surveyPoint.MD < surveyPoint.TVD)
                                throw new InvalidOperationException($"MD ({surveyPoint.MD:F2}) must be >= TVD ({surveyPoint.TVD:F2})");

                            // Parse Hole Angle (Column 3)
                            if (row.Cell(3).TryGetValue(out double holeAngle))
                                surveyPoint.HoleAngle = holeAngle;
                            else
                                throw new FormatException("Invalid Hole Angle value");

                            // BR-SV-002: Validate Hole Angle <= 93°
                            if (surveyPoint.HoleAngle > 93)
                                throw new InvalidOperationException($"Hole Angle ({surveyPoint.HoleAngle:F2}°) cannot exceed 93°");

                            // Parse Azimuth (Column 4)
                            if (row.Cell(4).TryGetValue(out double azimuth))
                                surveyPoint.Azimuth = azimuth;
                            else
                                throw new FormatException("Invalid Azimuth value");

                            // BR-SV-003: Validate Azimuth <= 360°
                            if (surveyPoint.Azimuth > 360)
                                throw new InvalidOperationException($"Azimuth ({surveyPoint.Azimuth:F2}°) cannot exceed 360°");

                            // Parse Horizontal Displacement (Column 5, optional)
                            if (!row.Cell(5).IsEmpty() && row.Cell(5).TryGetValue(out double northing))
                            {
                                surveyPoint.Northing = northing;
                            }

                            result.SurveyPoints.Add(surveyPoint);
                            result.ImportedCount++;
                        }
                        catch (Exception ex)
                        {
                            result.ErrorCount++;
                            result.DetailedErrors.Add($"Row {row.RowNumber()}: {ex.Message}");
                        }
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
                result.ErrorMessage = $"Error reading Excel file: {ex.Message}";
                return result;
            }
        }
    }
}
