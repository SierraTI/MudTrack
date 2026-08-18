using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Modules.VolumeBalance.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class AdditionsLiquidVolViewModel
    {
        // =========================
        // LIVE COLLECTION DESDE EL SERVICIO
        // =========================
        public ObservableCollection<AdditionsLiquidVol> AdditionsLiquidVolumes =>
            AdditionsLiquidService.Instance.LiveAdditions;

        public AdditionsLiquidVolViewModel()
        {
            LoadFromVolSystem();

            //VolSystemService.Instance.PitsUpdated += OnPitsUpdated;
        }

        private void OnPitsUpdated(object? sender, EventArgs e)
        {
            SyncFromVolSystem();
        }

        // =========================
        // SYNC INCREMENTAL
        // =========================
        private void SyncFromVolSystem()
        {
            //var pits = VolSystemService.Instance.GetPits();

            // Solo tomar tanques que ya tengan un FluidSubtype configurado
            //var grouped = pits
            //    .Where(p =>
                     
            //        !string.IsNullOrWhiteSpace(p.FluidSubtype))
            //    .GroupBy(p => new
            //    {
            //        PitSystem = 1,
            //        FluidSubtype = p.FluidSubtype.Trim()
            //    })
            //    .Select(g => new
            //    {
            //        PitSystem = g.Key.PitSystem,
            //        FluidSubtype = g.Key.FluidSubtype
            //    })
            //    .ToList();

            // =========================
            // AGREGAR NUEVOS
            // =========================
            //foreach (var item in grouped)
            //{
            //    var existing = AdditionsLiquidVolumes.FirstOrDefault(x =>
            //        //x.PitSystem == item.PitSystem &&
            //        x.FluidSubtype == item.FluidSubtype);

            //    if (existing == null)
            //    {
            //        AdditionsLiquidService.Instance.Add(new AdditionsLiquidVol
            //        {
            //            //PitSystem = 1,
            //            FluidSubtype = item.FluidSubtype,
            //            Water = 0,
            //            DewateringWater = 0,
            //            OsmosisWater = 0,
            //            OilBased = 0,
            //            Iflux = 0
            //        });
            //    }
            //}

            //// =========================
            //// ELIMINAR LOS QUE YA NO EXISTEN
            //// =========================
            //var toRemove = AdditionsLiquidVolumes
            //    .Where(x => !grouped.Any(g =>
            //       // g.PitSystem == x.PitSystem &&
            //        g.FluidSubtype == x.FluidSubtype))
            //    .ToList();

            //foreach (var item in toRemove)
            //{
            //    AdditionsLiquidService.Instance.Remove(item);
            //}
        }

        private void LoadFromVolSystem()
        {
            SyncFromVolSystem();
        }

        private string NormalizePitSystem(string pitSystem)
        {
            return pitSystem?.Trim().ToLower() switch
            {
                "activo" => "Active",
                "reserva" => "Reserve",
                "otro" => "Other",
                _ => pitSystem
            };
        }
    }
}