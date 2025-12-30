using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;

namespace ProjectReport.Services.Inventory
{
    public class UniversalProduct
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Presentacion { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public double Cantidad { get; set; } = 0;
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
            var colCodigo = GetCol("Codigo", "Código", "Codigo SIIGO", "Codigo SIIGO", "Code", "Item Code", "Codigo SIIGO");
            var colNombre = GetCol("Nombre", "Name", "Descripcion - Otros nombres", "Description", "Producto", "Product");
            var colCategoria = GetCol("Categoria", "Category", "CATEGORIA");
            var colPresentacion = GetCol("Presentacion", "Presentation", "Packaging");
            var colUnidad = GetCol("Unit", "Unidad", "UNIDAD");
            var colCantidad = GetCol("Cantidad", "Qty", "Quantity", "Cantidad (Stock)", "Stock");

            // Start reading rows AFTER headerRow
            var startRow = headerRow.RowNumber() + 1;
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? ws.Rows().Count();

            for (int r = startRow; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (row.IsEmpty()) continue;

                var prod = new UniversalProduct();

                if (colCodigo > 0)
                    prod.Codigo = row.Cell(colCodigo).GetString().Trim();

                if (colNombre > 0)
                    prod.Nombre = row.Cell(colNombre).GetString().Trim();

                if (colCategoria > 0)
                    prod.Categoria = row.Cell(colCategoria).GetString().Trim();

                if (colPresentacion > 0)
                    prod.Presentacion = row.Cell(colPresentacion).GetString().Trim();

                if (colUnidad > 0)
                    prod.Unidad = row.Cell(colUnidad).GetString().Trim();

                if (colCantidad > 0)
                {
                    var cellText = row.Cell(colCantidad).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(cellText))
                    {
                        if (double.TryParse(cellText, NumberStyles.Any, CultureInfo.InvariantCulture, out var q) ||
                            double.TryParse(cellText, NumberStyles.Any, CultureInfo.CurrentCulture, out q))
                        {
                            prod.Cantidad = q;
                        }
                        else
                        {
                            // try numeric cell value using ClosedXML TryGetValue
                            try
                            {
                                if (row.Cell(colCantidad).TryGetValue<double>(out var dv))
                                {
                                    prod.Cantidad = dv;
                                }
                                else if (row.Cell(colCantidad).TryGetValue<int>(out var iv))
                                {
                                    prod.Cantidad = iv;
                                }
                                else if (row.Cell(colCantidad).TryGetValue<decimal>(out var decv))
                                {
                                    prod.Cantidad = (double)decv;
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
                if (!string.IsNullOrWhiteSpace(prod.Codigo) || !string.IsNullOrWhiteSpace(prod.Nombre))
                    result.Add(prod);
            }

            return result;
        }
    }
}
