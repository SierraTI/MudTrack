using System;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.ViewModels;

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
            get => Model.TJ_OD.HasValue ? Model.TJ_OD.Value.ToString("F2") : string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_OD = null;
                    return;
                }

                if (double.TryParse(value, out double result))
                    TJ_OD = result;
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
            get => Model.TJ_ID.HasValue ? Model.TJ_ID.Value.ToString("F2") : string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_ID = null;
                    return;
                }

                if (double.TryParse(value, out double result))
                    TJ_ID = result;
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
            get => Model.TJ_Length.HasValue ? Model.TJ_Length.Value.ToString("F2") : string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_Length = null;
                    return;
                }

                if (double.TryParse(value, out double result))
                    TJ_Length = result;
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
            get => Model.Weight.HasValue ? Model.Weight.Value.ToString("F2") : string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Weight = null;
                    return;
                }

                if (double.TryParse(value, out double result))
                    Weight = result;
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
            get => Model.TJ_ID_Length.HasValue ? Model.TJ_ID_Length.Value.ToString("F2") : string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TJ_ID_Length = null;
                    return;
                }

                if (double.TryParse(value, out double result))
                    TJ_ID_Length = result;
            }
        }

        /// <summary>
        /// Drill pipe grade (API standard)
        /// </summary>
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

        /// <summary>
        /// Available API standard grades for dropdown
        /// </summary>
        public System.Collections.Generic.List<string> AvailableGrades => ToolJointConfig.StandardGrades;

        /// <summary>
        /// Indicates if this component has a float sub installed
        /// </summary>
        public bool HasFloatSub
        {
            get => Model.HasFloatSub;
            set
            {
                if (Model.HasFloatSub != value)
                {
                    Model.HasFloatSub = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public ToolJointConfigViewModel(ToolJointConfig model, ComponentType componentType = ComponentType.DrillPipe)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _componentType = componentType;
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
            OnPropertyChanged(nameof(HasFloatSub));
            OnPropertyChanged(nameof(AvailableGrades));

            SaveCommand = new RelayCommand(_ =>
            {
                if (Model.TJ_ID.HasValue && Model.TJ_OD.HasValue && Model.TJ_ID >= Model.TJ_OD)
                {
                    MessageBox.Show("Tool Joint ID must be less than Tool Joint OD", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                RequestClose?.Invoke(true);
            });

            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }
    }
}

