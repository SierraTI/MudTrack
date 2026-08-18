using ProjectReport.Models.Rig;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolSystemPit : INotifyPropertyChanged
    {
        // ============================================================
        // IDENTIDAD
        // ============================================================

        public Guid Uid { get; private set; }

        // ============================================================
        // EVENT FLUID SYSTEM ID
        // ============================================================

        private int? _eventFluidSystemId;

        public int? EventFluidSystemId
        {
            get => _eventFluidSystemId;

            set
            {
                SetProperty(
                    ref _eventFluidSystemId,
                    value);
            }
        }

        // ============================================================
        // PRIVATE FIELDS
        // ============================================================

        private int _pitId;

        private string _pitName = string.Empty;

        private int? _pitSystemId;

        private int? _fluidTypeId;

        private string _fluidType = string.Empty;

        private string _fluidSubtype = string.Empty;

        private double? _previousVolume;

        private double? _currentVolume;

        private double? _density;

        private string _previousVolumeText = string.Empty;

        private string _currentVolumeText = string.Empty;

        private string _densityText = string.Empty;

        private RigPit? _sourcePit;

        // ============================================================
        // ESTADOS DE MODIFICACIÓN
        // ============================================================

        private bool _isPitSystemModified;

        private bool _isFluidSubtypeModified;

        private bool _isCurrentVolumeModified;

        private bool _isDensityModified;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public VolSystemPit()
        {
            Uid = Guid.NewGuid();
        }

        // ============================================================
        // COPY CONSTRUCTOR
        // ============================================================

        public VolSystemPit(
            VolSystemPit source)
        {
            if (source == null)
                throw new ArgumentNullException(
                    nameof(source));

            Uid = source.Uid;

            _eventFluidSystemId =
                source.EventFluidSystemId;

            _pitId =
                source.PitId;

            _pitName =
                source.PitName;

            _pitSystemId =
                source.PitSystemId;

            _fluidTypeId =
                source.FluidTypeId;

            _fluidType =
                source.FluidType;

            _fluidSubtype =
                source.FluidSubtype;

            _previousVolume =
                source.PreviousVolume;

            _previousVolumeText =
                source.PreviousVolumeText;

            _currentVolume =
                source.CurrentVolume;

            _currentVolumeText =
                source.CurrentVolumeText;

            _density =
                source.Density;

            _densityText =
                source.DensityText;

            _sourcePit =
                source.SourcePit;

            _isPitSystemModified =
                source.IsPitSystemModified;

            _isFluidSubtypeModified =
                source.IsFluidSubtypeModified;

            _isCurrentVolumeModified =
                source.IsCurrentVolumeModified;

            _isDensityModified =
                source.IsDensityModified;
        }

        // ============================================================
        // PIT ID
        // ============================================================

        public int PitId
        {
            get => _pitId;

            set
            {
                SetProperty(
                    ref _pitId,
                    value);
            }
        }

        // ============================================================
        // PIT NAME
        // ============================================================

        public string PitName
        {
            get => _pitName;

            set
            {
                SetProperty(
                    ref _pitName,
                    value);
            }
        }

        // ============================================================
        // PIT SYSTEM
        // ============================================================

        public int? PitSystemId
        {
            get => _pitSystemId;

            set
            {
                if (!SetProperty(
                    ref _pitSystemId,
                    value))
                {
                    return;
                }

                IsPitSystemModified = true;

                OnPropertyChanged(
                    nameof(FluidDisplay));
            }
        }

        public bool IsPitSystemModified
        {
            get => _isPitSystemModified;

            private set
            {
                SetProperty(
                    ref _isPitSystemModified,
                    value);
            }
        }

        // ============================================================
        // RESTAURAR PIT SYSTEM SIN MARCAR COMO MODIFICADO
        // ============================================================

        public void RestorePitSystemId(
            int? value)
        {
            if (_pitSystemId == value)
                return;

            _pitSystemId = value;

            OnPropertyChanged(
                nameof(PitSystemId));

            OnPropertyChanged(
                nameof(FluidDisplay));
        }

        // ============================================================
        // FLUID TYPE ID
        // ============================================================

        public int? FluidTypeId
        {
            get => _fluidTypeId;

            set
            {
                if (!SetProperty(
                    ref _fluidTypeId,
                    value))
                {
                    return;
                }

                OnPropertyChanged(
                    nameof(FluidDisplay));
            }
        }

        // ============================================================
        // FLUID TYPE
        // ============================================================

        public string FluidType
        {
            get => _fluidType;

            set
            {
                if (!SetProperty(
                    ref _fluidType,
                    value))
                {
                    return;
                }

                OnPropertyChanged(
                    nameof(FluidDisplay));
            }
        }

        // ============================================================
        // FLUID SUBTYPE
        // ============================================================

        public string FluidSubtype
        {
            get => _fluidSubtype;

            set
            {
                if (!SetProperty(
                    ref _fluidSubtype,
                    value))
                {
                    return;
                }

                IsFluidSubtypeModified = true;

                OnPropertyChanged(
                    nameof(FluidDisplay));
            }
        }

        public bool IsFluidSubtypeModified
        {
            get => _isFluidSubtypeModified;

            private set
            {
                SetProperty(
                    ref _isFluidSubtypeModified,
                    value);
            }
        }

        // ============================================================
        // PREVIOUS VOLUME
        // ============================================================

        public double? PreviousVolume
        {
            get => _previousVolume;

            set
            {
                if (!SetProperty(
                    ref _previousVolume,
                    value))
                {
                    return;
                }

                UpdatePreviousVolumeText();

                OnPropertyChanged(
                    nameof(PreviousVolumeText));
            }
        }

        // ============================================================
        // PREVIOUS VOLUME TEXT
        // ============================================================

        public string PreviousVolumeText
        {
            get => _previousVolumeText;

            set
            {
                if (!SetProperty(
                    ref _previousVolumeText,
                    value))
                {
                    return;
                }

                ParsePreviousVolume(value);
            }
        }

        private void ParsePreviousVolume(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (_previousVolume != null)
                {
                    _previousVolume = null;

                    OnPropertyChanged(
                        nameof(PreviousVolume));
                }

                return;
            }

            string normalized =
                value.Trim()
                    .Replace(',', '.');

            if (double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double result))
            {
                if (!_previousVolume.HasValue ||
                    _previousVolume.Value != result)
                {
                    _previousVolume = result;

                    OnPropertyChanged(
                        nameof(PreviousVolume));
                }
            }
        }

        private void UpdatePreviousVolumeText()
        {
            _previousVolumeText =
                _previousVolume.HasValue
                    ? _previousVolume.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : string.Empty;
        }

        // ============================================================
        // CURRENT VOLUME
        // ============================================================

        public double? CurrentVolume
        {
            get => _currentVolume;

            set
            {
                if (!SetProperty(
                    ref _currentVolume,
                    value))
                {
                    return;
                }

                IsCurrentVolumeModified = true;

                UpdateCurrentVolumeText();

                OnPropertyChanged(
                    nameof(CurrentVolumeText));
            }
        }

        public bool IsCurrentVolumeModified
        {
            get => _isCurrentVolumeModified;

            private set
            {
                SetProperty(
                    ref _isCurrentVolumeModified,
                    value);
            }
        }

        // ============================================================
        // CURRENT VOLUME TEXT
        // ============================================================

        public string CurrentVolumeText
        {
            get => _currentVolumeText;

            set
            {
                if (!SetProperty(
                    ref _currentVolumeText,
                    value))
                {
                    return;
                }

                ParseCurrentVolume(value);
            }
        }

        private void ParseCurrentVolume(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (_currentVolume != null)
                {
                    _currentVolume = null;

                    OnPropertyChanged(
                        nameof(CurrentVolume));
                }

                IsCurrentVolumeModified = true;

                return;
            }

            string normalized =
                value.Trim()
                    .Replace(',', '.');

            if (double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double result))
            {
                if (!_currentVolume.HasValue ||
                    _currentVolume.Value != result)
                {
                    _currentVolume = result;

                    OnPropertyChanged(
                        nameof(CurrentVolume));
                }

                IsCurrentVolumeModified = true;
            }
        }

        private void UpdateCurrentVolumeText()
        {
            _currentVolumeText =
                _currentVolume.HasValue
                    ? _currentVolume.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : string.Empty;
        }

        // ============================================================
        // DENSITY
        // ============================================================

        public double? Density
        {
            get => _density;

            set
            {
                if (!SetProperty(
                    ref _density,
                    value))
                {
                    return;
                }

                IsDensityModified = true;

                UpdateDensityText();

                OnPropertyChanged(
                    nameof(DensityText));
            }
        }

        public bool IsDensityModified
        {
            get => _isDensityModified;

            private set
            {
                SetProperty(
                    ref _isDensityModified,
                    value);
            }
        }

        // ============================================================
        // DENSITY TEXT
        // ============================================================

        public string DensityText
        {
            get => _densityText;

            set
            {
                if (!SetProperty(
                    ref _densityText,
                    value))
                {
                    return;
                }

                ParseDensity(value);
            }
        }

        private void ParseDensity(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (_density != null)
                {
                    _density = null;

                    OnPropertyChanged(
                        nameof(Density));
                }

                IsDensityModified = true;

                return;
            }

            string normalized =
                value.Trim()
                    .Replace(',', '.');

            if (double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double result))
            {
                if (!_density.HasValue ||
                    _density.Value != result)
                {
                    _density = result;

                    OnPropertyChanged(
                        nameof(Density));
                }

                IsDensityModified = true;
            }
        }

        private void UpdateDensityText()
        {
            _densityText =
                _density.HasValue
                    ? _density.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : string.Empty;
        }

        // ============================================================
        // CARGAR VALORES DESDE SQLITE
        // ============================================================

        public void LoadDatabaseValues(
            int? eventFluidSystemId,
            double? previousVolume,
            double? currentVolume,
            double? density)
        {
            _eventFluidSystemId =
                eventFluidSystemId;

            _previousVolume =
                previousVolume;

            _previousVolumeText =
                previousVolume.HasValue
                    ? previousVolume.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : string.Empty;

            _currentVolume =
                currentVolume;

            _currentVolumeText =
                currentVolume.HasValue
                    ? currentVolume.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : string.Empty;

            _density =
                density;

            _densityText =
                density.HasValue
                    ? density.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : string.Empty;

            _isPitSystemModified = false;

            _isFluidSubtypeModified = false;

            _isCurrentVolumeModified = false;

            _isDensityModified = false;

            OnPropertyChanged(
                nameof(EventFluidSystemId));

            OnPropertyChanged(
                nameof(PreviousVolume));

            OnPropertyChanged(
                nameof(PreviousVolumeText));

            OnPropertyChanged(
                nameof(CurrentVolume));

            OnPropertyChanged(
                nameof(CurrentVolumeText));

            OnPropertyChanged(
                nameof(Density));

            OnPropertyChanged(
                nameof(DensityText));

            OnPropertyChanged(
                nameof(IsPitSystemModified));

            OnPropertyChanged(
                nameof(IsFluidSubtypeModified));

            OnPropertyChanged(
                nameof(IsCurrentVolumeModified));

            OnPropertyChanged(
                nameof(IsDensityModified));
        }

        // ============================================================
        // MARCAR COMO GUARDADO
        // ============================================================

        public void MarkDatabaseValuesAsSaved()
        {
            _isPitSystemModified = false;

            _isFluidSubtypeModified = false;

            _isCurrentVolumeModified = false;

            _isDensityModified = false;

            OnPropertyChanged(
                nameof(IsPitSystemModified));

            OnPropertyChanged(
                nameof(IsFluidSubtypeModified));

            OnPropertyChanged(
                nameof(IsCurrentVolumeModified));

            OnPropertyChanged(
                nameof(IsDensityModified));
        }

        // ============================================================
        // DISPLAY
        // ============================================================

        public string FluidDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(
                    FluidType) &&
                    string.IsNullOrWhiteSpace(
                        FluidSubtype))
                {
                    return string.Empty;
                }

                if (string.IsNullOrWhiteSpace(
                    FluidSubtype))
                {
                    return FluidType;
                }

                if (string.IsNullOrWhiteSpace(
                    FluidType))
                {
                    return FluidSubtype;
                }

                return
                    $"{FluidType} - {FluidSubtype}";
            }
        }

        // ============================================================
        // SOURCE PIT
        // ============================================================

        public RigPit? SourcePit
        {
            get => _sourcePit;

            set
            {
                SetProperty(
                    ref _sourcePit,
                    value);
            }
        }

        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]
            string? propertyName = null)
        {
            if (
                EqualityComparer<T>.Default.Equals(
                    field,
                    value))
            {
                return false;
            }

            field = value;

            OnPropertyChanged(
                propertyName);

            return true;
        }

        protected void OnPropertyChanged(
            string? propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}