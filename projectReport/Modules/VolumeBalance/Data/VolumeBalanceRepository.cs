using System;
using Microsoft.Data.Sqlite;
using ProjectReport.Services;

namespace ProjectReport.Modules.VolumeBalance.Data
{
    public class VolumeBalanceRepository
    {
        private readonly DatabaseService _db;

        public VolumeBalanceRepository()
        {
            _db = new DatabaseService();
        }

        // ============================================================
        // OBTENER EL BALANCE ABIERTO O CREAR UNO NUEVO
        // ============================================================
        //
        // REGLA DE NEGOCIO:
        //
        // Un pozo solo puede tener un Balance de Volúmenes abierto.
        //
        // Si existe un balance abierto:
        //     -> Devuelve el ID existente.
        //
        // Si no existe:
        //     -> Crea un nuevo balance.
        //     -> Estado inicial = Open.
        //     -> Devuelve el ID generado.
        //
        // IMPORTANTE:
        //
        // reportDate y shift solo se utilizan al crear
        // un nuevo balance.
        //
        // Para buscar un balance existente solamente se utiliza:
        //
        //     well_id
        //     status = 'Open'
        //
        // ============================================================

        public int GetOrCreate(
            int wellId,
            string reportDate,
            string shift)
        {
            // ========================================================
            // 1. BUSCAR SI EL POZO YA TIENE UN BALANCE ABIERTO
            // ========================================================

            var result = _db.ExecuteScalar(@"
                SELECT volume_balance_id
                FROM volume_balance
                WHERE well_id = @well_id
                  AND status = 'Open'
                ORDER BY volume_balance_id DESC
                LIMIT 1;
            ",
                new SqliteParameter(
                    "@well_id",
                    wellId
                )
            );

            // ========================================================
            // 2. SI EXISTE UN BALANCE ABIERTO
            //    DEVOLVER SU ID
            // ========================================================

            if (result != null &&
                result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            // ========================================================
            // 3. SI NO EXISTE UN BALANCE ABIERTO
            //    CREAR UNO NUEVO
            // ========================================================

            return _db.ExecuteInsertAndGetId(@"
                INSERT INTO volume_balance
                (
                    well_id,
                    report_date,
                    shift,
                    status,
                    engineer,
                    remarks,
                    created_by,
                    created_date
                )
                VALUES
                (
                    @well_id,
                    @report_date,
                    @shift,
                    @status,
                    @engineer,
                    @remarks,
                    @created_by,
                    @created_date
                );
            ",
                // Pozo
                new SqliteParameter(
                    "@well_id",
                    wellId
                ),

                // Fecha de inicio del balance
                new SqliteParameter(
                    "@report_date",
                    reportDate
                ),

                // Turno en el que se inició el balance
                new SqliteParameter(
                    "@shift",
                    shift
                ),

                // Estado inicial
                new SqliteParameter(
                    "@status",
                    "Open"
                ),

                // Ingeniero
                new SqliteParameter(
                    "@engineer",
                    DBNull.Value
                ),

                // Observaciones
                new SqliteParameter(
                    "@remarks",
                    DBNull.Value
                ),

                // Usuario que crea el balance
                new SqliteParameter(
                    "@created_by",
                    Environment.UserName
                ),

                // Fecha y hora de creación
                new SqliteParameter(
                    "@created_date",
                    DateTime.Now
                )
            );
        }
    }
}