using System;
using System.Globalization;
using System.Windows.Data;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            // Convert enum to display string
            string enumName = value.ToString() ?? string.Empty;
            
            // Handle WellSectionType enum with proper formatting
            if (value is WellSectionType wellSectionType)
            {
                return wellSectionType switch
                {
                    WellSectionType.Riser => "Riser",
                    WellSectionType.ConductorCasing => "Conductor casing",
                    WellSectionType.SurfaceCasing => "Surface casing",
                    WellSectionType.IntermediateCasing => "Intermediate casing",
                    WellSectionType.ProductionCasing => "Production casing",
                    WellSectionType.Liner => "Liner",
                    WellSectionType.CasedHole => "Cased hole",
                    WellSectionType.OpenHole => "Open hole",
                    _ => enumName
                };
            }
            
            // Handle specific mappings
            if (enumName == "OpenHole") return "Open Hole";
            if (enumName == "LeakOff") return "Leak Off";
            if (enumName == "FractureGradient") return "Fracture gradient";
            if (enumName == "FormationIntegrity") return "Integrity";
            if (enumName == "PorePressure") return "Pore pressure";
            if (enumName == "Stabilizer") return "Stabilizer";
            
            return enumName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && parameter is Type enumType)
            {
                // Handle WellSectionType enum conversion
                if (enumType == typeof(WellSectionType))
                {
                    return str switch
                    {
                        "Riser" => WellSectionType.Riser,
                        "Conductor casing" => WellSectionType.ConductorCasing,
                        "Surface casing" => WellSectionType.SurfaceCasing,
                        "Intermediate casing" => WellSectionType.IntermediateCasing,
                        "Production casing" => WellSectionType.ProductionCasing,
                        "Liner" => WellSectionType.Liner,
                        "Cased hole" => WellSectionType.CasedHole,
                        "Open hole" => WellSectionType.OpenHole,
                        _ => value
                    };
                }
                
                // Convert display string back to enum
                string normalized = str.Replace(" ", "");
                if (normalized == "OpenHole") normalized = "OpenHole";
                if (normalized == "LeakOff") normalized = "LeakOff";
                if (normalized == "Fracturegradient") normalized = "FractureGradient";
                if (normalized == "Integrity") normalized = "FormationIntegrity";
                if (normalized == "Porepressure") normalized = "PorePressure";
                if (normalized == "Stabilizer") normalized = "Stabilizer";
                
                if (Enum.TryParse(enumType, normalized, true, out object? result))
                    return result;
            }
            
            return value;
        }
    }
}

