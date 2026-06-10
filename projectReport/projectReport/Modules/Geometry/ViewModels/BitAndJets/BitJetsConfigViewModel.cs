using System;
using System.Windows.Input;
using ProjectReport.Models.Geometry;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Geometry.BitAndJets
{
    public class BitJetsConfigViewModel : BaseViewModel
    {
        public BitJetsConfig Model { get; }

        public int NumberOfJets
        {
            get => Model.NumberOfJets;
            set
            {
                if (Model.NumberOfJets != value)
                {
                    Model.NumberOfJets = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DiameterIn32nds
        {
            get => Model.DiameterIn32nds;
            set
            {
                if (Model.DiameterIn32nds != value)
                {
                    Model.DiameterIn32nds = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TFA => Model.TFA;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public BitJetsConfigViewModel(BitJetsConfig model)
        {
            Model = model ?? new BitJetsConfig();
            
            // Subscribe to model changes to update calculated properties
            Model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BitJetsConfig.TFA))
                {
                    OnPropertyChanged(nameof(TFA));
                }
            };

            SaveCommand = new RelayCommand(_ =>
            {
                if (NumberOfJets <= 0)
                {
                    System.Windows.MessageBox.Show("Number of jets must be greater than zero.", "Validation Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (DiameterIn32nds <= 0)
                {
                    System.Windows.MessageBox.Show("Diameter must be greater than zero.", "Validation Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                RequestClose?.Invoke(true);
            });

            CancelCommand = new RelayCommand(_ =>
            {
                RequestClose?.Invoke(false);
            });
        }
    }
}
