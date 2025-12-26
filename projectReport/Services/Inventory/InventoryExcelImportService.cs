using System.Collections.Generic;
using System.IO;
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
    }

    public class InventoryExcelImportService
    {
        // Reads first worksheet and maps columns by header names (case-insensitive)
        public List<UniversalProduct> LoadUniversalProducts(string path)
        {
            var result = new List<UniversalProduct>();

            if (!File.Exists(path)) return result;

            using (var wb = new XLWorkbook(path))
            {
                var ws = wb.Worksheets.FirstOrDefault();
                if (ws == null) return result;

                var headerRow = ws.Row(1);
                var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                var headerMap = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

                for (int c = 1; c <= lastCol; c++)
                {
                    var cell = headerRow.Cell(c);
                    var text = cell.GetString().Trim();
                    if (!string.IsNullOrEmpty(text) && !headerMap.ContainsKey(text))
                        headerMap[text] = c;
                }

                // Helper to get by possible header names
                int GetCol(params string[] names)
                {
                    foreach (var n in names)
                    {
                        if (headerMap.TryGetValue(n, out var idx)) return idx;
                    }
                    return -1;
                }

                var colCodigo = GetCol("Codigo", "Codigo SIIGO", "Código", "CÓDIGO");
                var colNombre = GetCol("Nombre", "Name", "NOMBRE");
                var colCategoria = GetCol("Categoria", "Category", "CATEGORIA");
                var colPresentacion = GetCol("Presentacion", "Presentation");
                var colUnidad = GetCol("Unit", "Unidad", "UNIDAD");

                var rows = ws.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
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

                    // Only add if has code or name
                    if (!string.IsNullOrWhiteSpace(prod.Codigo) || !string.IsNullOrWhiteSpace(prod.Nombre))
                        result.Add(prod);
                }
            }

            return result;
        }
    }
}
