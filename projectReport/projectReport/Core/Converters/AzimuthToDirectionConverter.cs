using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class AzimuthToDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double azimuth)
            {
                // Normalize azimuth to 0-360
                azimuth = azimuth % 360;
                if (azimuth < 0) azimuth += 360;

                if (azimuth >= 337.5 || azimuth < 22.5) return "N";
                if (azimuth >= 22.5 && azimuth < 67.5) return "NE";
                if (azimuth >= 67.5 && azimuth < 112.5) return "E";
                if (azimuth >= 112.5 && azimuth < 157.5) return "SE";
                if (azimuth >= 157.5 && azimuth < 202.5) return "S";
                if (azimuth >= 202.5 && azimuth < 247.5) return "SW";
                if (azimuth >= 247.5 && azimuth < 292.5) return "W";
                if (azimuth >= 292.5 && azimuth < 337.5) return "NW";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
