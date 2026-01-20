using System;
using System.Collections.ObjectModel;
using ProjectReport.Models;

namespace ProjectReport.Models.Rig
{
    public class RigProfile : BaseModel
    {
        private string _rigName = string.Empty;
        private string _contractor = string.Empty;
        private string _rigType = string.Empty;
        private double _rkbElevation;
        private double _casingHeadElevation;

        // General Rig Properties
        public string RigName
        {
            get => _rigName;
            set => SetProperty(ref _rigName, value);
        }

        public string Contractor
        {
            get => _contractor;
            set => SetProperty(ref _contractor, value);
        }

        public string RigType
        {
            get => _rigType;
            set => SetProperty(ref _rigType, value);
        }

        public double RkbElevation
        {
            get => _rkbElevation;
            set => SetProperty(ref _rkbElevation, value);
        }

        public double CasingHeadElevation
        {
            get => _casingHeadElevation;
            set => SetProperty(ref _casingHeadElevation, value);
        }

        // Collections
        public ObservableCollection<RigSurfaceEquipment> SurfaceEquipment { get; set; } = new ObservableCollection<RigSurfaceEquipment>();
        public ObservableCollection<RigPump> Pumps { get; set; } = new ObservableCollection<RigPump>();
        public ObservableCollection<RigSolidsControl> SolidsControl { get; set; } = new ObservableCollection<RigSolidsControl>();
        public ObservableCollection<RigPit> Pits { get; set; } = new ObservableCollection<RigPit>();

        // Nueva colección: Service Line (mismos campos que SurfaceEquipment)
        public ObservableCollection<RigSurfaceEquipment> ServiceLine { get; set; } = new ObservableCollection<RigSurfaceEquipment>();
    }

    public class RigSurfaceEquipment : BaseModel
    {
        private int _no;
        private string _component = string.Empty;
        private double _internalDiameter;
        private double _length;
        private string _description = string.Empty;

        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        public string Component
        {
            get => _component;
            set => SetProperty(ref _component, value);
        }

        public double InternalDiameter
        {
            get => _internalDiameter;
            set => SetProperty(ref _internalDiameter, value);
        }

        public double Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private double _frictionCoefficient;
        public double FrictionCoefficient
        {
            get => _frictionCoefficient;
            set => SetProperty(ref _frictionCoefficient, value);
        }
    }

    public class RigPump : BaseModel
    {
        private int _no;
        private string _pumpName = string.Empty;
        private double _linerSize;
        private double _strokeLength;
        private double _efficiency;

        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        public string PumpName
        {
            get => _pumpName;
            set => SetProperty(ref _pumpName, value);
        }

        public double LinerSize
        {
            get => _linerSize;
            set
            {
                if (SetProperty(ref _linerSize, value))
                    OnPropertyChanged(nameof(Output));
            }
        }

        public double StrokeLength
        {
            get => _strokeLength;
            set
            {
                if (SetProperty(ref _strokeLength, value))
                    OnPropertyChanged(nameof(Output));
            }
        }

        public double Efficiency
        {
            get => _efficiency;
            set
            {
                if (SetProperty(ref _efficiency, value))
                    OnPropertyChanged(nameof(Output));
            }
        }

        /// <summary>
        /// Calculated Output in bbl/stk using formula: 0.000243 × ID² × Stroke × Efficiency
        /// </summary>
        public double Output
        {
            get
            {
                if (LinerSize <= 0 || StrokeLength <= 0 || Efficiency <= 0)
                    return 0;
                
                return Math.Round(0.000243 * Math.Pow(LinerSize, 2) * StrokeLength * (Efficiency / 100.0), 4);
            }
        }
    }

    public class RigSolidsControl : BaseModel
    {
        private int _no;
        private string _type = string.Empty; // Shaker, Centrifuge, etc.
        private string _manufacturer = string.Empty;
        private string _model = string.Empty;
        private double _gpmCapacity;
        private int _numberOfScreens;

        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string Manufacturer
        {
            get => _manufacturer;
            set => SetProperty(ref _manufacturer, value);
        }

        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// Capacidad nominal en GPM (valor heredado / calculado desde catálogo).
        /// </summary>
        public double GpmCapacity
        {
            get => _gpmCapacity;
            set => SetProperty(ref _gpmCapacity, value);
        }

        public int NumberOfScreens
        {
            get => _numberOfScreens;
            set => SetProperty(ref _numberOfScreens, value);
        }

        private string _screenType = string.Empty;
        public string ScreenType
        {
            get => _screenType;
            set => SetProperty(ref _screenType, value);
        }

        // Nueva propiedades solicitadas para la tabla de solids control
        private string _style = string.Empty;
        public string Style
        {
            get => _style;
            set => SetProperty(ref _style, value);
        }

        private int _desilterNumberOfCones;
        public int DesilterNumberOfCones
        {
            get => _desilterNumberOfCones;
            set => SetProperty(ref _desilterNumberOfCones, value);
        }

        private double _desilterConeSize;
        public double DesilterConeSize
        {
            get => _desilterConeSize;
            set => SetProperty(ref _desilterConeSize, value);
        }

        private int _desanderNumberOfCones;
        public int DesanderNumberOfCones
        {
            get => _desanderNumberOfCones;
            set => SetProperty(ref _desanderNumberOfCones, value);
        }

        private double _desanderConeSize;
        public double DesanderConeSize
        {
            get => _desanderConeSize;
            set => SetProperty(ref _desanderConeSize, value);
        }

        private int _nominalRpm;
        public int NominalRpm
        {
            get => _nominalRpm;
            set => SetProperty(ref _nominalRpm, value);
        }

        // Campo adicional explícito para "Cap flow (gpm)" — se mantiene junto a GpmCapacity por compatibilidad
        private double _capFlowGpm;
        public double CapFlowGpm
        {
            get => _capFlowGpm;
            set => SetProperty(ref _capFlowGpm, value);
        }
    }

    public class RigPit : BaseModel
    {
        private int _no;
        private string _pitName = string.Empty;
        private string _shape = string.Empty; // Rectangular, Cylindrical, etc.
        private string _dimensions = string.Empty; // e.g., "20x10x8" or "Diameter: 10, Height: 8"
        private double _maxCapacity;

        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        public string PitName
        {
            get => _pitName;
            set => SetProperty(ref _pitName, value);
        }

        public string Shape
        {
            get => _shape;
            set => SetProperty(ref _shape, value);
        }

        public string Dimensions
        {
            get => _dimensions;
            set => SetProperty(ref _dimensions, value);
        }

        public double MaxCapacity
        {
            get => _maxCapacity;
            set => SetProperty(ref _maxCapacity, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private double _currentVolume;
        public double CurrentVolume
        {
            get => _currentVolume;
            set => SetProperty(ref _currentVolume, value);
        }
    }
}
