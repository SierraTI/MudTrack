using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.WellTest;

namespace ProjectReport.Services
{
    public class ExportService
    {
        /// <summary>
        /// Export wellbore components to CSV format
        /// </summary>
        public void ExportWellboreToCsv(IEnumerable<WellboreComponent> components, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Name,Section Type,Top MD (ft),Bottom MD (ft),OD (in),ID (in),Volume (bbl)");
            
            foreach (var component in components)
            {
                csv.AppendLine($"{component.Id}," +
                             $"\"{component.Name}\"," +
                             $"{component.SectionType}," +
                             $"{(component.TopMD.HasValue ? component.TopMD.Value.ToString("F2") : string.Empty)}," +
                             $"{(component.BottomMD.HasValue ? component.BottomMD.Value.ToString("F2") : string.Empty)}," +
                             $"{(component.OD.HasValue ? component.OD.Value.ToString("F3") : string.Empty)}," +
                             $"{(component.ID.HasValue ? component.ID.Value.ToString("F3") : string.Empty)}," +
                             $"{component.Volume:F2}");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// Export drill string components to CSV format
        /// </summary>
        public void ExportDrillStringToCsv(IEnumerable<DrillStringComponent> components, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Name,Component Type,Length (ft),OD (in),ID (in),Displacement Volume (bbl),Internal Volume (bbl)");
            
            foreach (var component in components)
            {
                csv.AppendLine($"{component.Id}," +
                             $"\"{component.Name}\"," +
                             $"{component.ComponentType}," +
                             $"{component.Length:F2}," +
                             $"{component.OD:F3}," +
                             $"{component.ID:F3}," +
                             $"{component.DisplacementVolume:F2}," +
                             $"{component.InternalVolume:F2}");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// Export survey points to CSV format
        /// </summary>
        public void ExportSurveyToCsv(IEnumerable<SurveyPoint> points, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,MD (ft),TVD (ft),Hole Angle (deg),Azimuth (deg),Northing (ft),Easting (ft),Vertical Section (ft)");
            
            foreach (var point in points)
            {
                csv.AppendLine($"{point.Id}," +
                             $"{point.MD:F2}," +
                             $"{point.TVD:F2}," +
                             $"{point.HoleAngle:F2}," +
                             $"{point.Azimuth:F2}," +
                             $"{point.Northing:F2}," +
                             $"{point.Easting:F2}," +
                             $"{point.VerticalSection:F2}");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// Export well tests to CSV format
        /// </summary>
        public void ExportWellTestsToCsv(IEnumerable<WellTest> tests, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Section,Type,MD (ft),TVD (ft),Test Value (ppb),Description");
            
            foreach (var test in tests)
            {
                csv.AppendLine($"{test.Id}," +
                             $"\"{test.Section}\"," +
                             $"{test.Type}," +
                             $"{test.MD:F2}," +
                             $"{test.TVD:F2}," +
                             $"{test.TestValue:F2}," +
                             $"\"{test.Description}\"");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// Export data to JSON format
        /// </summary>
        public void ExportToJson<T>(IEnumerable<T> data, string filePath)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Export annular volume details to CSV
        /// </summary>
        public void ExportAnnularVolumeDetailsToCsv(IEnumerable<AnnularVolumeDetail> details, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Section/Component Name,Wellbore ID (in),Drill String OD (in),Depth Range (ft),Annular Volume (bbl)");
            
            foreach (var detail in details)
            {
                csv.AppendLine($"{detail.Id}," +
                             $"\"{detail.Name}\"," +
                             $"{detail.WellboreID:F3}," +
                             $"{detail.DrillStringOD:F3}," +
                             $"\"{detail.DepthRange}\"," +
                             $"{detail.Volume:F2}");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }
    }
}
