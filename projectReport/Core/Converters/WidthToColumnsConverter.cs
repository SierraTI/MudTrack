using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class WidthToColumnsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double w)
            {
                if (w < 520) return 1;     // móvil/mini ventana
                if (w < 820) return 2;     // pequeño
                if (w < 1100) return 3;    // mediano
                return 6;                  // grande
            }
            return 6;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
