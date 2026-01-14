using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    /// <summary>
    /// If value is null, returns the provided ConverterParameter (placeholder string).
    /// Otherwise returns value.ToString().
    /// </summary>
    public class NullToPlaceholderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                if (parameter is string s && !string.IsNullOrEmpty(s)) return s;
                return string.Empty;
            }
            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
