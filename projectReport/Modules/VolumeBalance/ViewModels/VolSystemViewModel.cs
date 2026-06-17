using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ProjectReport.Core.Data;
using Microsoft.Data.Sqlite;
using ProjectReport.Models.Rig;
using ProjectReport.Modules.VolumeBalance.Models;
using ProjectReport.Services;
using ProjectReport.ViewModels;

namespace ProjectReport.Modules.VolumeBalance.ViewModels
{
    public class VolSystemViewModel : BaseViewModel, IDisposable
    {
        private readonly WellContextService _context;

        private ObservableCollection<VolSystemPit> _pits = new();

        public VolSystemViewModel()
        {
            _context = WellContextService.Instance;

            UpdatePitsFromContext();

            _context.WellChanged += OnWellChanged;
            _context.RigProfileUpdated += OnRigProfileUpdated;
        }

        public ObservableCollection<VolSystemPit> Pits
        {
            get => _pits;
            private set => SetProperty(ref _pits, value);
        }

        public void Refresh()
        {
            UpdatePitsFromContext();
        }

        public List<string> GetFluidOptionsForPit(RigPit pit)
        {
            var list = new List<string>();
            var well = _context.CurrentWell;
            if (well == null) return list;

            try
            {
                using var db = new DatabaseService();
                var repo = new CatalogRepository(db);
                // fluids assigned to this well
                var wf = repo.GetFluidsByWell(well.Id);
                list.AddRange(wf);
                // add master list too (avoid duplicates)
                var master = repo.GetFluidNames();
                foreach (var m in master)
                {
                    if (!list.Contains(m)) list.Add(m);
                }
                // include well default fluid from WellInfo if present and not already in list
                try
                {
                    var dt = "" ;
                    //if (dt.Rows.Count > 0)
                    //{
                    //    //var wellDefault = dt.Rows[0]["FluidType"]?.ToString();
                    //    //if (!string.IsNullOrEmpty(wellDefault) && !list.Contains(wellDefault))
                    //    //{
                    //    //    // place at beginning so it's easy to see
                    //    //    list.Insert(0, wellDefault);
                    //    //}
                    //}
                }
                catch { }
            }
            catch { }

            return list;
        }

        private void OnWellChanged(object? sender, ProjectReport.Models.Well? well)
        {
            UpdatePitsFromContext();
        }

        private void UpdatePitsFromContext()
        {
            Pits.Clear();

            try
            {
                var well = _context.CurrentWell;

                if (well == null)
                {
                    Debug.WriteLine("VolSystemViewModel: CurrentWell is null.");
                    return;
                }

                if (well.Id <= 0)
                {
                    Debug.WriteLine("VolSystemViewModel: Invalid Well Id.");
                    return;
                }

                using var db = new DatabaseService();
                var repo = new RigProfileRepository(db);

                var rigProfile = repo.LoadRigProfile(well.Id);

                // Load well-level fluid defaults (WellInfo.FluidType) and well fluids
                string? wellDefaultFluid = null;
                try
                {
                    var dt = "";
                    //if (dt.Rows.Count > 0)
                    //{
                    //    wellDefaultFluid = dt.Rows[0]["FluidType"]?.ToString();
                    //}
                }
                catch { }

                var catalogRepo = new CatalogRepository(db);
                var wellFluids = catalogRepo.GetFluidsByWell(well.Id);

                if (rigProfile?.Pits != null)
                {
                    foreach (var pit in rigProfile.Pits)
                    {
                        Pits.Add(new VolSystemPit
                        {
                            PitId = pit.Id,
                            PitName = pit.PitName ?? string.Empty,

                            // Valor inicial del combo
                            PitSystem = "Activo",

                            // Inicializar FluidType desde WellInfo o desde la lista de fluidos del pozo si existe
                            FluidType = !string.IsNullOrEmpty(wellDefaultFluid) ? wellDefaultFluid : (wellFluids.Count > 0 ? wellFluids[0] : string.Empty),
                            FluidSubtype = string.Empty,

                            PreviousVolume = 0,
                            CurrentVolume = 0,
                            Density = 0,

                            SourcePit = pit
                        });
                    }

                    Debug.WriteLine(
                        $"VolSystemViewModel: Loaded {rigProfile.Pits.Count} pits from DB for well {well.Id}.");
                }
                else
                {
                    Debug.WriteLine(
                        $"VolSystemViewModel: No pits found in DB for well {well.Id}.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"VolSystemViewModel: Error loading pits from DB. {ex.Message}");
            }
        }

        private void OnRigProfileUpdated(object? sender, RigProfileUpdatedEventArgs e)
        {
            UpdatePitsFromContext();
        }

        public void Dispose()
        {
            _context.WellChanged -= OnWellChanged;
            _context.RigProfileUpdated -= OnRigProfileUpdated;
        }
    }
}