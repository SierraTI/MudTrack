using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class BitStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val)
            {
                return val ? "BIT ON BOTTOM (Ready)" : "OFF BOTTOM (Check String Length)";
            }
            return "Unknown Status";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
