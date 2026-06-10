using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class ZeroToEmptyConverter : IValueConverter
    {
        // Convert from double -> string: return empty if value == 0
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (Math.Abs(d) < 1e-12) return string.Empty;
                return d.ToString(parameter as string ?? "N3", culture);
            }
            return string.Empty;
        }

        // ConvertBack from string -> double: empty or invalid => 0
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            if (double.TryParse(s, NumberStyles.Any, culture, out var d)) return d;
            return 0.0;
        }
    }
}