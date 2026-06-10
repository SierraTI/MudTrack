using System;

namespace ProjectReport.Models.Rig
{
    public class ReportPumpOperation : BaseModel
    {
        private int _no;
        private string _pumpName = string.Empty;
        private double _linerSize;
        private double _strokeLength;
        private double _efficiency;
        private double _spm;
        private double _gpm;
        private double _pressure;

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
                    CalculateGpm();
            }
        }

        public double StrokeLength
        {
            get => _strokeLength;
            set
            {
                if (SetProperty(ref _strokeLength, value))
                    CalculateGpm();
            }
        }

        public double Efficiency
        {
            get => _efficiency;
            set
            {
                if (SetProperty(ref _efficiency, value))
                    CalculateGpm();
            }
        }

        public double Spm
        {
            get => _spm;
            set
            {
                if (SetProperty(ref _spm, value))
                    CalculateGpm();
            }
        }

        public double Gpm
        {
            get => _gpm;
            private set => SetProperty(ref _gpm, value);
        }

        public double Pressure
        {
            get => _pressure;
            set => SetProperty(ref _pressure, value);
        }

        private void CalculateGpm()
        {
            // Formula: GPM = 0.0102 * D^2 * L * SPM * Eff%
            // Based on Triplex pump standard constant
            double output = 0.0102 * Math.Pow(LinerSize, 2) * StrokeLength * Spm * (Efficiency / 100.0);
            Gpm = Math.Round(output, 2);
        }

        /// <summary>
        /// Updates operation specs from a RigPump definition
        /// </summary>
        public void UpdateFromRigPump(RigPump rigPump)
        {
            if (rigPump == null) return;
            
            PumpName = rigPump.PumpName;
            LinerSize = rigPump.LinerSize;
            StrokeLength = rigPump.StrokeLength;
            Efficiency = rigPump.Efficiency;
        }
    }
}
