using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ProjectReport.Models.Geometry.DrillString;
using ClosedXML.Excel;

namespace ProjectReport.Services.DrillString
{
    public class DrillStringImportService
    {
        public class ImportResult
        {
            public bool Success { get; set; }
            public List<DrillStringComponent> DrillStringComponents { get; set; } = new();
            public string ErrorMessage { get; set; } = string.Empty;
            public int ImportedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> DetailedErrors { get; set; } = new();
        }

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

                var dataLines = lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));

                foreach (var line in dataLines)
                {
                    try
                    {
                        var component = ParseCsvLine(line);
                        if (component != null)
                        {
                            result.DrillStringComponents.Add(component);
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
                    result.ErrorMessage = $"Failed to import any valid drill string components. {result.ErrorCount} errors encountered.";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private DrillStringComponent? ParseCsvLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            var parts = line.Split(',');
            if (parts.Length < 4) return null;

            var comp = new DrillStringComponent();

            // Expected: Type, Length(ft), ID(in), OD(in)
            comp.ComponentTypeString = parts[0].Trim();
            if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var len)) comp.Length = len;
            if (double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var id)) comp.ID = id;
            if (double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var od)) comp.OD = od;

            return comp;
        }

        // Excel import implementation (matches behavior of the backup service)
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
                    var rows = worksheet.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        try
                        {
                            if (row.IsEmpty()) continue;

                            var component = new DrillStringComponent();

                            // Parse Component Type (Column 1)
                            var componentTypeStr = row.Cell(1).GetString().Trim();
                            component.ComponentTypeString = componentTypeStr;

                            // Parse Length (Column 2)
                            if (row.Cell(2).TryGetValue(out double length))
                                component.Length = length;

                            // Parse ID (Column 3)
                            if (row.Cell(3).TryGetValue(out double id))
                                component.ID = id;

                            // Parse OD (Column 4)
                            if (row.Cell(4).TryGetValue(out double od))
                                component.OD = od;

                            // Optional weight (Column 5)
                            if (!row.Cell(5).IsEmpty() && row.Cell(5).TryGetValue(out double weight))
                                component.WeightPerFoot = weight;

                            result.DrillStringComponents.Add(component);
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
                    result.ErrorMessage = $"Failed to import any valid drill string components. {result.ErrorCount} errors encountered.";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
    }
}
