using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class LessThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string paramStr)
            {
                if (double.TryParse(paramStr, out double threshold))
                {
                    return doubleValue < threshold;
                }
            }
            // Handle numeric parameter passed directly (not string) if needed, though XAML usually passes string from Parameter
            if (value is double dVal && parameter is double dParam)
            {
               return dVal < dParam;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
