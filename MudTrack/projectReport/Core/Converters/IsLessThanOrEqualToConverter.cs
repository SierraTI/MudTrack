using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Collections;

namespace ProjectReport.Converters
{
    public class IsLessThanOrEqualToConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return false;

            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return false;

            if (!double.TryParse(values[0].ToString(), out double value1) || 
                !double.TryParse(values[1].ToString(), out double value2))
                return false;

            return value1 <= value2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack not supported");
        }
    }
}
