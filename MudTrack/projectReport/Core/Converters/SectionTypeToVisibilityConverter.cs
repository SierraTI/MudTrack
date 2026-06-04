using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Converters
{
    public class SectionTypeToVisibilityConverter : IValueConverter
    {
        public ComponentType TargetType { get; set; }
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComponentType sectionType)
            {
                bool isMatch = sectionType == TargetType;
                if (Inverse) isMatch = !isMatch;
                
                return isMatch ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
