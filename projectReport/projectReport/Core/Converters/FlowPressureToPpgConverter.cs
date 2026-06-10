using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class FlowPressureToPpgConverter : IMultiValueConverter
    {
        // Convert [FlowRate, PressureDrop] -> MudDensity (ppg)
        // Reemplaza ComputePpg por la fórmula real que necesites.
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double flow = 0.0;
            double pressure = 0.0;

            if (values.Length > 0 && values[0] != null)
                double.TryParse(values[0].ToString(), NumberStyles.Any, culture, out flow);

            if (values.Length > 1 && values[1] != null)
                double.TryParse(values[1].ToString(), NumberStyles.Any, culture, out pressure);

            double ppg = ComputePpg(flow, pressure);
            return ppg;
        }

        // Fórmula placeholder — sustituye por la ecuación correcta.
        private double ComputePpg(double flowRateGpm, double pressureDropPsi)
        {
            if (flowRateGpm <= 0) return 0.0;
            // EJEMPLO: relación simple (solo placeholder)
            return Math.Round((pressureDropPsi / Math.Max(0.0001, flowRateGpm)) * 0.1, 3);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}