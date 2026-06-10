using System;
using System.IO;

namespace ProjectReport.Helpers
{
    public static class ConfigHelper
    {
        private static string? _connectionString;

        public static string GetConnectionString(string name = "DefaultConnection")
        {
            if (_connectionString != null)
                return _connectionString;

            // Build the DB path relative to the executable directory
            string baseDir = AppContext.BaseDirectory;
            string dbPath = Path.Combine(baseDir, "projectReport.db");
            _connectionString = $"Data Source={dbPath};Cache=Shared";
            return _connectionString;
        }

        public static void SaveConnectionString(string name, string connectionString)
        {
            // Override the in-memory connection string
            _connectionString = connectionString;
        }
    }
}

