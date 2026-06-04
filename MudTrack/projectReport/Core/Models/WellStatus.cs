namespace ProjectReport.Models
{
    /// <summary>
    /// Represents the status of a well in the project lifecycle
    /// </summary>
    public enum WellStatus
    {
        /// <summary>
        /// Well created, minimal data entered
        /// </summary>
        Draft,

        /// <summary>
        /// Active work, data being entered
        /// </summary>
        InProgress,

        /// <summary>
        /// All data finalized, ready for reports
        /// </summary>
        Completed,

        /// <summary>
        /// Historical well, hidden from default view
        /// </summary>
        Archived
    }
}
