using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    /// <summary>
    /// Converts numbers to formatted strings with thousand separators (commas)
    /// and back from formatted strings to numbers
    /// </summary>
    public class NumberFormattingConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value == null)
                return string.Empty;

            if (double.TryParse(value.ToString(), out double dblValue))
            {
                return dblValue.ToString("N0", CultureInfo.CurrentCulture);
            }

            return value.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return null;

            string stringValue = value.ToString() ?? string.Empty;
            
            // Remove thousand separators
            stringValue = stringValue.Replace(",", "").Replace(" ", "");

            if (targetType == typeof(double?) || targetType == typeof(double))
            {
                if (double.TryParse(stringValue, NumberStyles.Float, CultureInfo.CurrentCulture, out double dblValue))
                {
                    return dblValue;
                }
            }

            return null;
        }
    }
}
