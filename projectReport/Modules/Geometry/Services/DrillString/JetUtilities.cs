using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectReport.Services.DrillString
{
    public static class JetUtilities
    {
        public static IEnumerable<object> GetStandardJetSizes()
        {
            return JetCalculationService.GetStandardJetSizes().Select(s => new { value = s, label = $"{s}/32\"", decimal_in = (s / 32.0).ToString("0.000\"") });
        }

        public static IEnumerable<JetSuggestion> SuggestConfigurations(double targetTfa)
        {
            return JetCalculationService.SuggestJetConfiguration(targetTfa, 3);
        }

        public static object ConvertTfaToEquivalentNozzles(double totalTfa)
        {
            var standardArea = JetCalculationService.CalculateTFA(1, 12) ?? 0.0;
            var eq = standardArea > 0 ? Math.Round(totalTfa / standardArea, 2) : 0.0;
            return new { tfa_total = totalTfa, equivalent_12_32_nozzles = eq, standard_nozzle_size = "12/32\"", standard_nozzle_area = standardArea };
        }
    }
}
