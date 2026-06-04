using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Views.Inventory
{
    public class AlternationIndexToNumberConverter : IValueConverter
    {
        // Convierte AlternationIndex (0-based) a número legible (1-based)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is int idx) return (idx + 1).ToString();
            if (int.TryParse(value.ToString(), out int parsed)) return (parsed + 1).ToString();
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}