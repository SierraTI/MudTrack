using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Views.Geometry
{
    public partial class ContinuityErrorDialog : Window, INotifyPropertyChanged
    {
        // Inicializados con null-forgiving: los constructores asignan valores reales.
        private WellboreComponent _previousSection = null!;
        private WellboreComponent _currentSection = null!;

        public ContinuityErrorDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ContinuityErrorDialog(WellboreComponent previousSection, WellboreComponent currentSection) : this()
        {
            _previousSection = previousSection ?? throw new ArgumentNullException(nameof(previousSection));
            _currentSection = currentSection ?? throw new ArgumentNullException(nameof(currentSection));

                // Calculate difference (nullable-aware)
                if (currentSection.TopMD.HasValue && previousSection.BottomMD.HasValue)
                    Difference = Math.Abs(currentSection.TopMD.Value - previousSection.BottomMD.Value);
                else
                    Difference = null;
        }

        #region Properties

        public string ErrorMessage =>
                $"The Top MD of Section '{_currentSection!.Name}' ({(_currentSection.TopMD.HasValue ? _currentSection.TopMD.Value.ToString("F2") + " ft" : "N/A")}) must be exactly equal " +
                $"to the Bottom MD of Section '{_previousSection!.Name}' ({(_previousSection.BottomMD.HasValue ? _previousSection.BottomMD.Value.ToString("F2") + " ft" : "N/A")}).\n\n" +
                $"Please correct the value to ensure wellbore continuity.";

        public int PreviousSectionId => _previousSection!.Id;
        public int CurrentSectionId => _currentSection!.Id;
            public double? PreviousBottomMD => _previousSection!.BottomMD;
            public double? CurrentTopMD => _currentSection!.TopMD;
            public double? Difference { get; }

        #endregion

        #region Event Handlers

        private void FixPreviousSection_Click(object sender, RoutedEventArgs e)
        {
            // Fix the previous section's Bottom MD to match current section's Top MD
            if (_previousSection != null && _currentSection != null)
            {   
                _previousSection.BottomMD = _currentSection.TopMD;
            }
            DialogResult = true;
            Close();
        }

        private void FixCurrentSection_Click(object sender, RoutedEventArgs e)
        {
            // Fix the current section's Top MD to match previous section's Bottom MD
            if (_previousSection != null && _currentSection != null)
            {
                _currentSection.TopMD = _previousSection.BottomMD;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
