using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.ViewModels;

namespace ProjectReport.Modules.VolumeBalance.Views
{
    public partial class VolSystemView : UserControl
    {
        // ============================================================
        // VALIDACIÓN NUMÉRICA
        // ============================================================

        private static readonly Regex _regex =
            new Regex(@"^[0-9]+(\.[0-9]*)?$");


        // ============================================================
        // VIEWMODEL
        // ============================================================

        private VolSystemViewModel? _vm;


        // ============================================================
        // EXPONER VIEWMODEL
        // ============================================================

        public VolSystemViewModel? ViewModel =>
            _vm;


        // ============================================================
        // CONTROL PARA EVITAR REENTRADA
        // ============================================================

        private bool _isRestoringPitSystem;


        // ============================================================
        // CONSTRUCTOR NORMAL
        // ============================================================

        public VolSystemView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            DataContextChanged += OnDataContextChanged;
        }


        // ============================================================
        // CONSTRUCTOR CON EVENT ID
        // ============================================================

        public VolSystemView(
            int volumeBalanceEventId)
            : this()
        {
            // --------------------------------------------------------
            // CREAR VIEWMODEL
            // --------------------------------------------------------

            var vm =
                new VolSystemViewModel();

            // --------------------------------------------------------
            // ASIGNAR EVENT ID
            // --------------------------------------------------------

            vm.VolumeBalanceEventId =
                volumeBalanceEventId;

            // --------------------------------------------------------
            // GUARDAR REFERENCIA LOCAL
            // --------------------------------------------------------

            _vm =
                vm;

            // --------------------------------------------------------
            // ASIGNAR DATACONTEXT
            // --------------------------------------------------------

            DataContext =
                vm;
        }


        // ============================================================
        // CAMBIO DE DATACONTEXT
        // ============================================================

        private void OnDataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            _vm =
                e.NewValue as VolSystemViewModel;
        }


        // ============================================================
        // LOADED
        // ============================================================

        private void OnLoaded(
            object sender,
            RoutedEventArgs e)
        {
            _vm ??=
                DataContext as VolSystemViewModel;
        }


        // ============================================================
        // UNLOADED
        // ============================================================

        private void OnUnloaded(
            object sender,
            RoutedEventArgs e)
        {
            // --------------------------------------------------------
            // NO HACER DISPOSE AQUÍ.
            // --------------------------------------------------------

            // Intencionalmente vacío.
        }


        // ============================================================
        // CAMBIO DE PIT SYSTEM
        // ============================================================

        private void PitSystemComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isRestoringPitSystem)
                return;

            if (sender is not ComboBox comboBox)
                return;

            if (comboBox.DataContext
                is not VolSystemPit pit)
            {
                return;
            }

            if (_vm == null)
            {
                _vm =
                    DataContext as VolSystemViewModel;
            }

            if (_vm == null)
                return;

            // ========================================================
            // OBTENER BINDING
            // ========================================================

            BindingExpression?
                binding =
                    comboBox.GetBindingExpression(
                        ComboBox.SelectedValueProperty);

            if (binding == null)
                return;

            // ========================================================
            // OBTENER VALOR NUEVO
            // ========================================================

            int? newSystemId =
                null;

            if (comboBox.SelectedValue != null &&
                comboBox.SelectedValue !=
                    DependencyProperty.UnsetValue)
            {
                try
                {
                    newSystemId =
                        Convert.ToInt32(
                            comboBox.SelectedValue);
                }
                catch
                {
                    newSystemId = null;
                }
            }

            // ========================================================
            // VALOR ANTERIOR
            // ========================================================

            int? previousSystemId =
                pit.PitSystemId;

            // ========================================================
            // SI NO HAY CAMBIO REAL
            // ========================================================

            if (newSystemId ==
                previousSystemId)
            {
                return;
            }

            // ========================================================
            // VALIDAR
            // ========================================================

            bool allowed =
                _vm.TryChangePitSystem(
                    pit,
                    newSystemId);

            // ========================================================
            // CAMBIO NO PERMITIDO
            // ========================================================

            if (!allowed)
            {
                try
                {
                    _isRestoringPitSystem = true;

                    // ------------------------------------------------
                    // IMPORTANTE:
                    //
                    // NO modificamos el modelo.
                    //
                    // Solamente hacemos que el ComboBox vuelva a
                    // mostrar el valor que tenía anteriormente.
                    // ------------------------------------------------

                    binding.UpdateTarget();
                }
                finally
                {
                    _isRestoringPitSystem = false;
                }

                return;
            }

            // ========================================================
            // CAMBIO PERMITIDO
            // ========================================================

            // --------------------------------------------------------
            // Ahora sí actualizamos el Source.
            //
            // Como el Binding usa UpdateSourceTrigger=Explicit,
            // hasta este punto PitSystemId NO se ha modificado.
            // --------------------------------------------------------

            binding.UpdateSource();
        }


        // ============================================================
        // VALIDACIÓN NUMÉRICA
        // ============================================================

        private void NumericOnly_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            string fullText =
                GetFullTextAfterInput(
                    tb,
                    e.Text);

            e.Handled =
                !_regex.IsMatch(fullText);
        }


        // ============================================================
        // OBTENER TEXTO DESPUÉS DE LA ENTRADA
        // ============================================================

        private string GetFullTextAfterInput(
            TextBox textBox,
            string input)
        {
            string text =
                textBox.Text;

            if (textBox.SelectionLength > 0)
            {
                text =
                    text.Remove(
                        textBox.SelectionStart,
                        textBox.SelectionLength);
            }

            text =
                text.Insert(
                    textBox.SelectionStart,
                    input);

            return text;
        }


        // ============================================================
        // PEGADO
        // ============================================================

        private void NumericOnly_Pasting(
            object sender,
            DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(
                typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            string text =
                (string)e.DataObject.GetData(
                    typeof(string));

            if (string.IsNullOrWhiteSpace(text) ||
                !_regex.IsMatch(text))
            {
                e.CancelCommand();
            }
        }


        // ============================================================
        // RESPONSIVE SCALE
        // ============================================================

        private void OnSizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            try
            {
                double width =
                    e.NewSize.Width;

                double scale =
                    width < 560 ? 0.78 :
                    width < 720 ? 0.88 :
                    width < 900 ? 0.96 :
                    1.0;

                if (FindName("GridScale")
                    is ScaleTransform st)
                {
                    st.ScaleX = scale;
                    st.ScaleY = scale;
                }
            }
            catch
            {
                // No interrumpir la interfaz.
            }
        }
    }
}