using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Geometry.BitAndJets;
using ProjectReport.Services.DrillString;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Geometry.BitAndJets
{
    public class MultiBitJetsConfigViewModel : BaseViewModel
    {
        public MultiBitJetsConfig Model { get; }

        public ObservableCollection<JetSet> JetSets { get; } = new();

        public double TfaTotal => Model.TfaTotal;

        public ICommand AddSetCommand { get; }
        public ICommand RemoveSetCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public MultiBitJetsConfigViewModel(MultiBitJetsConfig model)
        {
            Model = model ?? new MultiBitJetsConfig();

            foreach (var s in Model.JetSets)
            {
                s.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(JetSet.TFACalculated))
                        OnPropertyChanged(nameof(TfaTotal));
                };
                JetSets.Add(s);
            }

            AddSetCommand = new RelayCommand(_ =>
            {
                var newSet = new JetSet(Model.JetSets.Count + 1, null, null);
                newSet.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(JetSet.TFACalculated))
                        OnPropertyChanged(nameof(TfaTotal));
                };
                Model.AddJetSet(newSet);
                JetSets.Add(newSet);
                OnPropertyChanged(nameof(TfaTotal));
            });

            RemoveSetCommand = new RelayCommand(param =>
            {
                if (param is JetSet set)
                {
                    Model.RemoveJetSet(set.Id);
                    JetSets.Remove(set);
                    OnPropertyChanged(nameof(TfaTotal));
                }
            });

            SaveCommand = new RelayCommand(_ =>
            {
                var (ok, errs) = JetValidator.ValidateAllJetSets(Model.JetSets);
                if (!ok)
                {
                    // For simplicity show first error message
                    System.Windows.MessageBox.Show("Jet configuration has errors. Check input.", "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                RequestClose?.Invoke(true);
            });

            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }
    }
}
