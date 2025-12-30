using System;
using ProjectReport.Models;

namespace ProjectReport.Models.Geometry
{
    /// <summary>
    /// Represents a geological formation/zone for chart decoration
    /// </summary>
    public class Formation : BaseModel
    {
        private string _name = string.Empty;
        private double _topTVD;
        private double _bottomTVD;
        private string _color = "#F3F4FB"; // Default light gray (Gray-100)

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double TopTVD
        {
            get => _topTVD;
            set => SetProperty(ref _topTVD, value);
        }

        public double BottomTVD
        {
            get => _bottomTVD;
            set => SetProperty(ref _bottomTVD, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public Formation() { }

        public Formation(string name, double top, double bottom, string color)
        {
            _name = name;
            _topTVD = top;
            _bottomTVD = bottom;
            _color = color;
        }
    }
}
