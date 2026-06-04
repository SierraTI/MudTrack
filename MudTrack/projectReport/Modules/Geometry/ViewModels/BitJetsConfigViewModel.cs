using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ProjectReport.Models.Geometry;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Geometry.Config
{
    public class BitJetsConfigViewModel : BaseViewModel
    {
        public BitJetsConfig Model { get; }

        // Lista de jets (uno por fila)
        public ObservableCollection<JetItem> Jets { get; } = new();

        // Número de jets controla el tamaño de la lista
        private int _numberOfJets;
        public int NumberOfJets
        {
            get => _numberOfJets;
            set
            {
                if (SetProperty(ref _numberOfJets, value))
                {
                    SyncJets();
                    Model.NumberOfJets = _numberOfJets; // mantén Model actualizado si lo usas en otras partes
                    OnPropertyChanged(nameof(TotalTFA));
                }
            }
        }

        // KPI total
        public double TotalTFA => Jets.Sum(j => j.TFA);

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public BitJetsConfigViewModel(BitJetsConfig model)
        {
            Model = model ?? new BitJetsConfig();

            // Inicializa NumberOfJets desde el model
            _numberOfJets = Model.NumberOfJets <= 0 ? 1 : Model.NumberOfJets;
            SyncJets();

            // Si tu Model tiene un diámetro único previo, puedes precargarlo en todos los jets (opcional)
            if (Model.DiameterIn32nds > 0)
            {
                foreach (var j in Jets)
                    j.Diameter32 = Model.DiameterIn32nds;
            }

            // Recalcular TotalTFA cuando cambie cualquier jet
            Jets.CollectionChanged += (s, e) =>
            {
                HookJetEvents();
                OnPropertyChanged(nameof(TotalTFA));
            };
            HookJetEvents();

            SaveCommand = new RelayCommand(_ =>
            {
                if (NumberOfJets <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "Number of jets must be greater than zero.",
                        "Validation Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (Jets.Any(j => j.Diameter32 <= 0))
                {
                    System.Windows.MessageBox.Show(
                        "All jet diameters must be greater than zero.",
                        "Validation Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Guardar al model (si quieres persistir)
                Model.NumberOfJets = NumberOfJets;

                // Si tienes un diámetro base que quieres guardar en el modelo, descomenta y ajusta:
                // Model.DiameterIn32nds = (int)Math.Round(Jets.First().Diameter32);

                // El modelo gestiona su propia TFA (setter privado): pide al modelo que la recalcule
                Model.RecalculateTFA();

                RequestClose?.Invoke(true);
            });

            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        private void SyncJets()
        {
            if (NumberOfJets < 0) _numberOfJets = 0;

            while (Jets.Count < NumberOfJets)
                Jets.Add(new JetItem());

            while (Jets.Count > NumberOfJets)
                Jets.RemoveAt(Jets.Count - 1);
        }

        private void HookJetEvents()
        {
            foreach (var jet in Jets)
            {
                // evita doble suscripción
                jet.PropertyChanged -= Jet_PropertyChanged;
                jet.PropertyChanged += Jet_PropertyChanged;
            }
        }

        private void Jet_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Si cambia Diameter32, cambia TFA y por ende TotalTFA
            if (e.PropertyName == nameof(JetItem.Diameter32) || e.PropertyName == nameof(JetItem.TFA))
            {
                OnPropertyChanged(nameof(TotalTFA));
            }
        }
    }
}