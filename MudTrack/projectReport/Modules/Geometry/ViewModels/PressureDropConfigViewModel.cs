using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.FluidsAndPressure;
using ProjectReport.ViewModels;
using PressureDropConfigModel = ProjectReport.Models.Geometry.PressureDropConfig;

namespace ProjectReport.ViewModels.Geometry.Config
{
    public class PressureDropConfigViewModel : BaseViewModel
    {
        public PressureDropConfigModel Model { get; }

        public ObservableCollection<PressureDropPoint> Data => Model.Data;

        public double MudDensity
        {
            get => Model.MudDensity;
            set
            {
                if (Model.MudDensity != value)
                {
                    Model.MudDensity = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public PressureDropConfigViewModel(PressureDropConfigModel model)
        {
            Model = model;

            SaveCommand = new RelayCommand(_ => RequestClose?.Invoke(true));
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }
    }
}

