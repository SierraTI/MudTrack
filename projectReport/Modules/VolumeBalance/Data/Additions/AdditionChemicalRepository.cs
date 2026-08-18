using ProjectReport.Models;
using ProjectReport.Services;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProjectReport.Modules.VolumeBalance.Data.Additions
{
    public class AdditionsChemicalRepository
    {
        private readonly DatabaseService _database;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public AdditionsChemicalRepository(
            DatabaseService database)
        {
            _database = database;
        }


        // =====================================================
        // OBTENER TODOS LOS PRODUCTOS
        // =====================================================

        public List<InventoryProduct> GetAllProducts()
        {
            var products = new List<InventoryProduct>();


            // =================================================
            // CONSULTA SQL
            // =================================================

            var table = _database.ExecuteQuery(@"
                SELECT
                    id,
                    code,
                    name,
                    description,
                    physical_state,
                    presentation,
                    package_quantity,
                    package_unit,
                    sg,
                    category,
                    status,
                    is_selected_for_report
                FROM inventory_product
                ORDER BY name;
            ");


            // =================================================
            // MAPEAR RESULTADOS
            // =================================================

            foreach (DataRow row in table.Rows)
            {
                products.Add(new InventoryProduct
                {
                    Id =
                        Convert.ToInt32(row["id"]),

                    Code =
                        row["code"]?.ToString() ?? string.Empty,

                    Name =
                        row["name"]?.ToString() ?? string.Empty,

                    Description =
                        row["description"] == DBNull.Value
                            ? null
                            : row["description"]?.ToString(),

                    PhysicalState =
                        row["physical_state"]?.ToString()
                        ?? string.Empty,

                    Presentation =
                        row["presentation"]?.ToString()
                        ?? string.Empty,

                    PackageQuantity =
                        row["package_quantity"] != DBNull.Value
                            ? Convert.ToDouble(
                                row["package_quantity"])
                            : 0,

                    PackageUnit =
                        row["package_unit"]?.ToString()
                        ?? string.Empty,

                    SG =
                        row["sg"] != DBNull.Value
                            ? Convert.ToDouble(row["sg"])
                            : null,

                    Category =
                        row["category"]?.ToString()
                        ?? string.Empty,

                    Status =
                        row["status"] != DBNull.Value &&
                        Convert.ToInt32(row["status"]) == 1,

                    IsSelectedForReport =
                        row["is_selected_for_report"] != DBNull.Value &&
                        Convert.ToInt32(
                            row["is_selected_for_report"]) == 1
                });
            }


            return products;
        }
    }
}