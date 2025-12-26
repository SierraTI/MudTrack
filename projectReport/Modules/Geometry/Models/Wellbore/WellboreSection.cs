using System;
using System.ComponentModel;
using ProjectReport.Models;

namespace ProjectReport.Models.Geometry.Wellbore
{
    public class WellboreSection : BaseModel
    {
        private string _name = string.Empty;
        private double _od;
        private double _id;
        private double _topMd;
        private double _bottomMd;
        private WellboreSectionType _sectionType;
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

        public WellboreSectionType SectionType
        {
            get => _sectionType;
            set
            {
                if (SetProperty(ref _sectionType, value))
                {
                    // If section type is OpenHole, disable OD editing
                    if (_sectionType == WellboreSectionType.OpenHole)
                    {
                        OD = 0; // Or any default value for OpenHole
                    }
                }
            }
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
