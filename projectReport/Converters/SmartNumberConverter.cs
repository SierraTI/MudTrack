using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectReport.Converters
{
    public class SmartNumberConverter : IValueConverter
    {
        // =========================
        // VIEW -> UI (formato)
        // =========================
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
            {
                // Entero sin decimales, decimal con hasta 2
                return number % 1 == 0
                    ? number.ToString("0", CultureInfo.InvariantCulture)
                    : number.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        // =========================
        // UI -> VIEW (validación)
        // =========================
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim();

            if (string.IsNullOrEmpty(text))
                return null;

            // permitir coma como decimal
            text = text.Replace(",", ".");

            // validar caracteres básicos (números + punto)
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != '.')
                    return null;
            }

            // evitar múltiples puntos
            int firstDot = text.IndexOf('.');
            if (firstDot >= 0 && text.IndexOf('.', firstDot + 1) >= 0)
                return null;

            // parse final seguro
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;

            return null;
        }
    }
}