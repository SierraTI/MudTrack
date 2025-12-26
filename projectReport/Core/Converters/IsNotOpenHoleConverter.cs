using System;
using System.Globalization;
using System.Windows.Data;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Converters
{
    public class IsNotOpenHoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WellboreSectionType sectionType)
            {
                return sectionType != WellboreSectionType.OpenHole;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

