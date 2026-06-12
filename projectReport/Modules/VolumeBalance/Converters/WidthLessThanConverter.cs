using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Modules.VolumeBalance.Converters
{
    public class WidthLessThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (parameter == null) return false;
                if (double.TryParse(parameter.ToString(), out var threshold))
                {
                    return d < threshold;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
