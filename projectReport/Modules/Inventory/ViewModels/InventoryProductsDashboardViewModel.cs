using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.Services;

namespace ProjectReport.ViewModels.Inventory
{
    public class InventoryProductsDashboardViewModel : BaseViewModel
    {
        private readonly InventoryService _service;
        private readonly WellContextService _context_service = WellContextService.Instance;

        // NAV EVENTS
        public event Action? RequestOpenReceived;
        public event Action? RequestOpenReturned;
        public event Action? RequestOpenHistory;

        // Eventos por remisión específicos
        public event Action<string>? RequestEditReceivedByRequisition;
        public event Action<string>? RequestEditReturnedByRequisition;
        public event Action<string>? RequestEditReturnedByTicketId; // <- Nuevo evento añadido aquí

        // Nuevo: eventos para Used -> Fluido / Otras actividades (pasa la fila)
        public event Action<ProductSummaryRow>? RequestUsedAsFluido;
        public event Action<ProductSummaryRow>? RequestUsedAsOther;

        // COMMANDS
        public RelayCommand OpenTicketReceivedCommand { get; }
        public RelayCommand OpenTicketReturnedCommand { get; }
        public RelayCommand OpenHistoryCommand { get; }
        public RelayCommand RefreshCommand { get; }

        // Delete & Edit command per ticket/row
        public RelayCommand DeleteRowCommand { get; }
        public RelayCommand EditRowCommand { get; }

        // New: per-row commands to invoke edit-by-requisition
        public RelayCommand EditReceivedByRequisitionCommand { get; }
        public RelayCommand EditReturnedByRequisitionCommand { get; }

        // New command to edit by TicketId
        public RelayCommand EditReturnedByTicketIdCommand { get; }

        // New commands for Used menu
        public RelayCommand UsedAsFluidoCommand { get; }
        public RelayCommand UsedAsOtherCommand { get; }

        // TABLE DATA
        public ObservableCollection<ProductSummaryRow> Rows { get; } = new();
        public ObservableCollection<ProductSummaryRow> FilteredRows { get; } = new();

        private bool _isRigFilterEnabled;
        public bool IsRigFilterEnabled
        {
            get => _isRigFilterEnabled;
            set
            {
                if (SetProperty(ref _isRigFilterEnabled, value))
                    ApplyFilter();
            }
        }

        private ProductSummaryRow? _selectedRow;
        public ProductSummaryRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (SetProperty(ref _selectedRow, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                    LoadForDate(_selectedDate);
            }
        }

        public double TotalProductsCost => Rows.Sum(r => r.DailyCost);

        public InventoryProductsDashboardViewModel(InventoryService service)
        {
            _service = service;

            OpenTicketReceivedCommand = new RelayCommand(_ => RequestOpenReceived?.Invoke());
            OpenTicketReturnedCommand = new RelayCommand(_ => RequestOpenReturned?.Invoke());

            OpenHistoryCommand = new RelayCommand(_ => RequestOpenHistory?.Invoke());
            RefreshCommand = new RelayCommand(_ => LoadForDate(SelectedDate));

            // Mantener EditRowCommand por compatibilidad, pero preferimos los comandos por remisión
            EditRowCommand = new RelayCommand(param =>
            {
                var row = param as ProductSummaryRow;
                if (row == null) return;

                // Si tenemos TicketId usamos el flujo normal
                if (!string.IsNullOrWhiteSpace(row.TicketId))
                {
                    RequestEditReceivedByRequisition?.Invoke(row.TicketId);
                }
            });

            // Comandos nuevos: Edit by Requisition
            EditReceivedByRequisitionCommand = new RelayCommand(param =>
            {
                var requisition = param as string ?? string.Empty;
                requisition = requisition.Trim();
                if (string.IsNullOrWhiteSpace(requisition))
                {
                    MessageBox.Show("No hay remisión asociada a esta fila para editar Received.", "Editar Received", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                RequestEditReceivedByRequisition?.Invoke(requisition);
            });

            EditReturnedByRequisitionCommand = new RelayCommand(param =>
            {
                var requisition = param as string ?? string.Empty;
                requisition = requisition.Trim();
                if (string.IsNullOrWhiteSpace(requisition))
                {
                    MessageBox.Show("No hay remisión asociada a esta fila para editar Returned.", "Editar Returned", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                RequestEditReturnedByRequisition?.Invoke(requisition);
            });

            // Edit by TicketId command
            EditReturnedByTicketIdCommand = new RelayCommand(param =>
            {
                if (param is not ProductSummaryRow row)
                {
                    MessageBox.Show("No se ha seleccionado una fila válida para editar Returned.", "Editar Returned", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Si ya tiene TicketId, usarlo
                if (!string.IsNullOrWhiteSpace(row.TicketId))
                {
                    RequestEditReturnedByTicketId?.Invoke(row.TicketId);
                    return;
                }

                // 1) Buscar Returned en la fecha seleccionada
                var match = _service.GetMovements()
                    .Where(m => string.Equals(m.ProductCode, row.ProductCode, StringComparison.OrdinalIgnoreCase)
                                && m.Type == TicketType.Returned
                                && m.Date.Date == SelectedDate.Date)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefault();

                if (match != null && !string.IsNullOrWhiteSpace(match.TicketId))
                {
                    RequestEditReturnedByTicketId?.Invoke(match.TicketId);
                    return;
                }

                // 2) Buscar Returned en todo el historial
                match = _service.GetMovements()
                    .Where(m => string.Equals(m.ProductCode, row.ProductCode, StringComparison.OrdinalIgnoreCase)
                                && m.Type == TicketType.Returned)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefault();

                if (match != null && !string.IsNullOrWhiteSpace(match.TicketId))
                {
                    RequestEditReturnedByTicketId?.Invoke(match.TicketId);
                    return;
                }

                // 3) Fallback: usar la remisión existente en la fila (si la hay)
                if (!string.IsNullOrWhiteSpace(row.Requisition))
                {
                    RequestEditReturnedByRequisition?.Invoke(row.Requisition);
                    return;
                }

                MessageBox.Show("No se encontró TicketId ni Remisión asociada para esta fila. Edite el ticket desde el historial.", "Editar Returned", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            // Comandos para Used menu
            UsedAsFluidoCommand = new RelayCommand(param =>
            {
                if (param is ProductSummaryRow row)
                {
                    Debug.WriteLine("InventoryProductsDashboardViewModel: UsedAsFluidoCommand ejecutado. Abriendo ReportConsumedDialog. ProductCode: " + row.ProductCode);

                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var dlg = new ProjectReport.Views.Inventory.ReportConsumedDialog();
                            var vmc = new ProjectReport.ViewModels.Inventory.ReportConsumedViewModel(_service);

                            // Preseleccionar producto si existe
                            var prod = _service.GetProducts()
                                .FirstOrDefault(p => string.Equals(p.Code, row.ProductCode, StringComparison.OrdinalIgnoreCase));
                            if (prod != null) vmc.SelectedProduct = prod;

                            dlg.DataContext = vmc;

                            vmc.RequestClose += () =>
                            {
                                if (dlg.IsVisible) dlg.Close();
                                // refrescar tabla tras cerrar
                                LoadForDate(SelectedDate);
                            };

                            // Asignar owner seguro y abrir modal
                            dlg.Owner = Application.Current?.MainWindow;
                            dlg.ShowDialog();
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error al abrir ReportConsumedDialog: " + ex);
                        MessageBox.Show("ERROR al abrir ReportConsumedDialog: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });

            UsedAsOtherCommand = new RelayCommand(param =>
            {
                if (param is ProductSummaryRow row)
                {
                    RequestUsedAsOther?.Invoke(row);
                }
            });

            // Eliminar por MovementId / TicketId+ProductCode (solo esa línea)
            DeleteRowCommand = new RelayCommand(param =>
            {
                ProductSummaryRow? row = param as ProductSummaryRow;
                if (row == null) return;

                var confirm = MessageBox.Show("¿Eliminar sólo este registro (producto) del ticket? Esta acción ajustará stocks.", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                // 1) Preferir eliminación por MovementId si está disponible (más preciso)
                if (!string.IsNullOrWhiteSpace(row.MovementId))
                {
                    _service.DeleteMovementById(row.MovementId);
                    LoadForDate(SelectedDate);
                    return;
                }

                // 2) Si hay TicketId, eliminar la línea por ticket+producto
                if (!string.IsNullOrWhiteSpace(row.TicketId))
                {
                    _service.DeleteMovementsForTicketLine(row.TicketId, row.ProductCode);
                    LoadForDate(SelectedDate);
                    return;
                }

                // 3) Buscar movimientos Returned para este producto en la fecha seleccionada
                var matches = _service.GetMovements()
                    .Where(m => string.Equals(m.ProductCode, row.ProductCode, StringComparison.OrdinalIgnoreCase)
                                && m.Date.Date == SelectedDate.Date
                                && m.Type == TicketType.Returned)
                    .ToList();

                if (matches.Count > 0)
                {
                    var msg = $"Se encontraron {matches.Count} movimiento(s) Returned para este producto en {SelectedDate:yyyy-MM-dd}.\n¿Eliminar todos los movimientos encontrados?";
                    var c2 = MessageBox.Show(msg, "Eliminar movimientos", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (c2 == MessageBoxResult.Yes)
                    {
                        foreach (var m in matches)
                        {
                            if (!string.IsNullOrWhiteSpace(m.MovementId))
                                _service.DeleteMovementById(m.MovementId);
                        }
                        LoadForDate(SelectedDate);
                        return;
                    }
                    else
                    {
                        // Si el usuario no confirma eliminar todos, intentar eliminar el último si existe MovementId
                        var last = matches.OrderByDescending(m => m.Date).FirstOrDefault();
                        if (last != null && !string.IsNullOrWhiteSpace(last.MovementId))
                        {
                            _service.DeleteMovementById(last.MovementId);
                            LoadForDate(SelectedDate);
                            return;
                        }
                    }
                }

                // 4) Si no hubo matches Returned en la fecha, buscar movimientos (cualquier tipo) en la fecha
                matches = _service.GetMovements()
                    .Where(m => string.Equals(m.ProductCode, row.ProductCode, StringComparison.OrdinalIgnoreCase)
                                && m.Date.Date == SelectedDate.Date)
                    .ToList();

                if (matches.Count > 0)
                {
                    var msg = $"Se encontraron {matches.Count} movimiento(s) para este producto en {SelectedDate:yyyy-MM-dd} (diferentes tipos).\n¿Eliminar todos los movimientos encontrados?";
                    var c3 = MessageBox.Show(msg, "Eliminar movimientos", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (c3 == MessageBoxResult.Yes)
                    {
                        foreach (var m in matches)
                        {
                            if (!string.IsNullOrWhiteSpace(m.MovementId))
                                _service.DeleteMovementById(m.MovementId);
                        }
                        LoadForDate(SelectedDate);
                        return;
                    }
                }

                // 5) Último recurso: buscar en todo el historial movimientos Returned por ProductCode
                matches = _service.GetMovements()
                    .Where(m => string.Equals(m.ProductCode, row.ProductCode, StringComparison.OrdinalIgnoreCase)
                                && m.Type == TicketType.Returned)
                    .ToList();

                if (matches.Count > 0)
                {
                    var msg = $"No se encontraron movimientos únicos en la fecha seleccionada, pero existen {matches.Count} movimiento(s) Returned para este producto en el historial.\n¿Eliminar todos los movimientos Returned encontrados en historial?";
                    var c4 = MessageBox.Show(msg, "Eliminar movimientos", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (c4 == MessageBoxResult.Yes)
                    {
                        foreach (var m in matches)
                        {
                            if (!string.IsNullOrWhiteSpace(m.MovementId))
                                _service.DeleteMovementById(m.MovementId);
                        }
                        LoadForDate(SelectedDate);
                        return;
                    }
                }

                MessageBox.Show("No se pudo determinar un movimiento único para eliminar. Seleccione la fila exacta del ticket o edite desde el historial.", "Eliminar", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            // Subscribe to inventory updates to refresh dashboard in real time
            _service.InventoryUpdated += () =>
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() => LoadForDate(SelectedDate));
                }
                else
                {
                    LoadForDate(SelectedDate);
                }
            };

            LoadForDate(SelectedDate);
        }

        public InventoryProductsDashboardViewModel() : this(ServiceLocator.InventoryService) { }

        public void LoadForDate(DateTime date)
        {
            Rows.Clear();
            SelectedRow = null;

            var products = _service.GetProducts();
            var movements = _service.GetMovements()
                .Where(m => m.Date.Date == date.Date)
                .OrderBy(m => m.Date)
                .ToList();

            // net change por producto (case-insensitive)
            var netByProduct = movements
                .GroupBy(m => (m.ProductCode ?? "").ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => (x.Type == TicketType.Received || x.Type == TicketType.Returned) ? x.Quantity : 0.0));

            // Agrupar por ProductCode solamente para que received/returned aparezcan en la misma fila
            var groups = movements
                .GroupBy(m => (m.ProductCode ?? "").ToUpperInvariant());

            foreach (var g in groups.OrderBy(g => g.Key))
            {
                // Buscar producto por código (case-insensitive)
                var prod = products.FirstOrDefault(p => string.Equals(p.Code, g.Key, StringComparison.OrdinalIgnoreCase));
                var productName = prod?.Name ?? g.Key;
                var productUnit = prod?.Unit ?? "";

                double received = g.Where(x => x.Type == TicketType.Received).Sum(x => x.Quantity);
                double returned = g.Where(x => x.Type == TicketType.Returned).Sum(x => x.Quantity);
                double used = g.Where(x => x.Type != TicketType.Received && x.Type != TicketType.Returned).Sum(x => x.Quantity);

                // Calcular DailyCost: suma de (Quantity * UnitPrice) para movimientos de consumo (Consumed)
                var consumedMovements = g.Where(x => x.Type == TicketType.Consumed).ToList();
                double dailyCost = consumedMovements.Sum(m => m.UnitPrice * m.Quantity);

                // Calcular UnitCostAvg: precio promedio ponderado de los consumos del día; si no hay consumo usar current unit cost del producto
                double unitCostAvg = 0;
                var totalConsumedQty = consumedMovements.Sum(m => m.Quantity);
                if (totalConsumedQty > 0)
                {
                    unitCostAvg = consumedMovements.Sum(m => m.UnitPrice * m.Quantity) / totalConsumedQty;
                }
                else
                {
                    unitCostAvg = prod?.CurrentUnitCost ?? 0;
                }

                double netChangeToday = 0;
                netByProduct.TryGetValue(g.Key, out netChangeToday);

                // Ahora: inicial del día mostrado como 0 (los productos están llegando hoy)
                double initialQty = 0;

                // Si el grupo contiene exactamente un ticket, relleno TicketId; si no, lo dejo vacío
                var distinctTicketIds = g.Select(x => x.TicketId ?? "").Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var ticketId = distinctTicketIds.Count == 1 ? distinctTicketIds.First() : "";

                // Si el grupo contiene exactamente un movimiento, uso su MovementId; si no, vacío
                var movementId = g.Count() == 1 ? g.First().MovementId ?? "" : "";

                // Preferir mostrar la requisición del ticket Returned cuando exista (si hay TicketId),
                // en caso contrario usar la primera requisisión disponible del grupo.
                string requisition;
                if (!string.IsNullOrWhiteSpace(ticketId))
                {
                    // Buscar la requisición asociada a movimientos Returned dentro del grupo
                    var returnedReq = g
                        .Where(x => x.Type == TicketType.Returned)
                        .Select(x => x.Requisition)
                        .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));

                    requisition = returnedReq ?? g.Select(x => x.Requisition).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)) ?? "";
                }
                else
                {
                    requisition = g.Select(x => x.Requisition).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)) ?? "";
                }

                Rows.Add(new ProductSummaryRow
                {
                    ProductCode = g.Key,
                    ProductName = productName,
                    Unit = productUnit,
                    InitialQty = initialQty,
                    Received = received,
                    Used = used,
                    Returned = returned,
                    RemainingStock = prod?.StockQty ?? 0,
                    UnitCostAvg = unitCostAvg,
                    DailyCost = dailyCost,
                    TicketId = ticketId,
                    Requisition = requisition,
                    MovementId = movementId
                });
            }

            OnPropertyChanged(nameof(TotalProductsCost));
            ApplyFilter();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ApplyFilter()
        {
            FilteredRows.Clear();
            var allRows = Rows.ToList();

            if (!IsRigFilterEnabled)
            {
                foreach (var r in allRows) FilteredRows.Add(r);
                return;
            }

            var rig = _context_service.CurrentWell?.RigProfile;
            var shakerKeywords = new List<string>();
            if (rig != null)
            {
                shakerKeywords = rig.SolidsControl
                    .Where(sc => sc.Type?.IndexOf("Shaker", StringComparison.OrdinalIgnoreCase) >= 0)
                    .SelectMany(sc => new[] { sc.Manufacturer, sc.Model })
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();
            }

            foreach (var row in allRows)
            {
                // Simple heuristic: if it's a screen, only show if it matches rig shakers
                bool isScreen = row.ProductName.IndexOf("Screen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                row.ProductCode.IndexOf("Screen", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isScreen)
                {
                    bool isMatch = shakerKeywords.Any(k =>
                        row.ProductName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        row.ProductCode.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (isMatch) FilteredRows.Add(row);
                }
                else
                {
                    FilteredRows.Add(row);
                }
            }
        }
    }
}
