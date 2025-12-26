using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectReport.Services.DrillString
{
    public static class JetValidator
    {
        public static (bool isValid, List<string> errors) ValidateJetSet(int? numberOfJets, int? jetDiameter32nds)
        {
            var errors = new List<string>();

            if (!numberOfJets.HasValue)
                errors.Add("Number of jets is required");
            else if (numberOfJets.Value <= 0)
                errors.Add("Number of jets must be greater than 0");
            else if (numberOfJets.Value > 20)
                errors.Add("Number of jets seems too high (max: 20)");

            if (!jetDiameter32nds.HasValue)
                errors.Add("Jet diameter is required");
            else if (jetDiameter32nds.Value <= 0)
                errors.Add("Jet diameter must be greater than 0");
            else if (jetDiameter32nds.Value > 32)
                errors.Add("Jet diameter cannot exceed 32/32\" (1 inch)");

            return (!errors.Any(), errors);
        }

        public static (bool isValid, List<object> errors) ValidateAllJetSets(IEnumerable<ProjectReport.Models.Geometry.BitAndJets.JetSet> sets)
        {
            var allErrors = new List<object>();
            int idx = 1;
            foreach (var s in sets)
            {
                var (ok, errs) = ValidateJetSet(s.NumberOfJets, s.JetDiameter32nds);
                if (!ok)
                {
                    allErrors.Add(new { jet_set_id = s.Id, jet_set_index = idx, errors = errs });
                }
                idx++;
            }

            return (allErrors.Count == 0, allErrors);
        }

        public static (double min, double max) GetRecommendedTfaRange(double bitSizeInches)
        {
            var recommendations = new Dictionary<double, (double min, double max)>
            {
                {6.0, (0.25, 0.45)},
                {6.5, (0.30, 0.50)},
                {7.875, (0.35, 0.60)},
                {8.5, (0.40, 0.70)},
                {9.875, (0.50, 0.85)},
                {12.25, (0.70, 1.20)},
                {17.5, (1.20, 2.00)}
            };

            double closest = recommendations.Keys.OrderBy(k => Math.Abs(k - bitSizeInches)).First();
            return recommendations[closest];
        }

        public static List<(string severity, string message)> ValidateTfaForBitSize(double totalTfa, double bitSizeInches)
        {
            var warnings = new List<(string severity, string message)>();
            var (min, max) = GetRecommendedTfaRange(bitSizeInches);

            if (totalTfa < min)
                warnings.Add(("WARNING", $"TFA ({totalTfa} in²) is below recommended minimum ({min} in²) for {bitSizeInches}\" bit"));

            if (totalTfa > max)
                warnings.Add(("WARNING", $"TFA ({totalTfa} in²) exceeds recommended maximum ({max} in²) for {bitSizeInches}\" bit"));

            return warnings;
        }
    }
}
