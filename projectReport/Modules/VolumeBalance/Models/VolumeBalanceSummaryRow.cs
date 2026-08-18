using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolumeBalanceSummaryRow : INotifyPropertyChanged
    {
        // ============================================================
        // LABEL
        // ============================================================

        private string _label = string.Empty;

        public string Label
        {
            get => _label;

            set
            {
                if (_label == value)
                    return;

                _label = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // ACTIVE
        // ============================================================

        private double _active;

        public double Active
        {
            get => _active;

            set
            {
                if (_active == value)
                    return;

                _active = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // RESERVE
        // ============================================================

        private double _reserve;

        public double Reserve
        {
            get => _reserve;

            set
            {
                if (_reserve == value)
                    return;

                _reserve = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // OTHER
        // ============================================================

        private double _other;

        public double Other
        {
            get => _other;

            set
            {
                if (_other == value)
                    return;

                _other = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }

}
