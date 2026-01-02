using System;
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
        /// Updates the System Global Depth. typically called from Daily Reports.
        /// </summary>
        public void UpdateSystemDepth(double newMD)
        {
            if (CurrentWell != null)
            {
                // Logic to ensure we don't accidentally decrease depth unless explicit?
                // For now, simple update.
                CurrentWell.TotalMD = newMD;
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
    }
}
