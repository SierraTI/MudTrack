using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Services.DrillString
{
    /// <summary>
    /// Servicio de nomenclatura automática para componentes de drill string.
    /// Genera nombres descriptivos únicos basados en tipo y secuencia.
    /// </summary>
    public class DrillStringNamingService
    {
        /// <summary>
        /// Genera un nombre descriptivo automáticamente para un componente.
        /// 
        /// Convenciones:
        /// - "Drill Pipe 1", "Drill Pipe 2", etc.
        /// - "HWDP 1"
        /// - "Drill Collar 1", "Drill Collar 2"
        /// - "Stabilizer 1"
        /// - "Jar 1"
        /// - "Bit" (único)
        /// - "MWD" (único)
        /// - "LWD" (único)
        /// - "Motor" (único)
        /// </summary>
        public static string GenerateComponentName(ComponentType componentType, IEnumerable<DrillStringComponent> existingComponents)
        {
            if (existingComponents == null)
                existingComponents = new List<DrillStringComponent>();

            var existingList = existingComponents.ToList();

            // Componentes únicos - solo uno en la sarta
            var uniqueComponents = new[] { ComponentType.Bit, ComponentType.Motor, ComponentType.MWD, ComponentType.LWD, ComponentType.PWD, ComponentType.PWO };

            if (uniqueComponents.Contains(componentType))
            {
                return GetComponentTypeLabel(componentType);
            }

            // Para componentes múltiples, contar existentes del mismo tipo y generar nombre con secuencia
            int sameTypeCount = existingList.Count(c => c.ComponentType == componentType);
            int sequence = sameTypeCount + 1;

            string label = GetComponentTypeLabel(componentType);
            return $"{label} {sequence}";
        }

        /// <summary>
        /// Obtiene la etiqueta legible para un tipo de componente.
        /// </summary>
        private static string GetComponentTypeLabel(ComponentType componentType)
        {
            return componentType switch
            {
                ComponentType.DrillPipe => "Drill Pipe",
                ComponentType.HWDP => "HWDP",
                ComponentType.Casing => "Casing",
                ComponentType.Liner => "Liner",
                ComponentType.SettingTool => "Setting Tool",
                ComponentType.DC => "Drill Collar",
                ComponentType.LWD => "LWD",
                ComponentType.MWD => "MWD",
                ComponentType.PWO => "PWO",
                ComponentType.PWD => "PWD",
                ComponentType.Motor => "Motor",
                ComponentType.XO => "Crossover",
                ComponentType.Jar => "Jar",
                ComponentType.Accelerator => "Accelerator",
                ComponentType.NearBit => "Near Bit",
                ComponentType.Stabilizer => "Stabilizer",
                ComponentType.Bit => "Bit",
                ComponentType.BitSub => "Bit Sub",
                _ => componentType.ToString()
            };
        }

        /// <summary>
        /// Valida que un nombre sea único entre los componentes (excluyendo el componente actual).
        /// </summary>
        public static (bool isValid, string message) ValidateComponentName(string name, int? componentId, IEnumerable<DrillStringComponent> existingComponents)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "Component name cannot be empty");
            }

            if (name.Length > 100)
            {
                return (false, "Component name cannot exceed 100 characters");
            }

            if (existingComponents == null)
                return (true, "");

            var existingList = existingComponents.ToList();

            // Buscar duplicados (excluyendo el componente actual)
            var duplicateExists = existingList.Any(c => 
                c.Id != componentId && 
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                return (false, $"Name \"{name}\" already exists. Please choose a unique name.");
            }

            return (true, "");
        }

        /// <summary>
        /// Auto-renombra componentes después de una eliminación para mantener secuencia.
        /// Ej: Si eliminas "Drill Pipe 2" de [1, 2, 3], los renombra a [1, 2] automáticamente.
        /// </summary>
        public static void AutoRenameSequence(IList<DrillStringComponent> components)
        {
            if (components == null || components.Count == 0)
                return;

            // Agrupar por tipo
            var groupedByType = components
                .GroupBy(c => c.ComponentType)
                .Where(g => !IsUniqueComponent(g.Key))
                .ToList();

            foreach (var group in groupedByType)
            {
                var sequence = 1;
                foreach (var component in group)
                {
                    string label = GetComponentTypeLabel(component.ComponentType);
                    component.Name = $"{label} {sequence}";
                    sequence++;
                }
            }

            // Asegurar que componentes únicos usan el nombre correcto
            foreach (var component in components.Where(c => IsUniqueComponent(c.ComponentType)))
            {
                component.Name = GetComponentTypeLabel(component.ComponentType);
            }
        }

        /// <summary>
        /// Verifica si un tipo de componente es único en la sarta.
        /// </summary>
        private static bool IsUniqueComponent(ComponentType componentType)
        {
            return componentType == ComponentType.Bit || 
                   componentType == ComponentType.Motor || 
                   componentType == ComponentType.MWD || 
                   componentType == ComponentType.LWD || 
                   componentType == ComponentType.PWD || 
                   componentType == ComponentType.PWO;
        }
    }
}
