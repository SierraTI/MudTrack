using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Modules.VolumeBalance.Converters
{
    public class HeightOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                double offset = 200.0;
                if (parameter != null)
                {
                    double.TryParse(parameter.ToString(), out offset);
                }

                var result = height - offset;
                if (result < 60) result = 60; // minimum height
                return result;
            }

            return 300.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
