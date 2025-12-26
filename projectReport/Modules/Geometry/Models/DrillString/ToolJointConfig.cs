using System;
using System.Collections.Generic;
using ProjectReport.Models;

namespace ProjectReport.Models.Geometry.DrillString
{
    public class ToolJointConfig : BaseModel
    {
        private double? _tjOD;
        private double? _tjID;
        private double? _tjLength;
        private double? _weight;
        private string _grade = "S-135";
        private bool _hasFloatSub = false;

        /// <summary>
        /// Standard API drill pipe grades
        /// </summary>
        public static readonly List<string> StandardGrades = new List<string>
        {
            "E-75",
            "X-95",
            "G-105",
            "S-135",
            "V-150"
        };

        /// <summary>
        /// Tool Joint Outer Diameter (inches) - OPTIONAL
        /// Leave null if not configured
        /// </summary>
        public double? TJ_OD
        {
            get => _tjOD;
            set => SetProperty(ref _tjOD, value);
        }

        /// <summary>
        /// Tool Joint Inner Diameter (inches) - OPTIONAL
        /// Leave null if not configured
        /// Must be less than TJ_OD if both are specified
        /// </summary>
        public double? TJ_ID
        {
            get => _tjID;
            set => SetProperty(ref _tjID, value);
        }

        /// <summary>
        /// Tool Joint Length (feet) - OPTIONAL
        /// Leave null if not configured
        /// </summary>
        public double? TJ_Length
        {
            get => _tjLength;
            set => SetProperty(ref _tjLength, value);
        }

        /// <summary>
        /// Tool Joint Weight - OPTIONAL
        /// Leave null if not configured
        /// </summary>
        public double? Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        private double? _tjIDLength;
        /// <summary>
        /// Tool Joint ID Length - OPTIONAL
        /// </summary>
        public double? TJ_ID_Length
        {
            get => _tjIDLength;
            set => SetProperty(ref _tjIDLength, value);
        }

        /// <summary>
        /// Drill pipe grade (API standard)
        /// Common grades: E-75, X-95, G-105, S-135, V-150
        /// </summary>
        public string Grade
        {
            get => _grade;
            set => SetProperty(ref _grade, value);
        }

        /// <summary>
        /// Indicates if this component has a float sub installed
        /// Float subs prevent backflow and reduce surge pressure
        /// </summary>
        public bool HasFloatSub
        {
            get => _hasFloatSub;
            set => SetProperty(ref _hasFloatSub, value);
        }
    }
}
