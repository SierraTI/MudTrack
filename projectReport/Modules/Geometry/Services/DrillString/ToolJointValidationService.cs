using System;
using System.Collections.Generic;

namespace ProjectReport.Services.DrillString
{
    /// <summary>
    /// Servicio para validación de configuración de Tool Joint.
    /// Tool Joint propiedades son opcionales - pueden quedar vacías.
    /// </summary>
    public class ToolJointValidationService
    {
        /// <summary>
        /// Obtiene valores vacíos para Tool Joint.
        /// IMPORTANTE: Todos los campos en null, NO en 0
        /// </summary>
        public static ToolJointData GetEmptyToolJoint()
        {
            return new ToolJointData
            {
                ToolJointOD = null,
                ToolJointID = null,
                ToolJointLength = null,
                JointLength = null
            };
        }

        /// <summary>
        /// Valida configuración de Tool Joint.
        /// Los campos son opcionales pero si se llenan deben ser válidos.
        /// </summary>
        public static (bool isValid, List<string> errors) ValidateToolJoint(ToolJointData? data)
        {
            var errors = new List<string>();

            if (data == null)
                return (true, errors); // Null es válido (sin configuración)

            // Validar ToolJointOD si existe
            if (data.ToolJointOD.HasValue)
            {
                if (data.ToolJointOD.Value <= 0)
                {
                    errors.Add("Tool Joint OD must be greater than 0");
                }
                
                // Si ID también existe, validar OD > ID
                if (data.ToolJointID.HasValue && data.ToolJointID.Value >= data.ToolJointOD.Value)
                {
                    errors.Add("Tool Joint ID must be smaller than OD");
                }
            }

            // Validar ToolJointID si existe
            if (data.ToolJointID.HasValue)
            {
                if (data.ToolJointID.Value <= 0)
                {
                    errors.Add("Tool Joint ID must be greater than 0");
                }
            }

            // Validar ToolJointLength si existe
            if (data.ToolJointLength.HasValue)
            {
                if (data.ToolJointLength.Value <= 0)
                {
                    errors.Add("Tool Joint Length must be greater than 0");
                }
            }

            // Validar JointLength si existe
            if (data.JointLength.HasValue)
            {
                if (data.JointLength.HasValue)
                {
                    errors.Add("Joint Length must be greater than 0");
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates Tool Joint configuration against pipe dimensions.
        /// Tool Joint OD must be >= Pipe OD
        /// Tool Joint ID must be <= Pipe ID
        /// </summary>
        public static List<string> ValidateAgainstPipe(ToolJointData? config, double? pipeOD, double? pipeID)
        {
            var errors = new List<string>();

            if (config == null) return errors;

            // Validate Tool Joint OD must be >= Pipe OD
            if (config.ToolJointOD.HasValue && pipeOD.HasValue)
            {
                if (config.ToolJointOD.Value < pipeOD.Value)
                {
                    errors.Add("Tool Joint OD cannot be less than Pipe OD");
                }
            }

            // Validate Tool Joint ID must be <= Pipe ID
            if (config.ToolJointID.HasValue && pipeID.HasValue)
            {
                if (config.ToolJointID.Value > pipeID.Value)
                {
                    errors.Add("Tool Joint ID cannot be greater than Pipe ID");
                }
            }

            return errors;
        }

        /// <summary>
        /// Verifica si Tool Joint está completamente configurado (todos los campos tienen valor).
        /// </summary>
        public static bool IsFullyConfigured(ToolJointData? data)
        {
            if (data == null)
                return false;

            return data.ToolJointOD.HasValue &&
                   data.ToolJointID.HasValue &&
                   data.ToolJointLength.HasValue &&
                   data.JointLength.HasValue;
        }

        /// <summary>
        /// Verifica si Tool Joint está parcialmente configurado (algunos campos tienen valor).
        /// </summary>
        public static bool IsPartiallyConfigured(ToolJointData? data)
        {
            if (data == null)
                return false;

            int filledFields = 0;
            if (data.ToolJointOD.HasValue) filledFields++;
            if (data.ToolJointID.HasValue) filledFields++;
            if (data.ToolJointLength.HasValue) filledFields++;
            if (data.JointLength.HasValue) filledFields++;

            return filledFields > 0 && filledFields < 4;
        }
    }

    /// <summary>
    /// Modelo para datos de Tool Joint Configuration.
    /// </summary>
    public class ToolJointData
    {
        public double? ToolJointOD { get; set; }
        public double? ToolJointID { get; set; }
        public double? ToolJointLength { get; set; }
        public double? JointLength { get; set; }
    }
}
