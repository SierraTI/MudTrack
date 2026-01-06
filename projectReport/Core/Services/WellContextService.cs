using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models;

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
        private readonly Dictionary<string, bool> _stepCompletionStatus = new();

        public event EventHandler<Well>? WellChanged;
        public event EventHandler<double>? DepthUpdated;
        public event EventHandler<double>? MudDensityUpdated;

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
    }
}
