using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Inventory
{
    public class TicketHistoryItem
    {
        public string TicketId { get; set; } = "";
        public string Requisition { get; set; } = "";
        public DateTime Date { get; set; }
        public string Type { get; set; } = "";
        public string Origin { get; set; } = "";
        public string User { get; set; } = "";
        public int LineCount { get; set; }
        public double TotalValue { get; set; }
        public string SupplierName { get; set; } = "";
        public string Observations { get; set; } = "";
        public string ShipmentReference { get; set; } = "";

        public string TicketLabel => string.IsNullOrWhiteSpace(Requisition) ? "(no #)" : $"#{Requisition}";
        public string TotalValueFmt => TotalValue > 0 ? $"${TotalValue:N2}" : "-";
    }

    public class TicketLineItem
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double Quantity { get; set; }
        public string Unit { get; set; } = "";
        public double UnitPrice { get; set; }
        public double Total => Quantity * UnitPrice;
        public string TotalFmt => Total > 0 ? $"${Total:N2}" : "-";
        public string UnitPriceFmt => UnitPrice > 0 ? $"${UnitPrice:N2}" : "-";
        public string OriginOrUse { get; set; } = "";
    }

    public class InventoryHistoryViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        public ObservableCollection<TicketHistoryItem> Tickets { get; } = new();
        public ObservableCollection<TicketLineItem> TicketLines { get; } = new();

        private TicketHistoryItem? _selectedTicket;
        public TicketHistoryItem? SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                if (SetProperty(ref _selectedTicket, value))
                    LoadLines(value);
            }
        }

        public bool HasSelection => _selectedTicket != null;

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ClearOldCommand { get; }
        public RelayCommand PrintSelectedTicketCommand { get; }

        public InventoryHistoryViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            RefreshCommand = new RelayCommand(_ => LoadMovements());
            ClearOldCommand = new RelayCommand(_ => ClearOldTickets());
            PrintSelectedTicketCommand = new RelayCommand(
                _ => PrintSelectedTicket(),
                _ => SelectedTicket != null);

            _service.InventoryUpdated += OnInventoryUpdated;
            LoadMovements();
        }

        private void OnInventoryUpdated() => LoadMovements();

        private void LoadMovements()
        {
            var previousId = _selectedTicket?.TicketId;

            Tickets.Clear();
            TicketLines.Clear();

            var movements = _service.GetMovements();

            var grouped = movements
                .Where(m => !string.IsNullOrWhiteSpace(m.TicketId))
                .GroupBy(m => m.TicketId)
                .Select(g =>
                {
                    var items = g.OrderBy(m => m.Date).ToList();
                    var first = items.First();
                    return new TicketHistoryItem
                    {
                        TicketId = g.Key,
                        Requisition = first.Requisition,
                        Date = first.Date,
                        Type = first.Type.ToString(),
                        Origin = first.OriginOrUse,
                        User = first.User,
                        LineCount = items.Count,
                        TotalValue = items.Sum(m => m.Quantity * m.UnitPrice),
                        SupplierName = first.SupplierName,
                        Observations = first.Observations,
                        ShipmentReference = first.ShipmentReference
                    };
                })
                .OrderByDescending(t => t.Date)
                .ToList();

            foreach (var t in grouped)
                Tickets.Add(t);

            if (previousId != null)
            {
                var toReselect = Tickets.FirstOrDefault(t => t.TicketId == previousId);
                if (toReselect != null)
                    SelectedTicket = toReselect;
            }
        }

        private void LoadLines(TicketHistoryItem? ticket)
        {
            TicketLines.Clear();
            OnPropertyChanged(nameof(HasSelection));
            CommandManager.InvalidateRequerySuggested();

            if (ticket == null)
                return;

            var movements = _service.GetMovements()
                .Where(m => string.Equals(m.TicketId, ticket.TicketId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.ProductName)
                .ToList();

            foreach (var m in movements)
            {
                TicketLines.Add(new TicketLineItem
                {
                    ProductCode = m.ProductCode,
                    ProductName = m.ProductName,
                    Quantity = m.Quantity,
                    UnitPrice = m.UnitPrice,
                    OriginOrUse = m.OriginOrUse
                });
            }
        }

        private void ClearOldTickets()
        {
            var allMovements = _service.GetMovements().ToList();
            if (allMovements.Count == 0)
                return;

            var ticketIds = allMovements
                .Select(m => m.TicketId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            foreach (var ticketId in ticketIds)
                _service.DeleteMovementsForTicket(ticketId, removeLinkedByRequisition: false);

            LoadMovements();
        }

        private void PrintSelectedTicket()
        {
            if (SelectedTicket == null || TicketLines.Count == 0)
                return;

            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true)
                    return;

                var doc = BuildTicketDocument(SelectedTicket, TicketLines);
                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                printDialog.PrintDocument(paginator, $"Ticket {SelectedTicket.TicketLabel}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to print ticket: {ex.Message}",
                    "Print Ticket",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static FlowDocument BuildTicketDocument(TicketHistoryItem ticket, ObservableCollection<TicketLineItem> lines)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(40),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };

            // Header with software logo (left) and company logo (right)
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var softwareLogo = BuildLogoImage("Logo.png", 76);
            if (softwareLogo != null)
            {
                Grid.SetColumn(softwareLogo, 0);
                headerGrid.Children.Add(softwareLogo);
            }

            var companyLogoHeader = BuildLogoImage("FooterLogo.png", 120);
            if (companyLogoHeader != null)
            {
                companyLogoHeader.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(companyLogoHeader, 2);
                headerGrid.Children.Add(companyLogoHeader);
            }

            doc.Blocks.Add(new BlockUIContainer(headerGrid));

            doc.Blocks.Add(new Paragraph(new Run("INVENTORY TICKET"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            doc.Blocks.Add(new Paragraph(new Run(
                $"Ticket: {ticket.TicketLabel}    Type: {ticket.Type}    Date: {ticket.Date:yyyy-MM-dd HH:mm}")));
            doc.Blocks.Add(new Paragraph(new Run(
                $"Origin/Use: {ticket.Origin}    Supplier: {ticket.SupplierName}    User: {ticket.User}")));
            doc.Blocks.Add(new Paragraph(new Run($"Shipment Ref: {ticket.ShipmentReference}"))
            {
                Margin = new Thickness(0, 0, 0, 8)
            });

            if (!string.IsNullOrWhiteSpace(ticket.Observations))
                doc.Blocks.Add(new Paragraph(new Run($"Notes: {ticket.Observations}")));

            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 12, 0, 10)
            };

            // Keep total width print-safe to avoid right-side clipping.
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(250) });
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var header = new TableRow();
            rowGroup.Rows.Add(header);
            AddCell(header, "Code", true);
            AddCell(header, "Product", true);
            AddCell(header, "Qty", true, rightAligned: true);
            AddCell(header, "Unit Price", true, rightAligned: true);
            AddCell(header, "Total", true, rightAligned: true);

            foreach (var line in lines)
            {
                var row = new TableRow();
                rowGroup.Rows.Add(row);
                AddCell(row, line.ProductCode);
                AddCell(row, line.ProductName);
                AddCell(row, $"{line.Quantity:N2}", rightAligned: true);
                AddCell(row, line.UnitPriceFmt, rightAligned: true);
                AddCell(row, line.TotalFmt, rightAligned: true);
            }

            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph(new Run($"TOTAL: {ticket.TotalValueFmt}"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 2, 0, 20)
            });

            // Footer with company logo
            var footerPanel = new StackPanel { Orientation = Orientation.Vertical };
            footerPanel.Children.Add(new TextBlock
            {
                Text = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}",
                Foreground = Brushes.DimGray,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var companyLogoFooter = BuildLogoImage("FooterLogo.png", 140);
            if (companyLogoFooter != null)
            {
                companyLogoFooter.HorizontalAlignment = HorizontalAlignment.Center;
                footerPanel.Children.Add(companyLogoFooter);
            }

            doc.Blocks.Add(new BlockUIContainer(footerPanel));

            return doc;
        }

        private static void AddCell(TableRow row, string text, bool isHeader = false, bool rightAligned = false)
        {
            var paragraph = new Paragraph(new Run(text ?? string.Empty))
            {
                TextAlignment = rightAligned ? TextAlignment.Right : TextAlignment.Left
            };

            row.Cells.Add(new TableCell(paragraph)
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(6, 4, 6, 4),
                FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal
            });
        }

        private static Image? BuildLogoImage(string fileName, double maxWidth)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Assets", fileName)),
                Path.Combine(Environment.CurrentDirectory, "projectReport", "Assets", fileName),
                Path.Combine(Environment.CurrentDirectory, "Assets", fileName),
                Path.Combine(@"C:\Dev\MudTrack\projectReport\Assets", fileName)
            };

            var path = candidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                return new Image
                {
                    Source = bmp,
                    MaxWidth = maxWidth,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0, 0, 0, 0)
                };
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            try { _service.InventoryUpdated -= OnInventoryUpdated; } catch { }
        }
    }
}
