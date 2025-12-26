using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ProjectReport.Models.Geometry.Wellbore;
using ClosedXML.Excel;

namespace ProjectReport.Services
{
    public class WellboreImportService
    {
        public class ImportResult
        {
            public bool Success { get; set; }
            public List<WellboreComponent> WellboreComponents { get; set; } = new();
            public string ErrorMessage { get; set; } = string.Empty;
            public int ImportedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> DetailedErrors { get; set; } = new();
        }

        /// <summary>
        /// Import wellbore data from CSV file
        /// Expected columns: Name, Section Type, Top MD, Bottom MD, ID, OD
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
                        var component = ParseCsvLine(line);
                        if (component != null)
                        {
                            result.WellboreComponents.Add(component);
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
                    result.ErrorMessage = $"Failed to import any valid wellbore components. {result.ErrorCount} errors encountered.";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error reading file: {ex.Message}";
                return result;
            }
        }

        private WellboreComponent? ParseCsvLine(string line)
        {
            var values = SplitCsvLine(line);

            if (values.Length < 6)
            {
                return null; // Not enough columns
            }

            var component = new WellboreComponent();

            // Parse Name (column 0)
            component.Name = values[0].Trim();
            if (string.IsNullOrWhiteSpace(component.Name))
            {
                throw new FormatException("Name cannot be empty");
            }

            // Parse Section Type (column 1)
            if (Enum.TryParse<WellboreSectionType>(values[1].Trim(), true, out var sectionType))
            {
                component.SectionType = sectionType;
            }
            else
            {
                throw new FormatException($"Invalid Section Type: {values[1]}");
            }

            // Parse Top MD (column 2)
            if (double.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double topMD))
            {
                component.TopMD = topMD;
            }
            else
            {
                throw new FormatException("Invalid Top MD value");
            }

            // Parse Bottom MD (column 3)
            if (double.TryParse(values[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double bottomMD))
            {
                component.BottomMD = bottomMD;
            }
            else
            {
                throw new FormatException("Invalid Bottom MD value");
            }

            // Validate Top MD < Bottom MD
            if (topMD >= bottomMD)
            {
                throw new InvalidOperationException($"Top MD ({topMD:F2}) must be less than Bottom MD ({bottomMD:F2})");
            }

            // Parse ID (column 4)
            if (double.TryParse(values[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double id))
            {
                component.ID = id;
            }
            else
            {
                throw new FormatException("Invalid ID value");
            }

            // Parse OD (column 5)
            if (double.TryParse(values[5], NumberStyles.Any, CultureInfo.InvariantCulture, out double od))
            {
                component.OD = od;
            }
            else
            {
                throw new FormatException("Invalid OD value");
            }

            // BR-WG-001: Skip OD validation for Open Hole (handled by model)
            // BR-WG-003: Validate ID < OD for non-Open Hole sections
            if (component.SectionType != WellboreSectionType.OpenHole && id >= od)
            {
                throw new InvalidOperationException($"ID ({id:F3}) must be less than OD ({od:F3})");
            }

            return component;
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
        /// Import wellbore data from Excel file (.xlsx)
        /// Expected columns: Name, Section Type, Top MD, Bottom MD, ID, OD
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
                            if (row.IsEmpty()) continue;

                            var component = new WellboreComponent();

                            // Parse Name (Column 1)
                            component.Name = row.Cell(1).GetString().Trim();
                            if (string.IsNullOrWhiteSpace(component.Name))
                                throw new FormatException("Name cannot be empty");

                            // Parse Section Type (Column 2)
                            var sectionTypeStr = row.Cell(2).GetString().Trim();
                            if (Enum.TryParse<WellboreSectionType>(sectionTypeStr, true, out var sectionType))
                                component.SectionType = sectionType;
                            else
                                throw new FormatException($"Invalid Section Type: {sectionTypeStr}");

                            // Parse Top MD (Column 3)
                            if (row.Cell(3).TryGetValue(out double topMD))
                                component.TopMD = topMD;
                            else
                                throw new FormatException("Invalid Top MD value");

                            // Parse Bottom MD (Column 4)
                            if (row.Cell(4).TryGetValue(out double bottomMD))
                                component.BottomMD = bottomMD;
                            else
                                throw new FormatException("Invalid Bottom MD value");

                            // Validate Top MD < Bottom MD (when both present)
                            if (component.TopMD.HasValue && component.BottomMD.HasValue && component.TopMD.Value >= component.BottomMD.Value)
                                throw new InvalidOperationException($"Top MD ({component.TopMD.Value:F2}) must be less than Bottom MD ({component.BottomMD.Value:F2})");

                            // Parse ID (Column 5)
                            if (row.Cell(5).TryGetValue(out double id))
                                component.ID = id;
                            else
                                throw new FormatException("Invalid ID value");

                            // Parse OD (Column 6)
                            if (row.Cell(6).TryGetValue(out double od))
                                component.OD = od;
                            else
                                throw new FormatException("Invalid OD value");

                            // BR-WG-003: Validate ID < OD for non-Open Hole sections
                            if (component.SectionType != WellboreSectionType.OpenHole && component.ID >= component.OD)
                                throw new InvalidOperationException($"ID ({component.ID:F3}) must be less than OD ({component.OD:F3})");

                            result.WellboreComponents.Add(component);
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
                    result.ErrorMessage = $"Failed to import any valid wellbore components. {result.ErrorCount} errors encountered.";
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
