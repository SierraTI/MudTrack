using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using ProjectReport.Models.Geometry.ThermalGradient;
using ProjectReport.Services;

namespace ProjectReport.Services
{
    /// <summary>
    /// Service for importing thermal gradient data from Excel/CSV files
    /// </summary>
    public class ThermalGradientImportService
    {
        /// <summary>
        /// Imports thermal gradient data from a CSV file
        /// </summary>
        public List<ThermalGradientPoint> ImportFromCsv(string filePath)
        {
            var points = new List<ThermalGradientPoint>();
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV file not found", filePath);

            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length < 2) // Need header + at least 1 data row
                throw new InvalidDataException("CSV file must contain header and at least one data row");

            // Skip header row (assume first row is header)
            int id = 1;
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = line.Split(',');
                
                if (values.Length < 2)
                    continue; // Skip invalid rows

                // Parse TVD (first column)
                if (!double.TryParse(values[0].Trim(), out double tvd))
                    continue;

                // Parse Temperature (second column)
                if (!double.TryParse(values[1].Trim(), out double temperature))
                    continue;

                points.Add(new ThermalGradientPoint(id++, tvd, temperature));
            }

            return points;
        }

        /// <summary>
        /// Exports thermal gradient data to CSV file
        /// </summary>
        public void ExportToCsv(List<ThermalGradientPoint> points, string filePath)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("TVD,Temperature");

            // Data rows
            foreach (var point in points.OrderBy(p => p.TVD))
            {
                csv.AppendLine($"{point.TVD:F2},{point.Temperature:F1}");
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// Validates imported data
        /// </summary>
        public List<string> ValidateImportedData(List<ThermalGradientPoint> points)
        {
            var errors = new List<string>();

            if (points.Count < 2)
            {
                errors.Add("At least 2 thermal points are required");
            }

            foreach (var point in points)
            {
                if (point.TVD < 0)
                {
                    errors.Add($"Invalid TVD value: {point.TVD} (must be positive)");
                }

                if (point.Temperature < 32 || point.Temperature > 500)
                {
                    errors.Add($"Unusual temperature value: {point.Temperature}°F at TVD {point.TVD} ft");
                }
            }

            return errors;
        }

        /// <summary>
        /// Shows file dialog and imports data
        /// </summary>
        public List<ThermalGradientPoint>? ShowImportDialog()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import Thermal Gradient Data"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    return ImportFromCsv(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Import failed: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Shows file dialog and exports data
        /// </summary>
        public void ShowExportDialog(List<ThermalGradientPoint> points)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                Title = "Export Thermal Gradient Data"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExportToCsv(points, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Exported {points.Count} thermal points to CSV");
                }
                catch (Exception ex)
                {
                    ToastNotificationService.Instance.ShowError($"Export failed: {ex.Message}");
                }
            }
        }
    }
}
