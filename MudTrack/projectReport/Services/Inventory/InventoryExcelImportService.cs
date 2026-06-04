using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;

namespace ProjectReport.Services.Inventory
{
    public class UniversalProduct
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Presentation { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double Quantity { get; set; } = 0;
    }

    public class InventoryExcelImportService
    {
        // Reads first worksheet and maps columns by header names (case-insensitive)
        public List<UniversalProduct> LoadUniversalProducts(string path)
        {
            var result = new List<UniversalProduct>();

            if (!File.Exists(path)) return result;

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null) return result;

            // Find first used row to act as header (more robust than assuming row 1)
            var firstUsedRow = ws.FirstRowUsed();
            if (firstUsedRow == null) return result;

            var headerRow = firstUsedRow;
            var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var headerMap = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

            for (int c = 1; c <= lastCol; c++)
            {
                var cell = headerRow.Cell(c);
                var text = cell.GetString().Trim();
                if (!string.IsNullOrEmpty(text) && !headerMap.ContainsKey(text))
                    headerMap[text] = c;
            }

            int GetCol(params string[] names)
            {
                foreach (var n in names)
                {
                    if (string.IsNullOrWhiteSpace(n)) continue;
                    // try exact header key
                    if (headerMap.TryGetValue(n, out var idx)) return idx;
                    // try case-insensitive contains match (some files have trailing spaces or different formatting)
                    var found = headerMap.Keys.FirstOrDefault(k => k.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0);
                    if (found != null) return headerMap[found];
                }
                return -1;
            }

            // Accept many variants commonly used
            var colCode = GetCol("Code", "Item Code", "Codigo", "Codigo SIIGO", "Código", "Codigo SIIGO");
            var colName = GetCol("Name", "Nombre", "Description - Otros nombres", "Description", "Producto", "Product");
            var colCategory = GetCol("Category", "Categoria", "CATEGORIA");
            var colPresentation = GetCol("Presentation", "Presentacion", "Packaging");
            var colUnit = GetCol("Unit", "Unidad", "UNIDAD");
            var colQuantity = GetCol("Quantity", "Qty", "Cantidad", "Cantidad (Stock)", "Stock");

            // Start reading rows AFTER headerRow
            var startRow = headerRow.RowNumber() + 1;
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? ws.Rows().Count();

            for (int r = startRow; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (row.IsEmpty()) continue;

                var prod = new UniversalProduct();

                if (colCode > 0)
                    prod.Code = row.Cell(colCode).GetString().Trim();

                if (colName > 0)
                    prod.Name = row.Cell(colName).GetString().Trim();

                if (colCategory > 0)
                    prod.Category = row.Cell(colCategory).GetString().Trim();

                if (colPresentation > 0)
                    prod.Presentation = row.Cell(colPresentation).GetString().Trim();

                if (colUnit > 0)
                    prod.Unit = row.Cell(colUnit).GetString().Trim();

                if (colQuantity > 0)
                {
                    var cellText = row.Cell(colQuantity).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(cellText))
                    {
                        if (double.TryParse(cellText, NumberStyles.Any, CultureInfo.InvariantCulture, out var q) ||
                            double.TryParse(cellText, NumberStyles.Any, CultureInfo.CurrentCulture, out q))
                        {
                            prod.Quantity = q;
                        }
                        else
                        {
                            // try numeric cell value using ClosedXML TryGetValue
                            try
                            {
                                if (row.Cell(colQuantity).TryGetValue<double>(out var dv))
                                {
                                    prod.Quantity = dv;
                                }
                                else if (row.Cell(colQuantity).TryGetValue<int>(out var iv))
                                {
                                    prod.Quantity = iv;
                                }
                                else if (row.Cell(colQuantity).TryGetValue<decimal>(out var decv))
                                {
                                    prod.Quantity = (double)decv;
                                }
                            }
                            catch
                            {
                                // ignore fallback
                            }
                        }
                    }
                }

                // Only add if has code or name
                if (!string.IsNullOrWhiteSpace(prod.Code) || !string.IsNullOrWhiteSpace(prod.Name))
                    result.Add(prod);
            }

            return result;
        }
    }
}

