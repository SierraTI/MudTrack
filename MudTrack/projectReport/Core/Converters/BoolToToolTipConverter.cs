using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class BoolToToolTipConverter : IValueConverter
    {
        // parameter: message to show when the bool is false
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                // Let the binding system know the value is not set
                return DependencyProperty.UnsetValue;
            }

            if (value is bool b)
            {
                // When true, no tooltip desired -> return empty string (safer than null)
                if (b) return string.Empty;

                // If a parameter was provided, use its string representation
                if (parameter != null) return parameter.ToString() ?? "Disabled";

                return "Disabled";
            }

            // If value is not a bool, don't set the binding target
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
