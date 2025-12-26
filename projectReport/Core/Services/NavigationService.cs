using System;
using System.Windows.Input;
using ProjectReport.ViewModels;
using ProjectReport.Views;

namespace ProjectReport.Services
{
    /// <summary>
    /// Navigation service for managing module transitions
    /// </summary>
    public class NavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        private NavigationService() { }

        /// <summary>
        /// Navigate to Home module
        /// </summary>
        public void NavigateToHome()
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs(NavigationTarget.Home));
        }

        /// <summary>
        /// Navigate to Well Data module for a specific well
        /// </summary>
        public void NavigateToWellData(int wellId)
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs(NavigationTarget.WellData, wellId));
        }

        /// <summary>
        /// Navigate to Geometry module for a specific well
        /// </summary>
        public void NavigateToGeometry(int wellId)
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs(NavigationTarget.Geometry, wellId));
        }

            /// <summary>
            /// Navigate to Well Dashboard for a specific well
            /// </summary>
            public void NavigateToWellDashboard(int wellId)
            {
                NavigationRequested?.Invoke(this, new NavigationEventArgs(NavigationTarget.WellDashboard, wellId));
            }
        /// <summary>
        /// Navigate back to previous module
        /// </summary>
        public void NavigateBack()
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs(NavigationTarget.Back));
        }
    }

    /// <summary>
    /// Navigation targets
    /// </summary>
    public enum NavigationTarget
    {
        Home,
        WellData,
        Geometry,
        WellDashboard,
        Back
    }
    
    /// <summary>
    /// Navigation event arguments
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public NavigationTarget Target { get; }
        public int? WellId { get; }

        public NavigationEventArgs(NavigationTarget target, int? wellId = null)
        {
            Target = target;
            WellId = wellId;
        }
    }
}
