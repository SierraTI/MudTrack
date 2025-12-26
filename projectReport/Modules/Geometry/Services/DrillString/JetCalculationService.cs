using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectReport.Services.DrillString
{
    /// <summary>
    /// Servicio para cálculos relacionados con jets del bit.
    /// Calcula Total Flow Area (TFA) basado en número y diámetro de jets.
    /// </summary>
    public class JetCalculationService
    {
        // Tamaños estándar de jets en 32avos de pulgada
        private static readonly int[] StandardJetSizes = { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

        /// <summary>
        /// Calcula Total Flow Area (TFA) basado en número de jets y diámetro.
        /// 
        /// Fórmula: TFA = n × π × (d/2)²
        /// donde:
        /// - n = número de jets
        /// - d = diámetro del jet en pulgadas (convertido de 32avos)
        /// 
        /// Retorna null si faltan datos, para distinguir entre "no calculado" y "0.00"
        /// </summary>
        public static double? CalculateTFA(int? numJets, int? jetDiameter32nds)
        {
            // Retornar null si faltan datos para indicar "sin calcular"
            if (!numJets.HasValue || !jetDiameter32nds.HasValue)
                return null;

            if (numJets.Value <= 0 || jetDiameter32nds.Value <= 0)
                return null;

            try
            {
                int n = numJets.Value;
                int d32nds = jetDiameter32nds.Value;

                // Convertir de 32avos a pulgadas
                double diameterInches = d32nds / 32.0;

                // Calcular área de un jet: A = π × r²
                double radius = diameterInches / 2.0;
                double singleJetArea = Math.PI * (radius * radius);

                // TFA total
                double tfa = n * singleJetArea;

                return Math.Round(tfa, 3);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene los tamaños estándar de jets disponibles.
        /// </summary>
        public static IEnumerable<int> GetStandardJetSizes()
        {
            return StandardJetSizes.AsEnumerable();
        }

        /// <summary>
        /// Obtiene opciones de dropdown para tamaños de jets con etiquetas formateadas.
        /// </summary>
        public static IEnumerable<(int value, string label)> GetJetSizeOptions()
        {
            return StandardJetSizes.Select(size => (size, $"{size}/32\""));
        }

        /// <summary>
        /// Sugiere configuraciones de jets para lograr un TFA deseado.
        /// Retorna las 3 mejores opciones ordenadas por cercanía al TFA objetivo.
        /// </summary>
        public static IEnumerable<JetSuggestion> SuggestJetConfiguration(double desiredTFA, int? numJets = 3)
        {
            if (!numJets.HasValue || numJets.Value <= 0)
                return new List<JetSuggestion>();

            var suggestions = new List<JetSuggestion>();

            foreach (var jetSize in StandardJetSizes)
            {
                var tfa = CalculateTFA(numJets.Value, jetSize);
                if (tfa.HasValue)
                {
                    suggestions.Add(new JetSuggestion
                    {
                        NumJets = numJets.Value,
                        JetSizeDiameter32nds = jetSize,
                        JetSizeFormatted = $"{jetSize}/32\"",
                        CalculatedTFA = tfa.Value,
                        DifferenceFromTarget = Math.Abs(tfa.Value - desiredTFA)
                    });
                }
            }

            // Ordenar por diferencia menor (más cercano al objetivo)
            return suggestions.OrderBy(s => s.DifferenceFromTarget).Take(3);
        }

        /// <summary>
        /// Valida la configuración de jets.
        /// </summary>
        public static (bool isValid, string message) ValidateJetConfiguration(int? numJets, int? jetDiameter32nds)
        {
            if (!numJets.HasValue || !jetDiameter32nds.HasValue)
            {
                return (false, "Both Number of Jets and Jet Diameter are required");
            }

            if (numJets.Value <= 0)
            {
                return (false, "Number of Jets must be greater than 0");
            }

            if (jetDiameter32nds.Value <= 0)
            {
                return (false, "Jet Diameter must be greater than 0");
            }

            if (!StandardJetSizes.Contains(jetDiameter32nds.Value))
            {
                return (false, $"Jet Diameter must be one of the standard sizes: {string.Join(", ", StandardJetSizes.Select(s => $"{s}/32\""))}");
            }

            return (true, "");
        }
    }

    /// <summary>
    /// Modelo para sugerencias de configuración de jets.
    /// </summary>
    public class JetSuggestion
    {
        public int NumJets { get; set; }
        public int JetSizeDiameter32nds { get; set; }
        public string JetSizeFormatted { get; set; } = string.Empty;
        public double CalculatedTFA { get; set; }
        public double DifferenceFromTarget { get; set; }
    }
}
