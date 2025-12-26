using System;
using System.Globalization;
using System.Windows.Data;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Converters
{
    public class OpenHoleODConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // We need the whole object to check SectionType
            if (value is WellboreComponent component)
            {
                if (component.SectionType == WellboreSectionType.OpenHole)
                {
                    return "N/A";
                }
                return component.OD.HasValue ? component.OD.Value.ToString("F2") : string.Empty;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                if (strValue == "N/A") return 0;
                if (double.TryParse(strValue, out double result))
                {
                    return result;
                }
            }
            return 0;
        }
    }
}
