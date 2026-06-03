using System;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.ViewModels;
using System.Globalization;


namespace ProjectReport.ViewModels.Geometry.Config
{
    public class ToolJointConfigViewModel : BaseViewModel
    {
        public ToolJointConfig Model { get; }

        private double? _tjOD;
        private double? _tjID;
        private double? _tjLength;
        private double? _weight;
        private double? _tjIDLength;

        private readonly WellboreComponent? _currentWellboreComponent; 

        public double? TJ_OD
        {
            get => Model.TJ_OD;
            set
            {
                if (SetProperty(ref _tjOD, value))
                {
                    Model.TJ_OD = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TJ_OD_String));
                }
            }
        }

        public string TJ_OD_String
        {
      
            get => Model.TJ_OD?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_OD = null;
                    return;
                }

                value = value.Replace(',', '.');

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    TJ_OD = result;
                }
            }
        }

        public double? TJ_ID
        {
            get => Model.TJ_ID;
            set
            {
                if (SetProperty(ref _tjID, value))
                {
                    Model.TJ_ID = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TJ_ID_String));
                }
            }
        }

        public string TJ_ID_String
        {

            get => Model.TJ_ID?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_ID = null;
                    return;
                }

                value = value.Trim().Replace(',', '.');

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    TJ_ID = result;
                }
            }
        }

        public double? TJ_Length
        {
            get => Model.TJ_Length;
            set
            {
                if (SetProperty(ref _tjLength, value))
                {
                    Model.TJ_Length = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TJ_Length_String));
                }
            }
        }

        public string TJ_Length_String
        {
            get => Model.TJ_Length?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_Length = null;
                    return;
                }

                value = value.Trim().Replace(',', '.');

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    TJ_Length = result;
                }
            }
        }

        public double? Weight
        {
            get => Model.Weight;
            set
            {
                if (SetProperty(ref _weight, value))
                {
                    Model.Weight = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Weight_String));
                }
            }
        }

        public string Weight_String
        {
            get => Model.Weight?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Weight = null;
                    return;
                }

                value = value.Trim().Replace(',', '.');

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    Weight = result;
                }
            }
        }

        public double? TJ_ID_Length
        {
            get => Model.TJ_ID_Length;
            set
            {
                if (SetProperty(ref _tjIDLength, value))
                {
                    Model.TJ_ID_Length = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TJ_ID_Length_String));
                }
            }
        }

        public string TJ_ID_Length_String
        {
            get => Model.TJ_ID_Length?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_ID_Length = null;
                    return;
                }

                value = value.Trim().Replace(',', '.');

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    TJ_ID_Length = result;
                }
            }
        }

        public string Grade
        {
            get => Model.Grade;
            set
            {
                if (Model.Grade != value)
                {
                    Model.Grade = value;
                    OnPropertyChanged();
                }
            }
        }

        public System.Collections.Generic.List<string> AvailableGrades => ToolJointConfig.StandardGrades;

        private ComponentType _componentType;
        public ComponentType ComponentType
        {
            get => _componentType;
            set
            {
                if (SetProperty(ref _componentType, value))
                {
                    OnPropertyChanged(nameof(ShowToolIDLength));
                }
            }
        }

        public bool ShowToolIDLength => ComponentType != ComponentType.DC;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public ToolJointConfigViewModel(
            ToolJointConfig model,
            ComponentType componentType = ComponentType.DrillPipe,
            WellboreComponent? currentWellboreComponent = null
        )
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _componentType = componentType;
            _currentWellboreComponent = currentWellboreComponent;

            _tjOD = model.TJ_OD;
            _tjID = model.TJ_ID;
            _tjLength = model.TJ_Length;
            _weight = model.Weight;
            _tjIDLength = model.TJ_ID_Length;

            OnPropertyChanged(nameof(ShowToolIDLength));

            OnPropertyChanged(nameof(TJ_OD_String));
            OnPropertyChanged(nameof(TJ_ID_String));
            OnPropertyChanged(nameof(TJ_Length_String));
            OnPropertyChanged(nameof(Weight_String));
            OnPropertyChanged(nameof(TJ_ID_Length_String));
            OnPropertyChanged(nameof(Grade));
            OnPropertyChanged(nameof(AvailableGrades));

            SaveCommand = new RelayCommand(_ =>
            {
                var componentType = _currentWellboreComponent?.Component.ToString() ?? "Unknown";

                if (Model.TJ_ID.HasValue && Model.TJ_OD.HasValue && Model.TJ_ID >= Model.TJ_OD)
                {
                    MessageBox.Show(
                        $"Invalid Tool Joint dimensions:\n\n" +
                        $"- TJ ID: {Model.TJ_ID.Value}\n" +
                        $"- TJ OD: {Model.TJ_OD.Value}\n\n" +
                        $"TJ ID must be less than TJ OD.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                if (_currentWellboreComponent == null)
                    return;

                if (_currentWellboreComponent.Component == ComponentType.OpenHole)
                {
                    if (Model.TJ_OD.HasValue && Model.TJ_OD.Value >= _currentWellboreComponent.OD)
                    {
                        MessageBox.Show(
                            $"Invalid configuration against {componentType}:\n\n" +
                            $"- Tool Joint OD: {Model.TJ_OD.Value}\n" +
                            $"- {componentType} OD: {_currentWellboreComponent.OD}\n\n" +
                            $"Tool Joint OD must be less than the {componentType} OD.",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    if (Model.TJ_OD.HasValue && Model.TJ_OD.Value >= _currentWellboreComponent.ID)
                    {
                        MessageBox.Show(
                            $"Invalid configuration against {componentType}:\n\n" +
                            $"- Tool Joint OD: {Model.TJ_OD.Value}\n" +
                            $"- {componentType} ID: {_currentWellboreComponent.ID}\n\n" +
                            $"Tool Joint OD must be less than the {componentType} ID.",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }

                RequestClose?.Invoke(true);
            });

            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

    }
}
