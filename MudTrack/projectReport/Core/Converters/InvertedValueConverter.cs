using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    /// <summary>
    /// Converter that inverts a numeric value (multiplies by -1)
    /// Used for inverting Y-axis in charts (depth charts where 0 should be at top)
    /// </summary>
    public class InvertedValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            
            if (double.TryParse(value.ToString(), NumberStyles.Any, culture, out double doubleValue))
            {
                return -doubleValue;
            }
            
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            
            if (double.TryParse(value.ToString(), NumberStyles.Any, culture, out double doubleValue))
            {
                return -doubleValue;
            }
            
            return 0;
        }
    }
}
