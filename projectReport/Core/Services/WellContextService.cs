using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models;
using ProjectReport.Models.Rig;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Services
{
    /// <summary>
    /// Singleton service to hold the shared state of the application (Current Well, Project Context).
    /// Acts as the 'Thread' connecting all modules.
    /// </summary>
    public class WellContextService
    {
        private static WellContextService? _instance;
        public static WellContextService Instance => _instance ??= new WellContextService();

        private WellContextService() { }

        private Project? _currentProject;
        private Well? _currentWell;
        private double _currentDepth;
        private double _currentFlowRate;
        private readonly Dictionary<string, bool> _stepCompletionStatus = new();
        private List<ChemicalItem> _currentSelectedChemicals = new();

        public event EventHandler<Well>? WellChanged;
        public event EventHandler<double>? DepthUpdated;
        public event EventHandler<double>? MudDensityUpdated;
        public event EventHandler<double>? FlowRateUpdated;
        
        /// <summary>
        /// Event fired when report thermal data (MaxBHT and TVD) changes
        /// </summary>
        public event EventHandler<ReportThermalDataEventArgs>? ReportThermalDataUpdated;

        /// <summary>
        /// Event fired when Geometry module recalculates totals.
        /// VolumeBalance subscribes to this for auto-population.
        /// </summary>
        public event EventHandler<GeometryDataUpdatedEventArgs>? GeometryDataUpdated;

        /// <summary>
        /// Event fired when Rig Profile pit data changes.
        /// VolumeBalance subscribes to auto-populate surface tanks.
        /// </summary>
        public event EventHandler<RigProfileUpdatedEventArgs>? RigProfileUpdated;

        /// <summary>
        /// Event fired when chemicals are selected and saved from the Inventory module.
        /// VolumeBalance subscribes to add these to its additions table.
        /// </summary>
        public event EventHandler<ChemicalSelectionUpdatedEventArgs>? ChemicalSelectionUpdated;

        /// <summary>
        /// Last live selection from Chemical List (includes custom/session products).
        /// </summary>
        public IReadOnlyList<ChemicalItem> CurrentSelectedChemicals => _currentSelectedChemicals;

        public Project? CurrentProject
        {
            get => _currentProject;
            set => _currentProject = value;
        }

        public Well? CurrentWell
        {
            get => _currentWell;
            set
            {
                if (_currentWell != value)
                {
                    _currentWell = value;
                    WellChanged?.Invoke(this, _currentWell!);
                }
            }
        }

        /// <summary>
        /// Current drilling depth from Daily Reports. Used for validation and dynamic scaling.
        /// </summary>
        public double CurrentDepth
        {
            get => _currentDepth;
            set => _currentDepth = value;
        }

        public double CurrentFlowRate
        {
            get => _currentFlowRate;
            set => _currentFlowRate = value;
        }

        /// <summary>
        /// Updates the System Global Depth. typically called from Daily Reports.
        /// </summary>
        public void UpdateSystemDepth(double newMD)
        {
            if (CurrentWell != null)
            {
                // Logic to ensure we don't accidentally decrease depth unless explicit?
                // For now, simple update.
                CurrentWell.TotalMD = newMD;
                CurrentDepth = newMD; // Also update CurrentDepth for validation
                DepthUpdated?.Invoke(this, newMD);
            }
        }

        /// <summary>
        /// Updates the current active Mud Density.
        /// </summary>
        public void UpdateMudDensity(double density)
        {
            // If we had a property for this in Well, we'd update it.
            // For now, just firing the event for Geometry/WellTest to consume.
            MudDensityUpdated?.Invoke(this, density);
        }

        /// <summary>
        /// Updates the current active Flow Rate (GPM).
        /// </summary>
        public void UpdateFlowRate(double gpm)
        {
            CurrentFlowRate = gpm;
            FlowRateUpdated?.Invoke(this, gpm);
        }

        /// <summary>
        /// Marks a module step as complete in the master flow.
        /// </summary>
        public void MarkStepComplete(string stepName)
        {
            _stepCompletionStatus[stepName] = true;
        }

        /// <summary>
        /// Checks if a module step has been completed.
        /// </summary>
        public bool IsStepComplete(string stepName)
        {
            return _stepCompletionStatus.ContainsKey(stepName) && _stepCompletionStatus[stepName];
        }

        /// <summary>
        /// Gets a list of missing/skipped steps in the master flow sequence.
        /// </summary>
        public List<string> GetMissingSteps()
        {
            var requiredSteps = new[] 
            { 
                "Dashboard", 
                "DailyReport", 
                "WellboreGeometry", 
                "DrillString", 
                "Survey", 
                "ThermalGradient", 
                "WellTest" 
            };

            return requiredSteps.Where(step => !IsStepComplete(step)).ToList();
        }

        /// <summary>
        /// Validates that wellbore depth does not exceed current drilling depth.
        /// </summary>
        public string? ValidateDepthConsistency(double wellboreBottomMD)
        {
            if (CurrentDepth > 0 && wellboreBottomMD > CurrentDepth)
            {
                return $"Error: Wellbore cannot be deeper than current drilling depth ({CurrentDepth:F0} ft)";
            }
            return null;
        }

        /// <summary>
        /// Notifies subscribers when report thermal data (MaxBHT and TVD) is updated
        /// </summary>
        public void NotifyReportThermalDataUpdated(double? reportTVD, double? reportMaxBHT)
        {
            ReportThermalDataUpdated?.Invoke(this, new ReportThermalDataEventArgs(reportTVD, reportMaxBHT));
        }

        /// <summary>
        /// Called by GeometryViewModel.RecalculateTotals() to broadcast updated wellbore/drill-string volumes.
        /// </summary>
        public void PublishGeometryData(
            double holeCapacity,
            double stringDisplacement,
            double stringInternalVolume,
            double annularVolume,
            double theoreticalWellbore)
        {
            GeometryDataUpdated?.Invoke(this, new GeometryDataUpdatedEventArgs(
                holeCapacity, stringDisplacement, stringInternalVolume, annularVolume, theoreticalWellbore));
        }

        /// <summary>
        /// Called by RigProfileViewModel when pits change, to broadcast active tank list.
        /// </summary>
        public void PublishRigProfilePits(IList<RigPit> activePits)
        {
            RigProfileUpdated?.Invoke(this, new RigProfileUpdatedEventArgs(activePits));
        }

        /// <summary>
        /// Called by ChemicalListViewModel when user clicks "SAVE" to broadcast selected chemicals.
        /// </summary>
        public void PublishChemicalSelection(IList<ChemicalItem> selectedItems)
        {
            _currentSelectedChemicals = (selectedItems ?? new List<ChemicalItem>())
                .Where(i => i != null)
                .Select(i => new ChemicalItem
                {
                    Code = i.Code ?? string.Empty,
                    Name = i.Name ?? string.Empty,
                    Description = i.Description ?? string.Empty,
                    PhysicalState = i.PhysicalState ?? string.Empty,
                    Presentation = i.Presentation ?? string.Empty,
                    Quantity = i.Quantity,
                    Unit = i.Unit ?? string.Empty,
                    SG = i.SG,
                    Category = i.Category ?? string.Empty,
                    UnitPrice = i.UnitPrice,
                    IsSelected = i.IsSelected
                })
                .ToList();

            ChemicalSelectionUpdated?.Invoke(this, new ChemicalSelectionUpdatedEventArgs(_currentSelectedChemicals));
        }
    }

    /// <summary>
    /// Event arguments for report thermal data updates
    /// </summary>
    public class ReportThermalDataEventArgs : EventArgs
    {
        public double? ReportTVD { get; }
        public double? ReportMaxBHT { get; }

        public ReportThermalDataEventArgs(double? reportTVD, double? reportMaxBHT)
        {
            ReportTVD = reportTVD;
            ReportMaxBHT = reportMaxBHT;
        }
    }

    /// <summary>
    /// Carries wellbore and drill-string calculated volumes from the Geometry module.
    /// </summary>
    public class GeometryDataUpdatedEventArgs : EventArgs
    {
        /// <summary>Total capacity of the empty wellbore (bbl)</summary>
        public double HoleCapacity { get; }
        /// <summary>Volume of steel in the drill string — open-end displacement (bbl)</summary>
        public double StringDisplacement { get; }
        /// <summary>Internal volume of the drill string at bit depth (bbl)</summary>
        public double StringInternalVolume { get; }
        /// <summary>Active annular volume at bit depth (bbl)</summary>
        public double AnnularVolume { get; }
        /// <summary>HoleCapacity minus StringDisplacement = fluid in hole (bbl)</summary>
        public double TheoreticalWellbore { get; }

        public GeometryDataUpdatedEventArgs(
            double holeCapacity,
            double stringDisplacement,
            double stringInternalVolume,
            double annularVolume,
            double theoreticalWellbore)
        {
            HoleCapacity = holeCapacity;
            StringDisplacement = stringDisplacement;
            StringInternalVolume = stringInternalVolume;
            AnnularVolume = annularVolume;
            TheoreticalWellbore = theoreticalWellbore;
        }
    }

    /// <summary>
    /// Carries the list of selected chemicals from Inventory to Volume Balance.
    /// </summary>
    public class ChemicalSelectionUpdatedEventArgs : EventArgs
    {
        public IList<ChemicalItem> SelectedItems { get; }

        public ChemicalSelectionUpdatedEventArgs(IList<ChemicalItem> selectedItems)
        {
            SelectedItems = selectedItems ?? new List<ChemicalItem>();
        }
    }

    /// <summary>
    /// Carries active pit list from the Rig Profile module.
    /// </summary>
    public class RigProfileUpdatedEventArgs : EventArgs
    {
        public IList<RigPit> ActivePits { get; }

        public RigProfileUpdatedEventArgs(IList<RigPit> activePits)
        {
            ActivePits = activePits ?? new List<RigPit>();
        }
    }
}
