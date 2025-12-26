using System;
using System.ComponentModel;

namespace ProjectReport.Models.Geometry.ThermalGradient
{
    /// <summary>
    /// Represents a single thermal gradient data point
    /// </summary>
    public class ThermalGradientPoint : BaseModel
    {
        private int _id;
        private double _tvd;
        private double _temperature;
        private bool _hasValidationWarning;
        private string _validationMessage = string.Empty;
        private string _label = string.Empty;
        private double? _calculatedGradient;
        private bool _isAnomalous;

        /// <summary>
        /// Auto-incrementing unique identifier (read-only)
        /// </summary>
        public new int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// True Vertical Depth in feet
        /// </summary>
        public double TVD
        {
            get => _tvd;
            set
            {
                if (SetProperty(ref _tvd, value))
                {
                    ValidateTVD();
                }
            }
        }

        /// <summary>
        /// Formation temperature at depth in degrees Fahrenheit
        /// </summary>
        public double Temperature
        {
            get => _temperature;
            set
            {
                if (SetProperty(ref _temperature, value))
                {
                    ValidateTemperature();
                }
            }
        }

        /// <summary>
        /// Indicates if this point has a validation warning (not an error)
        /// </summary>
        public bool HasValidationWarning
        {
            get => _hasValidationWarning;
            set => SetProperty(ref _hasValidationWarning, value);
        }

        /// <summary>
        /// Validation warning message
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// Label for this thermal point (e.g., "Surface", "Mudline", "BHT")
        /// </summary>
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        /// <summary>
        /// Calculated gradient to the next point in °F/100ft (read-only, auto-calculated)
        /// </summary>
        public double? CalculatedGradient
        {
            get => _calculatedGradient;
            internal set => SetProperty(ref _calculatedGradient, value);
        }

        /// <summary>
        /// Indicates if this point has an anomalous gradient (differs significantly from average)
        /// </summary>
        public bool IsAnomalous
        {
            get => _isAnomalous;
            internal set => SetProperty(ref _isAnomalous, value);
        }

        /// <summary>
        /// Validates TVD value (must be positive)
        /// </summary>
        private void ValidateTVD()
        {
            if (TVD < 0)
            {
                HasValidationWarning = true;
                ValidationMessage = "TVD must be positive";
            }
            else
            {
                HasValidationWarning = false;
                ValidationMessage = string.Empty;
            }
        }

        /// <summary>
        /// Validates temperature value
        /// </summary>
        private void ValidateTemperature()
        {
            if (Temperature < 32)
            {
                HasValidationWarning = true;
                ValidationMessage = "⚠ Temperature below freezing - verify this value";
            }
            else if (Temperature > 500)
            {
                HasValidationWarning = true;
                ValidationMessage = "⚠ Temperature exceeds 500°F - verify this value";
            }
            else
            {
                HasValidationWarning = false;
                ValidationMessage = string.Empty;
            }
        }

        /// <summary>
        /// Creates a new thermal gradient point
        /// </summary>
        public ThermalGradientPoint()
        {
            _id = 0;
            _tvd = 0;
            _temperature = 70; // Default surface temperature
        }

        /// <summary>
        /// Creates a new thermal gradient point with specified values
        /// </summary>
        public ThermalGradientPoint(int id, double tvd, double temperature)
        {
            _id = id;
            _tvd = tvd;
            _temperature = temperature;
            ValidateTVD();
            ValidateTemperature();
        }
    }
}
