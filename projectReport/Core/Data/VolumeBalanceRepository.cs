using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Services;

namespace ProjectReport.Core.Data
{
    public class VolumeBalanceRepository
    {
        private readonly DatabaseService _db;

        public VolumeBalanceRepository(DatabaseService db)
        {
            _db = db;
        }

        public void SaveEvents(int reportId, IEnumerable<VolumeBalanceEvent> events)
        {
            if (reportId <= 0) return;

            // 1. Clear existing events for this report
            // Delete order: Chemicals -> Transfers -> Events (to avoid FK issues)
            string delChems = "DELETE FROM EventChemical WHERE idVE IN (SELECT idVE FROM VolumeEvent WHERE idRep = @idRep)";
            string delTransfers = "DELETE FROM EventTransfer WHERE idVE IN (SELECT idVE FROM VolumeEvent WHERE idRep = @idRep)";
            string delEvents = "DELETE FROM VolumeEvent WHERE idRep = @idRep";

            _db.ExecuteNonQuery(delChems, new SqlParameter("@idRep", reportId));
            _db.ExecuteNonQuery(delTransfers, new SqlParameter("@idRep", reportId));
            _db.ExecuteNonQuery(delEvents, new SqlParameter("@idRep", reportId));

            // 2. Insert new events
            string eventQuery = @"INSERT INTO VolumeEvent (idRep, EventTime, Description, TotalSurfaceVol, HoleVol, TotalSystemVol)
                                 OUTPUT INSERTED.idVE
                                 VALUES (@idRep, @time, @desc, @surf, @hole, @sys)";

            string chemQuery = @"INSERT INTO EventChemical (idVE, ProductName, Quantity, Unit, SG, VolumeBbl, PPB)
                                VALUES (@idVE, @name, @qty, @unit, @sg, @vol, @ppb)";

            string transferQuery = @"INSERT INTO EventTransfer (idVE, FromTank, ToTank, VolumeBbl)
                                   VALUES (@idVE, @from, @to, @vol)";

            //foreach (var e in events)
            //{
            //    int eventId = Convert.ToInt32(_db.ExecuteScalar(eventQuery,
            //        new SqlParameter("@idRep", reportId),
            //        new SqlParameter("@time", e.Timestamp),
            //        new SqlParameter("@desc", e.Notes ?? (object)DBNull.Value),
            //        new SqlParameter("@surf", e.TotalPreviousPitVol),
            //        new SqlParameter("@hole", e.TotalWellboreBbl),
            //        new SqlParameter("@sys", e.TotalPreviousPitVol + e.TotalWellboreBbl)));

            //    foreach (var chem in e.Chemicals)
            //    {
            //        _db.ExecuteNonQuery(chemQuery,
            //            new SqlParameter("@idVE", eventId),
            //            new SqlParameter("@name", chem.ProductName ?? (object)DBNull.Value),
            //            new SqlParameter("@qty", chem.QuantityLbs),
            //            new SqlParameter("@unit", "lbs"), // Assuming lbs based on QuantityLbs
            //            new SqlParameter("@sg", chem.SG),
            //            new SqlParameter("@vol", chem.VolumeBbl),
            //            new SqlParameter("@ppb", chem.ConcentrationPpb));
            //    }

            //    foreach (var trans in e.Transfers)
            //    {
            //        _db.ExecuteNonQuery(transferQuery,
            //            new SqlParameter("@idVE", eventId),
            //            new SqlParameter("@from", trans.FromTank ?? (object)DBNull.Value),
            //            new SqlParameter("@to", trans.ToTank ?? (object)DBNull.Value),
            //            new SqlParameter("@vol", trans.VolumeBbl));
            //    }
            //}
        }

        public List<VolumeBalanceEvent> LoadEvents(int reportId)
        {
            string query = "SELECT * FROM VolumeEvent WHERE idRep = @idRep ORDER BY EventTime ASC";
            DataTable dt = _db.ExecuteQuery(query, new SqlParameter("@idRep", reportId));
            
            var results = new List<VolumeBalanceEvent>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    int eventId = (int)dr["idVE"];
            //    var ev = new VolumeBalanceEvent
            //    {
            //        Timestamp = (DateTime)dr["EventTime"],
            //        Notes = dr["Description"]?.ToString() ?? string.Empty,
            //        StringVolBbl = Convert.ToDouble(dr["HoleVol"]) / 2, // Approximate for now
            //        AnnulusVolBbl = Convert.ToDouble(dr["HoleVol"]) / 2  // Approximate for now
            //    };

            //    // Load chemicals
            //    DataTable dtChems = _db.ExecuteQuery("SELECT * FROM EventChemical WHERE idVE = @idVE", new SqlParameter("@idVE", eventId));
            //    foreach (DataRow drC in dtChems.Rows)
            //    {
            //        ev.Chemicals.Add(new EventChemical
            //        {
            //            ProductName = drC["ProductName"]?.ToString() ?? string.Empty,
            //            QuantityLbs = Convert.ToDouble(drC["Quantity"]),
            //            SG = Convert.ToDouble(drC["SG"]),
            //            VolumeBbl = Convert.ToDouble(drC["VolumeBbl"])
            //        });
            //    }

            //    // Load transfers
            //    DataTable dtTrans = _db.ExecuteQuery("SELECT * FROM EventTransfer WHERE idVE = @idVE", new SqlParameter("@idVE", eventId));
            //    foreach (DataRow drT in dtTrans.Rows)
            //    {
            //        ev.Transfers.Add(new EventTransfer
            //        {
            //            FromTank = drT["FromTank"]?.ToString() ?? string.Empty,
            //            ToTank = drT["ToTank"]?.ToString() ?? string.Empty,
            //            VolumeBbl = Convert.ToDouble(drT["VolumeBbl"])
            //        });
            //    }

            //    results.Add(ev);
            //}
            return results;
        }
    }
}
