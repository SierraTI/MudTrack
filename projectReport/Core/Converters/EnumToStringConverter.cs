using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            // Convert enum to display string
            string enumName = value.ToString() ?? string.Empty;
            
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

