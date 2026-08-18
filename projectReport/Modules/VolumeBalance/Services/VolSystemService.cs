using Microsoft.Data.Sqlite;
using ProjectReport.Core.Data;
using ProjectReport.Models;
using ProjectReport.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class VolSystemService
    {
        // ============================================================
        // SINGLETON
        // ============================================================

        public static VolSystemService Instance { get; } =
            new VolSystemService();

        private VolSystemService()
        {
        }


        // ============================================================
        // FLUID OPTIONS
        // ============================================================

        public List<FluidOption> GetFluidOptionsForWell(
            int wellId)
        {
            var result =
                new List<FluidOption>();

            if (wellId <= 0)
                return result;

            try
            {
                using var db =
                    new DatabaseService();


                // ====================================================
                // OBTENER ÚLTIMO REPORTE DEL POZO
                // ====================================================

                var dtReport =
                    db.ExecuteQuery(
                        @"
                        SELECT idRep
                        FROM Report
                        WHERE idW = @wellId
                        ORDER BY idRep DESC
                        LIMIT 1
                        ",
                        new SqliteParameter(
                            "@wellId",
                            wellId));


                if (dtReport.Rows.Count == 0)
                {
                    Debug.WriteLine(
                        $"No se encontró Report para WellId={wellId}");

                    return result;
                }


                var idRep =
                    Convert.ToInt32(
                        dtReport.Rows[0]["idRep"]);


                // ====================================================
                // OBTENER FLUIDOS
                // ====================================================

                var dtFluids =
                    db.ExecuteQuery(
                        @"
                        SELECT
                            id,
                            FluidName,
                            FluidType
                        FROM ReportFluids
                        WHERE idRep = @idRep
                        ORDER BY id
                        ",
                        new SqliteParameter(
                            "@idRep",
                            idRep));


                foreach (DataRow row
                    in dtFluids.Rows)
                {
                    if (row["id"] == DBNull.Value)
                        continue;


                    var fluidName =
                        row["FluidName"]?.ToString()
                        ?? string.Empty;


                    var fluidType =
                        row["FluidType"]?.ToString()
                        ?? string.Empty;


                    if (string.IsNullOrWhiteSpace(
                        fluidName))
                    {
                        continue;
                    }


                    result.Add(
                        new FluidOption
                        {
                            Id =
                                Convert.ToInt32(
                                    row["id"]),

                            FluidName =
                                fluidName,

                            FluidType =
                                fluidType
                        });
                }


                Debug.WriteLine(
                    $"GetFluidOptionsForWell: " +
                    $"{result.Count} fluidos encontrados. " +
                    $"Well={wellId} | Report={idRep}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"GetFluidOptionsForWell ERROR: {ex}");
            }


            return result;
        }
    }
}

