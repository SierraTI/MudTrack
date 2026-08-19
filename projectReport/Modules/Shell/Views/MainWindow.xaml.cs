using ProjectReport.Models;
using ProjectReport.Modules.ReportDetail.ViewModels;
using ProjectReport.Modules.ReportDetails.Views;
using ProjectReport.Modules.RigProfile.Views;
using ProjectReport.Modules.VolumeBalance.Services;
using ProjectReport.Modules.VolumeBalance.ViewModels;
using ProjectReport.Modules.VolumeBalance.Views;
using ProjectReport.Services;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;
using ProjectReport.ViewModels.Geometry;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Views.Geometry;
using ProjectReport.Views.Inventory;
using ProjectReport.Views.ReportWizard;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProjectReport.Views
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;

        // INSTANCIAS VOLUMENES
        private VolumeBalanceView? _volumeBalanceView;

        private VolumeBalanceViewModel?
            _volumeBalanceViewModel;

        private VolumeBalanceEventView?
            _volumeBalanceEventView;

        private readonly VolumeBalanceNavigationService
            _volumeNavigation;
        //------------------------------------------------------------

        private int? _currentWellId;
        private Well _currentWell;

        private void VolumeBalanceButton_Click(
      object sender,
      RoutedEventArgs e)
        {
            if (_currentWellId == null || _currentWell == null)
            {
                MessageBox.Show(
                    "Debe seleccionar un Well primero.");

                return;
            }

            if (_volumeBalanceView == null)
            {
                _volumeBalanceView =
                    new VolumeBalanceView();
            }

            _volumeBalanceViewModel =
                new VolumeBalanceViewModel(
                    _volumeNavigation,
                    _currentWellId.Value,
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    "Day");

            _volumeBalanceView.DataContext =
                _volumeBalanceViewModel;

            ContentTitle.Text =
                $"Volume Balance - {_currentWell.WellName}";

            ContentArea.Content =
                _volumeBalanceView;
        }

        private void OpenVolumeBalanceEvent(
      VolumeBalanceEvent evento)
        {
            if (evento == null)
                return;

            Debug.WriteLine(
                "========================================");

            Debug.WriteLine(
                "[MainWindow] OPEN VOLUME BALANCE EVENT");

            Debug.WriteLine(
                $"EventNo = {evento.EventNo}");

            Debug.WriteLine(
                $"EventId = {evento.VolumeBalanceEventId}");

            Debug.WriteLine(
                $"VolumeBalanceId = {evento.VolumeBalanceId}");

            Debug.WriteLine(
                $"EventDateTime = {evento.EventDateTime}");

            Debug.WriteLine(
                "========================================");

            // ============================================================
            // CREAR UNA NUEVA VISTA PARA CADA EVENTO
            // ============================================================

            _volumeBalanceEventView =
                new VolumeBalanceEventView();

            // ============================================================
            // ASIGNAR EL EVENTO SELECCIONADO
            // ============================================================

            _volumeBalanceEventView.DataContext =
                evento;

            // ============================================================
            // MOSTRAR
            // ============================================================

            ContentTitle.Text =
                $"Volume Balance Event #{evento.EventNo}";

            ContentArea.Content =
                _volumeBalanceEventView;
        }

        private void RegisterVolumeBalanceNavigation()
        {
            _volumeNavigation.NavigateToEventRequested += OpenVolumeBalanceEvent;
        }

        private GeometryView? _geometryView;
        private HomeView? _homeView;
        private WellDataView? _wellDataView;
        private Views.WellDashboardView? _wellDashboardView;
        private RigProfileView? _rigProfileView;

        public Project CurrentProject { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _volumeNavigation = new VolumeBalanceNavigationService();

            // ✅ IMPORTANTE: registrar navegación
            RegisterVolumeBalanceNavigation();

            _databaseService = new DatabaseService();
            _currentWell = new Well();

            // Demo project
            CurrentProject = new Project
            {
                Name = "Y-23A",
                WellName = "Well-04"
            };

            DataContext = this;

            NavigationService.Instance.NavigationRequested += OnNavigationRequested;

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += Timer_Tick;
            timer.Start();

            NavigateToHome();
        }


        private void SetTopMenuButtonsVisibility(Visibility visibility)
        {
            HomeButton.Visibility = visibility;
            GeometryButton.Visibility = visibility;
            InventoryButton.Visibility = visibility;
            ReportDetailButton.Visibility = visibility;
            VolumeBalanceButton.Visibility = visibility;
            ContentTitle.Visibility = visibility;

            if (ContentIndicator != null)
                ContentIndicator.Visibility = visibility;
        }


        private void Timer_Tick(object? sender, EventArgs e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        //==========================================
        // NAVIGATION SYSTEM
        //==========================================

        private void OnNavigationRequested(object? sender, NavigationEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.Target)
                {
                    case NavigationTarget.Home:
                        NavigateToHome();
                        break;

                    case NavigationTarget.WellData:
                        if (e.WellId.HasValue)
                            NavigateToWellData(e.WellId.Value);
                        break;

                    case NavigationTarget.Geometry:
                        if (e.WellId.HasValue)
                            NavigateToGeometry(e.WellId.Value);
                        break;

                    case NavigationTarget.WellDashboard:
                        if (e.WellId.HasValue)
                            NavigateToWellDashboard(e.WellId.Value);
                        break;

                    case NavigationTarget.RigProfile:
                        if (e.WellId.HasValue)
                            NavigateToRigProfile(e.WellId.Value);
                        break;
                }
            });
        }

        private void NavigateToHome()
        {
            SaveGeometryDataIfNeeded();

            if (_homeView == null)
            {
                _homeView = new HomeView();
                var vm = new ProjectReport.ViewModels.HomeViewModel(CurrentProject);
                _homeView.DataContext = vm;
            }

            ContentTitle.Text = "Home";
            ContentArea.Content = _homeView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;


            SetTopMenuButtonsVisibility(Visibility.Collapsed);

        }


        private void NavigateToWellData(int wellId)
        {
            SaveGeometryDataIfNeeded();

            // Load full well from DB
            var well = WellContextService.Instance.GetAllWells().FirstOrDefault(w => w.Id == wellId);
            if (well == null)
                well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null) return;

            WellContextService.Instance.CurrentWell = well;
            _currentWellId = wellId;
            _currentWell = well;

            _wellDataView = new WellDataView();
            var vm = new ProjectReport.ViewModels.WellDataViewModel(CurrentProject);
            vm.LoadWell(well);
            _wellDataView.DataContext = vm;

            ContentTitle.Text = $"Well Data - {well.WellName}";
            ContentArea.Content = _wellDataView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
        }


        private void NavigateToGeometry(int wellId)
        {
            // Load fresh well from database instead of using potentially stale in-memory data
            var well = WellContextService.Instance.GetAllWells().FirstOrDefault(w => w.Id == wellId);
            if (well == null) return;

            if (_geometryView == null)
                _geometryView = new GeometryView();

            if (_geometryView.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.LoadWell(well);

            ContentTitle.Text = $"Geometry - {well.WellName}";
            ContentArea.Content = _geometryView;

            GeometrySubmenu.Visibility = Visibility.Visible;

        }

        private void NavigateToWellDashboard(int wellId)
        {
            // Always load the full well from DB
            var well = WellContextService.Instance.CurrentWell?.Id == wellId
                ? WellContextService.Instance.CurrentWell
                : WellContextService.Instance.GetAllWells().FirstOrDefault(w => w.Id == wellId);

            if (well == null) return;

            // Set as current well — this triggers DB load of reports, geometry, etc.
            WellContextService.Instance.CurrentWell = well;

            _currentWellId = wellId;
            _currentWell = well;

            _wellDashboardView = new Views.WellDashboardView();
            var vm = new ProjectReport.ViewModels.WellDashboardViewModel(CurrentProject);
            _ = vm.LoadWell(well);
            _wellDashboardView.DataContext = vm;

            vm.OpenReportDetailsRequested += (selectedWell) =>
            {
                var reportVM = new ProjectReport.Modules.ReportDetail.ViewModels.ReportDViewModel(selectedWell);

                reportVM.OnReportSaved += (s, newReport) =>
                {
                    NavigateToWellDashboard(selectedWell.Id);
                };

                var view = new ReportDetailsView(selectedWell);
                view.DataContext = reportVM;

                ContentArea.Content = view;
                ContentTitle.Text = $"Report Detail - {selectedWell.WellName}";
            };

            ContentTitle.Text = $"Dashboard - {well.WellName}";
            ContentArea.Content = _wellDashboardView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
            SetTopMenuButtonsVisibility(Visibility.Visible);
        }



        private void NavigateToRigProfile(int wellId)
        {
            // Load fresh well from database instead of using potentially stale in-memory data
            var well = WellContextService.Instance.GetAllWells().FirstOrDefault(w => w.Id == wellId);
            if (well == null) return;

            if (_rigProfileView == null)
                _rigProfileView = new RigProfileView();

            ContentTitle.Text = $"Rig Profile - {well.WellName}";
            ContentArea.Content = _rigProfileView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
        }

        //==========================================
        // INVENTORY
        //==========================================

        private InventoryService? _inventoryService;
        private InventoryDashboardView? _inventoryDashboardView;

        private void InventoryButton_Click(object sender, RoutedEventArgs e)
        {
            SaveGeometryDataIfNeeded();

            // Use the shared singleton InventoryService so all views share the same instance
            _inventoryService ??= ProjectReport.Services.ServiceLocator.InventoryService;

            if (_inventoryDashboardView == null)
            {
                _inventoryDashboardView = new InventoryDashboardView();

                // Use the shared service instance for the dashboard VM
                var vm = new InventoryProductsDashboardViewModel(_inventoryService);
                Debug.WriteLine($"DEBUG: MainWindow created InventoryProductsDashboardViewModel. Hash: {vm.GetHashCode()} Type: {vm.GetType().FullName}");

                // Aquí conectamos los botones del dashboard para abrir pantallas
                vm.RequestOpenReceived += () =>
                {
                    var view = new TicketReceivedView();
                    // Use the same shared service for the ticket VM
                    var vmr = new TicketReceivedViewModel(_inventoryService);
                    view.DataContext = vmr;

                    // Cuando cierren (después de guardar) volvemos al dashboard y refrescamos
                    vmr.RequestClose += () =>
                    {
                        ContentTitle.Text = $"Inventory - {_currentWell.WellName}";
                        ContentArea.Content = _inventoryDashboardView;

                        if (_inventoryDashboardView?.DataContext is InventoryProductsDashboardViewModel dvm)
                            dvm.LoadForDate(dvm.SelectedDate);
                    };

                    ContentTitle.Text = "Inventory - Ticket Received";
                    ContentArea.Content = view;
                };

                vm.RequestOpenHistory += () =>
                {
                    var view = new InventoryHistoryView();
                    view.DataContext = new InventoryHistoryViewModel(_inventoryService);

                    ContentTitle.Text = "Inventory - History";
                    ContentArea.Content = view;
                };

                vm.RequestOpenReturned += () =>
                {
                    var view = new TicketReturnedView();
                    var vmr = new TicketReturnedViewModel(_inventoryService);
                    view.DataContext = vmr;

                    vmr.RequestClose += () =>
                    {
                        ContentTitle.Text = $"Inventory - {_currentWell.WellName}";
                        ContentArea.Content = _inventoryDashboardView;

                        if (_inventoryDashboardView?.DataContext is InventoryProductsDashboardViewModel dvm)
                            dvm.LoadForDate(dvm.SelectedDate);
                    };

                    ContentTitle.Text = "Inventory - Ticket Returned";
                    ContentArea.Content = view;
                };

                // Suscribir edición por remisión (Received / Returned)
                vm.RequestEditReceivedByRequisition += (requisition) =>
                {
                    var view = new TicketReceivedView();
                    var vmr = new TicketReceivedViewModel(_inventoryService);
                    view.DataContext = vmr;

                    // Cargar por remisión
                    vmr.LoadByRequisition(requisition);

                    vmr.RequestClose += () =>
                    {
                        ContentTitle.Text = $"Inventory - {_currentWell.WellName}";
                        ContentArea.Content = _inventoryDashboardView;
                        if (_inventoryDashboardView?.DataContext is InventoryProductsDashboardViewModel dvm)
                            dvm.LoadForDate(dvm.SelectedDate);
                    };

                    ContentTitle.Text = "Inventory - Edit Received (Shipment Ref " + requisition + ")";
                    ContentArea.Content = view;
                };

                vm.RequestEditReturnedByRequisition += (requisition) =>
                {
                    var view = new TicketReturnedView();
                    var vmr = new TicketReturnedViewModel(_inventoryService);
                    view.DataContext = vmr;

                    // Cargar por remisión
                    vmr.LoadByRequisition(requisition);

                    vmr.RequestClose += () =>
                    {
                        ContentTitle.Text = $"Inventory - {_currentWell.WellName}";
                        ContentArea.Content = _inventoryDashboardView;
                        if (_inventoryDashboardView?.DataContext is InventoryProductsDashboardViewModel dvm)
                            dvm.LoadForDate(dvm.SelectedDate);
                    };

                    ContentTitle.Text = "Inventory - Edit Returned (Shipment Ref " + requisition + ")";
                    ContentArea.Content = view;
                };

                // Suscribir edición por TicketId
                vm.RequestEditReturnedByTicketId += (ticketId) =>
                {
                    var view = new TicketReturnedView();
                    var vmr = new TicketReturnedViewModel(_inventoryService);
                    view.DataContext = vmr;

                    // Cargar por TicketId (esto rellenará Requisition y Lines correctamente)
                    vmr.LoadTicket(ticketId);

                    vmr.RequestClose += () =>
                    {
                        ContentTitle.Text = $"Inventory - {_currentWell.WellName}";
                        ContentArea.Content = _inventoryDashboardView;

                        if (_inventoryDashboardView?.DataContext is InventoryProductsDashboardViewModel dvm)
                            dvm.LoadForDate(dvm.SelectedDate);
                    };

                    ContentTitle.Text = "Inventory - Edit Returned (Shipment Ref " + (vmr.Requisition ?? ticketId) + ")";
                    ContentArea.Content = view;
                };

                // Subscribe Used -> Fluid: open consumption dialog
                vm.RequestUsedAsFluid += (row) =>
                {
                    Debug.WriteLine("DEBUG: MainWindow RequestUsedAsFluid received. ProductCode: " + (row?.ProductCode ?? "<null>"));

                    if (row == null) return;

                    var dlg = new ProjectReport.Views.Inventory.ReportConsumedDialog();
                    var vmc = new ProjectReport.ViewModels.Inventory.ReportConsumedViewModel(_inventoryService);

                    // preseleccionar el producto correspondiente si existe
                    var prod = _inventoryService.GetProducts()
                        .FirstOrDefault(p => string.Equals(p.Code, row.ProductCode, StringComparison.OrdinalIgnoreCase));
                    if (prod != null) vmc.SelectedProduct = prod;

                    dlg.DataContext = vmc;
                    vmc.RequestClose += () =>
                    {
                        if (dlg.IsVisible) dlg.Close();
                        // refrescar dashboard
                        if (_inventoryDashboardView?.DataContext is InventoryProductsDashboardViewModel dvm)
                            dvm.LoadForDate(dvm.SelectedDate);
                    };

                    dlg.Owner = this;
                    dlg.Topmost = true; // temporal: forzar encima
                    dlg.Show();        // usar Show para ver si aparece como ventana no modal
                    // (quita Topmost/Show y restaura ShowDialog() cuando confirmes)
                };

                vm.RequestUsedAsOther += (row) =>
                {
                    try
                    {
                        var dlg = new ProjectReport.Views.Inventory.ReportOtherDialog();
                        var vmr = new ProjectReport.ViewModels.Inventory.ReportOtherViewModel(_inventoryService);
                        dlg.DataContext = vmr;

                        vmr.RequestClose += () =>
                        {
                            if (dlg.IsVisible) dlg.Close();
                            if (_inventoryDashboardView?.DataContext is InventoryProductsDashboardViewModel dvm)
                                dvm.LoadForDate(dvm.SelectedDate);
                        };

                        dlg.Owner = this;
                        dlg.ShowDialog();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine("Error opening ReportOtherDialog: " + ex);
                        MessageBox.Show("Error opening Other Activities:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                _inventoryDashboardView.DataContext = vm;
            }

            ContentTitle.Text = $"Inventory - {_currentWell.WellName}"
            ;
            ContentArea.Content = _inventoryDashboardView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
        }



        //==========================================
        // REPORT (dentro del MainWindow, sin ventanas encima)
        //==========================================

        public void NavigateToReportDetails(Well well)
        {
            SaveGeometryDataIfNeeded();

            var page = new ReportDetailsPage();
            // OJO: usa tu VM real. Si tu constructor es distinto, me lo pegas y lo ajusto.
            page.DataContext = new ReportDetailsViewModel(well);

            ContentTitle.Text = $"New Report - {well.WellName}";
            ContentArea.Content = page;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
        }

        //==========================================
        // GEOMETRY SUB-PAGES
        //==========================================

        private GeometryView GetOrCreateGeometryView()
        {
            if (_geometryView == null)
            {
                _geometryView = new GeometryView();

                var vm = new GeometryViewModel(
                    new GeometryCalculationService(),
                    new ThermalGradientService());

                vm.LoadWell(_currentWell);

                _geometryView.DataContext = vm;
            }

            var geometryVM = (GeometryViewModel)_geometryView.DataContext;

            // 🔹 SOLO actualizar el reporte activo (no recargar el well)
            var report = _currentWell?.Reports?
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();

            if (report != null && geometryVM.CurrentReport != report)
            {
                geometryVM.LoadReport(report);
            }
            // 🔹 sincronizar SIEMPRE al entrar
            if (_geometryView.DataContext is GeometryViewModel geoVm)
            {
                geoVm.SyncGeometryWithReport();
            }


            return _geometryView;
        }


        private void ReportDetailButton_Click(object sender, RoutedEventArgs e)
        {
            var reportVM = new ProjectReport.Modules.ReportDetail.ViewModels.ReportDViewModel(_currentWell);

            reportVM.OnReportSaved += (s, newReport) =>
            {
                NavigateToWellDashboard(_currentWell.Id);
            };

            var view = new ReportDetailsView(_currentWell);
            view.DataContext = reportVM;

            ContentArea.Content = view;
            ContentTitle.Text = $"Report Detail - {_currentWell.WellName}";
        }




        private void WellboreGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Wellbore Geometry";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 0;

            ContentArea.Content = view;
        }

        private void DrillStringGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Drill String Geometry";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 1;

            ContentArea.Content = view;
        }

        private void SurveyButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Survey";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 2;

            ContentArea.Content = view;
        }

        private void ThermalGradientButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Thermal Gradient";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 3;

            ContentArea.Content = view;
        }

        private void WellTestButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Well Test";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 4;

            ContentArea.Content = view;
        }

        private void SummaryButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Summary";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 5;

            ContentArea.Content = view;
        }

        //==========================================
        // UTILS
        //==========================================

        private void SaveGeometryDataIfNeeded()
        {
            if (_geometryView != null &&
                _geometryView.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
            {
                vm.SaveToWell();
            }
        }

        private void GeometryButton_Click(object sender, RoutedEventArgs e)
        {
            GeometrySubmenu.Visibility =
                GeometrySubmenu.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

            ContentTitle.Text = $"Geometry - {_currentWell.WellName}";
            ContentArea.Content = GetOrCreateGeometryView();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentWellId.HasValue)
            {
                NavigateToWellDashboard(_currentWellId.Value);
            }
        }


        private void Logo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NavigateToHome();
        }

        //==========================================
        // WINDOW BUTTONS + DRAG
        //==========================================

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            NavigationService.Instance.NavigationRequested -= OnNavigationRequested;
            _databaseService?.Dispose();
            base.OnClosed(e);
        }

        private void ToastNotificationControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
