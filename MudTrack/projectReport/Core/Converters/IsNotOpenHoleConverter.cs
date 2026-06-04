using System;
using System.Globalization;
using System.Windows.Data;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Converters
{
    public class IsNotOpenHoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComponentType sectionType)
            {
                return sectionType != ComponentType.OpenHole;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

