using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using ProjectReport.Services;
using ProjectReport.Models;
using ProjectReport.Views.Geometry;
using ProjectReport.Views.Inventory;
using ProjectReport.Views.ReportWizard; 
using ProjectReport.ViewModels; // <-- added to reference ReportDetailsViewModel
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels.Inventory;




namespace ProjectReport.Views
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;

        private GeometryView? _geometryView;
        private HomeView? _homeView;
        private WellDataView? _wellDataView;
        private Views.WellDashboardView? _wellDashboardView;


        public Project CurrentProject { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _databaseService = new DatabaseService();

            // Demo project
            CurrentProject = new Project
            {
                Name = "Y-23A",
                WellName = "Well-04"
            };

            // IMPORTANTE: mantén esto por ahora
            DataContext = this;

            NavigationService.Instance.NavigationRequested += OnNavigationRequested;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();

            NavigateToHome();
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
        }

        private void NavigateToWellData(int wellId)
        {
            SaveGeometryDataIfNeeded();

            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null) return;

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
            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
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
            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null) return;

            _wellDashboardView = new Views.WellDashboardView();
            var vm = new ProjectReport.ViewModels.WellDashboardViewModel(CurrentProject);
            vm.LoadWell(well);
            _wellDashboardView.DataContext = vm;

            ContentTitle.Text = $"Dashboard - {well.WellName}";
            ContentArea.Content = _wellDashboardView;

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

            _inventoryService ??= new InventoryService(new JsonInventoryRepository());

            if (_inventoryDashboardView == null)
            {
                _inventoryDashboardView = new InventoryDashboardView();

                var vm = new InventoryProductsDashboardViewModel(_inventoryService);

                // Aquí conectamos los botones del dashboard para abrir pantallas
                vm.RequestOpenReceived += () =>
                {
                    var view = new TicketReceivedView();
                    view.DataContext = new TicketReceivedViewModel(_inventoryService);

                    ContentTitle.Text = "Inventory - Ticket Received";
                    ContentArea.Content = view;
                };

                vm.RequestOpenConsumed += () =>
                {
                    var view = new TicketConsumedView();
                    view.DataContext = new TicketConsumedViewModel(_inventoryService);

                    ContentTitle.Text = "Inventory - Ticket Consumed";
                    ContentArea.Content = view;
                };

                vm.RequestOpenHistory += () =>
                {
                    var view = new InventoryHistoryView();
                    view.DataContext = new InventoryHistoryViewModel(_inventoryService);

                    ContentTitle.Text = "Inventory - History";
                    ContentArea.Content = view;
                };

                _inventoryDashboardView.DataContext = vm;
            }

            ContentTitle.Text = "Inventory";
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
            _geometryView ??= new GeometryView();
            return _geometryView;
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

            ContentTitle.Text = "Geometry";
            ContentArea.Content = GetOrCreateGeometryView();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
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
    }
}
