using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProjectReport.Models;
using ProjectReport.Models.Geometry.DrillString;


namespace ProjectReport.Models.Geometry.Wellbore
{
    public class WellboreSection : BaseModel
    {
        private string _name = string.Empty;
        private double _od;
        private double _id;
        private double _topMd;
        private double _bottomMd;
        private ComponentType _component;
        private WellSectionType _wellSection;
        private double _volume;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double OD
        {
            get => _od;
            set
            {
                if (SetProperty(ref _od, value))
                {
                    CalculateVolume();
                }
            }
        }

        public double ID
        {
            get => _id;
            set
            {
                if (SetProperty(ref _id, value))
                {
                    CalculateVolume();
                }
            }
        }

        public double TopMD
        {
            get => _topMd;
            set
            {
                if (SetProperty(ref _topMd, value))
                {
                    CalculateVolume();
                    OnPropertyChanged(nameof(Length));
                }
            }
        }

        public double BottomMD
        {
            get => _bottomMd;
            set
            {
                if (SetProperty(ref _bottomMd, value))
                {
                    CalculateVolume();
                    OnPropertyChanged(nameof(Length));
                }
            }
        }

        /// <summary>
        /// Component type (Casing, Liner, OpenHole, etc.)
        /// Renamed from SectionType for clarity
        /// </summary>
        public ComponentType Component
        {
            get => _component;
            set
            {
                if (SetProperty(ref _component, value))
                {
                    // If component type is OpenHole, set ID to 0
                    if (_component == ComponentType.OpenHole)
                    {
                        ID = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Well section classification (Riser, Surface Casing, etc.)
        /// </summary>
        public WellSectionType WellSection
        {
            get => _wellSection;
            set => SetProperty(ref _wellSection, value);
        }

        public double Volume
        {
            get => _volume;
            private set => SetProperty(ref _volume, value);
        }

        public double Length => BottomMD - TopMD;

        private void CalculateVolume()
        {
            // SRS Section 4.5: Wellbore Annular Volume Formula
            // Volume (bbl) = (ID² / 1029.4) × Length
            // Where:
            // - ID = Inner Diameter of wellbore section (inches)
            // - Length = Bottom MD - Top MD (feet)
            // - 1029.4 = Conversion constant for bbl/ft from in²
            
            if (BottomMD > TopMD && ID > 0)
            {
                double length = BottomMD - TopMD; // Length in feet
                Volume = (ID * ID / 1029.4) * length;
            }
            else
            {
                Volume = 0;
            }
        }
    }
}
