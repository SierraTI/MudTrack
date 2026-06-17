using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClosedXML.Excel;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;
using ProjectReport.ViewModels;
using System.Collections.Generic;

namespace ProjectReport.ViewModels.Inventory
{
    public class ReportConsumedViewModel : BaseViewModel
    {
        private readonly InventoryService _service;

        // Fluid list loaded from Excel (legacy or English file names) or repository fallback
        public ObservableCollection<FluidItem> Fluids { get; } = new();
        public ICollectionView FluidsView { get; private set; }

        private FluidItem? _selectedFluid;
        public FluidItem? SelectedFluid
        {
            get => _selectedFluid;
            set => SetProperty(ref _selectedFluid, value);
        }

        // Fallback: productos clásicos (si se necesita)
        public ObservableCollection<Product> Products { get; } = new();
        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        // Texto para filtrar el collection view (se enlaza al ComboBox.Text)
        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    FluidsView?.Refresh();
                }
            }
        }

        public ICommand CancelCommand { get; }
        public ICommand SaveCommand { get; }

        public event Action? RequestClose;

        // Constructor
        public ReportConsumedViewModel(InventoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            CancelCommand = new RelayCommand(_ => OnCancel());
            SaveCommand = new RelayCommand(_ => OnSave());

            FluidsView = CollectionViewSource.GetDefaultView(Fluids);
            FluidsView.Filter = FluidsFilter;

            LoadProductsAndFluids();
        }

        // Simple model for each fluid row in Excel
        public class FluidItem
        {
            public string FluidSetName { get; set; } = string.Empty;
            public string BaseFluidType { get; set; } = string.Empty;
            public string FluidCategory { get; set; } = string.Empty;
            public string FluidSystem { get; set; } = string.Empty;
            public string BrineType { get; set; } = string.Empty;

            public string DisplayName =>
                $"{FluidSetName} | {BaseFluidType} | {FluidCategory} | {FluidSystem}" +
                (string.IsNullOrWhiteSpace(BrineType) ? string.Empty : $" | {BrineType}");
        }

        private bool FluidsFilter(object obj)
        {
            if (obj is not FluidItem fi) return false;
            if (string.IsNullOrWhiteSpace(FilterText)) return true;

            var q = FilterText.Trim().ToLowerInvariant();
            return (fi.FluidSetName?.ToLowerInvariant().Contains(q) ?? false) ||
                   (fi.BaseFluidType?.ToLowerInvariant().Contains(q) ?? false) ||
                   (fi.FluidCategory?.ToLowerInvariant().Contains(q) ?? false) ||
                   (fi.FluidSystem?.ToLowerInvariant().Contains(q) ?? false) ||
                   (fi.BrineType?.ToLowerInvariant().Contains(q) ?? false);
        }

        // Try fluid list files first; then fallback to repository products.
        private void LoadProductsAndFluids()
        {
            Fluids.Clear();
            Products.Clear();

            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListFluids.xlsx"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListFluids.xls"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListFluids.xlsx"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListFluids.xls"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListaFluidos.xlsx"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListaFluidos.xls"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListaFluidos.xlsx"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "ListaFluidos.xls"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "Lista.xlsx"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "Lista.xlsx")
            };

            var excelPath = candidates.FirstOrDefault(File.Exists) ?? string.Empty;

            if (!string.IsNullOrEmpty(excelPath))
            {
                // Intentar primero con el importador existente
                try
                {
                    var importer = new InventoryExcelImportService();
                    var uni = importer.LoadUniversalProducts(excelPath)?.ToList();

                    if (uni != null && uni.Count > 0)
                    {
                        // Si LoadUniversalProducts provee Name/Category/Unit, mapear a FluidItem donde posible
                        foreach (var u in uni)
                        {
                            var fi = new FluidItem
                            {
                                FluidSetName = string.IsNullOrWhiteSpace(u.Name) ? (u.Code ?? string.Empty) : u.Name,
                                BaseFluidType = u.Unit ?? string.Empty, // si el excel "Unit" no coincide, se sobreescribe en TryLoadExcelDirect
                                FluidCategory = u.Category ?? string.Empty,
                                FluidSystem = string.Empty,
                                BrineType = string.Empty
                            };
                            Fluids.Add(fi);

                            // También mantener Products fallback mínimo (para compatibilidad de guardado)
                            var p = new Product
                            {
                                Code = u.Code ?? fi.FluidSetName.ToUpperInvariant().Replace(' ', '_'),
                                Name = fi.FluidSetName,
                                Category = fi.FluidCategory,
                                Unit = u.Unit ?? "Each",
                                StockQty = 0,
                                CurrentUnitCost = 0,
                                Status = ProductStatus.Active
                            };
                            Products.Add(p);
                        }
                    }
                    else
                    {
                        // Si el importador no devolvió filas, lectura tolerante directa
                        TryLoadExcelDirectToFluids(excelPath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error loading fluid list via importer: " + ex);
                    try
                    {
                        TryLoadExcelDirectToFluids(excelPath);
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine("Error loading fluid list via ClosedXML: " + ex2);
                        // Fallback al repositorio
                        var repoList = _service.GetProducts()
                                               .Where(p => p.IsSelectedForReport)
                                               .OrderBy(p => p.Name);
                        foreach (var p in repoList)
                        {
                            Products.Add(p);
                            Fluids.Add(new FluidItem { FluidSetName = p.Name, BaseFluidType = p.Unit ?? string.Empty, FluidCategory = p.Category ?? string.Empty });
                        }
                    }
                }
            }
            else
            {
                // No Excel file found, use repository
                var list = _service.GetProducts()
                                   .Where(p => p.IsSelectedForReport)
                                   .OrderBy(p => p.Name);
                foreach (var p in list)
                {
                    Products.Add(p);
                    Fluids.Add(new FluidItem { FluidSetName = p.Name, BaseFluidType = p.Unit ?? string.Empty, FluidCategory = p.Category ?? string.Empty });
                }
            }

            // Refrescar vista y seleccionar primer elemento si aplica
            FluidsView = CollectionViewSource.GetDefaultView(Fluids);
            FluidsView.Filter = FluidsFilter;
            OnPropertyChanged(nameof(FluidsView));

            if (Fluids.Count > 0 && SelectedFluid == null) SelectedFluid = Fluids.First();
            if (Products.Count > 0 && SelectedProduct == null) SelectedProduct = Products.First();
        }

        // Tolerant direct reader: maps similar header names
        private void TryLoadExcelDirectToFluids(string path)
        {
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null) throw new InvalidOperationException("No worksheets found");

            // Map headers (row 1)
            var headerCells = ws.Row(1).CellsUsed().ToList();
            if (headerCells.Count == 0) throw new InvalidOperationException("No header row found");

            var headers = headerCells.Select(c => c.GetString().Trim().ToLowerInvariant()).ToList();

            int idxFluidSet = headers.FindIndex(h => h.Contains("fluid set") || h.Contains("fluidset") || h.Contains("fluid set name") || h.Contains("fluidsetname") || h.Contains("fluido"));
            int idxBaseType = headers.FindIndex(h => h.Contains("base") || h.Contains("base fluid") || h.Contains("base fluid type") || h.Contains("basefluidtype"));
            int idxCategory = headers.FindIndex(h => h.Contains("category") || h.Contains("fluid category") || h.Contains("categoria"));
            int idxSystem = headers.FindIndex(h => h.Contains("system") || h.Contains("fluid system"));
            int idxBrine = headers.FindIndex(h => h.Contains("brine") || h.Contains("brine type") || h.Contains("brine_type"));

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var rowsRead = 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (row.IsEmpty()) continue;

                string fluidSet = string.Empty;
                string baseType = string.Empty;
                string category = string.Empty;
                string system = string.Empty;
                string brine = string.Empty;

                // Use mapped indexes if present, otherwise fallback heuristics
                if (idxFluidSet >= 0) fluidSet = row.Cell(idxFluidSet + 1).GetString().Trim();
                if (idxBaseType >= 0) baseType = row.Cell(idxBaseType + 1).GetString().Trim();
                if (idxCategory >= 0) category = row.Cell(idxCategory + 1).GetString().Trim();
                if (idxSystem >= 0) system = row.Cell(idxSystem + 1).GetString().Trim();
                if (idxBrine >= 0) brine = row.Cell(idxBrine + 1).GetString().Trim();

                // Heuristic: if fluidSet is empty, use first cell
                if (string.IsNullOrWhiteSpace(fluidSet))
                    fluidSet = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(fluidSet)) continue;

                var fi = new FluidItem
                {
                    FluidSetName = fluidSet,
                    BaseFluidType = baseType,
                    FluidCategory = category,
                    FluidSystem = system,
                    BrineType = brine
                };

                Fluids.Add(fi);

                // Also populate Products for save compatibility.
                Products.Add(new Product
                {
                    Code = fluidSet.ToUpperInvariant().Replace(' ', '_'),
                    Name = fluidSet,
                    Category = category,
                    Unit = string.IsNullOrWhiteSpace(baseType) ? "Each" : baseType,
                    StockQty = 0,
                    CurrentUnitCost = 0,
                    Status = ProductStatus.Active
                });

                rowsRead++;
            }

            if (rowsRead == 0)
                throw new InvalidOperationException("No product rows discovered in Excel sheet");
        }

        private void OnCancel()
        {
            Debug.WriteLine("ReportConsumedViewModel: Cancel requested.");
            RequestClose?.Invoke();
        }

        private void OnSave()
        {
            Debug.WriteLine("ReportConsumedViewModel: Save requested.");

            if (SelectedFluid == null && SelectedProduct == null)
            {
#if DEBUG
                MessageBox.Show("Please select a fluid.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
#endif
                return;
            }

            if (Quantity <= 0)
            {
#if DEBUG
                MessageBox.Show("Quantity must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
#endif
                return;
            }

            try
            {
                // Prioritize SelectedFluid (fluid list). Otherwise use SelectedProduct (repository).
                string code;
                string name;
                double unitPrice;

                if (SelectedFluid != null)
                {
                    name = SelectedFluid.FluidSetName;
                    code = string.IsNullOrWhiteSpace(SelectedFluid.FluidSetName) ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant() : SelectedFluid.FluidSetName.ToUpperInvariant().Replace(' ', '_');
                    unitPrice = 0;
                }
                else
                {
                    name = SelectedProduct!.Name;
                    code = SelectedProduct!.Code;
                    unitPrice = SelectedProduct!.CurrentUnitCost;
                }

                var line = new TicketLine
                {
                    ProductCode = code,
                    ProductName = name,
                    Quantity = Quantity,
                    UnitPrice = unitPrice,
                    Context = string.Empty,
                    Observations = string.Empty
                };

                var ticket = new Ticket
                {
                    Type = TicketType.Consumed,
                    Date = DateTime.Now,
                    User = Environment.UserName,
                    Observations = "",
                    Requisition = "",
                    Lines = new System.Collections.Generic.List<TicketLine> { line }
                };

                _service.CreateTicketConsumed(ticket);

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving consumption: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


