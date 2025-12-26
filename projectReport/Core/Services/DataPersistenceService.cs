using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models;
using System.Collections.ObjectModel;

namespace ProjectReport.Services
{
    public class DataPersistenceService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task SaveProjectAsync(string filePath, Project project)
        {
            try
            {
                // Update the name to trigger LastModified update through the Name setter
                // This is a workaround since LastModified has a private setter
                var originalName = project.Name;
                if (string.IsNullOrEmpty(originalName))
                {
                    project.Name = " "; // Set a space if empty to trigger the setter
                    project.Name = string.Empty; // Set it back to empty
                }
                else
                {
                    project.Name = originalName; // Trigger the setter to update LastModified
                }
                
                var json = JsonSerializer.Serialize(project, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save project", ex);
            }
        }

        public static async Task<Project> LoadProjectAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Project file not found", filePath);

                var json = await File.ReadAllTextAsync(filePath);
                var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions) ?? 
                    throw new InvalidDataException("Invalid project file");

                // Update last modified timestamp by triggering the Name setter
                var originalName = project.Name;
                project.Name = originalName; // This will update LastModified through the Name setter

                return project;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load project", ex);
            }
        }
        
        public static async Task SaveWellboreComponentsAsync(IEnumerable<WellboreComponent> components, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions(_jsonOptions);
                options.Converters.Add(new JsonStringEnumConverter());
                
                var json = JsonSerializer.Serialize(components, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save wellbore components", ex);
            }
        }
        
        public static async Task<IEnumerable<WellboreComponent>> LoadWellboreComponentsAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<WellboreComponent>();
                    
                var options = new JsonSerializerOptions(_jsonOptions);
                options.Converters.Add(new JsonStringEnumConverter());
                
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<IEnumerable<WellboreComponent>>(json, options) ?? 
                    new List<WellboreComponent>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load wellbore components", ex);
            }
        }
        
        public static async Task SaveDrillStringComponentsAsync(IEnumerable<DrillStringComponent> components, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions(_jsonOptions);
                options.Converters.Add(new JsonStringEnumConverter());
                
                var json = JsonSerializer.Serialize(components, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save drill string components", ex);
            }
        }
        
        public static async Task<IEnumerable<DrillStringComponent>> LoadDrillStringComponentsAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<DrillStringComponent>();
                    
                var options = new JsonSerializerOptions(_jsonOptions);
                options.Converters.Add(new JsonStringEnumConverter());
                
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<IEnumerable<DrillStringComponent>>(json, options) ?? 
                    new List<DrillStringComponent>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load drill string components", ex);
            }
        }

        public static async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath);
                
                // Write header
                var properties = typeof(T).GetProperties();
                await writer.WriteLineAsync(string.Join(",", properties.Select(p => p.Name)));
                
                // Write data
                foreach (var item in data)
                {
                    var values = properties.Select(p => 
                        $"\"{p.GetValue(item)?.ToString()?.Replace("\"", "\"\"") ?? string.Empty}\"");
                    await writer.WriteLineAsync(string.Join(",", values));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export data to CSV", ex);
            }
        }
    }
}
