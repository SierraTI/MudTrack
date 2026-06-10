using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProjectReport.Models
{
    public class Project : BaseModel
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    LastModified = DateTime.Now;
            }
        }

        // Legacy property for backward compatibility
        private string _wellName = string.Empty;
        public string WellName
        {
            get => _wellName;
            set
            {
                if (SetProperty(ref _wellName, value))
                    LastModified = DateTime.Now;
            }
        }

        private DateTime _lastModified = DateTime.Now;
        public DateTime LastModified
        {
            get => _lastModified;
            private set => SetProperty(ref _lastModified, value);
        }

        // Multi-well support
        private int? _activeWellId;
        public int? ActiveWellId
        {
            get => _activeWellId;
            set
            {
                if (SetProperty(ref _activeWellId, value))
                {
                    OnPropertyChanged(nameof(ActiveWell));
                }
            }
        }

        public ObservableCollection<Well> Wells { get; set; } = new ObservableCollection<Well>();

        /// <summary>
        /// Gets the currently active well
        /// </summary>
        public Well? ActiveWell
        {
            get
            {
                if (ActiveWellId.HasValue)
                {
                    return Wells.FirstOrDefault(w => w.Id == ActiveWellId.Value);
                }
                return Wells.FirstOrDefault();
            }
        }

        /// <summary>
        /// Sets the active well by ID
        /// </summary>
        public void SetActiveWell(int wellId)
        {
            var well = Wells.FirstOrDefault(w => w.Id == wellId);
            if (well != null)
            {
                ActiveWellId = wellId;
            }
        }

        /// <summary>
        /// Adds a new well to the project
        /// </summary>
        public void AddWell(Well well)
        {
            if (well != null)
            {
                Wells.Add(well);
                LastModified = DateTime.Now;

                // If this is the first well, make it active
                if (Wells.Count == 1)
                {
                    ActiveWellId = well.Id;
                }
            }
        }

        /// <summary>
        /// Removes a well from the project
        /// </summary>
        public void RemoveWell(int wellId)
        {
            var well = Wells.FirstOrDefault(w => w.Id == wellId);
            if (well != null)
            {
                Wells.Remove(well);
                LastModified = DateTime.Now;

                // If we removed the active well, select another
                if (ActiveWellId == wellId)
                {
                    ActiveWellId = Wells.FirstOrDefault()?.Id;
                }
            }
        }
    }
}
