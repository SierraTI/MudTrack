using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace ProjectReport.Services
{
    public class DatabaseService : IDisposable
    {
        private SqliteConnection? _connection;
        private bool _disposed = false;
 
        public DatabaseService()
        {
            // Auto-connect using default connection string
            string connString = ProjectReport.Helpers.ConfigHelper.GetConnectionString();
            if (!string.IsNullOrEmpty(connString))
            {
                Connect(connString, out _);
            }
        }

        public bool IsConnected => _connection?.State == ConnectionState.Open;

        public bool Connect(string connectionString, out string? errorMessage)
        {
            try
            {
                _connection = new SqliteConnection(connectionString);
                _connection.Open();
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool TestConnection(string connectionString)
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void EnsureConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                string connString = ProjectReport.Helpers.ConfigHelper.GetConnectionString();
                if (!string.IsNullOrEmpty(connString))
                {
                    Connect(connString, out _);
                }
                
                if (_connection == null || _connection.State != ConnectionState.Open)
                {
                    throw new InvalidOperationException("Database connection is not open");
                }
            }
        }

        public DataTable ExecuteQuery(string query, params SqliteParameter[] parameters)
        {
            EnsureConnection();

            var dataTable = new DataTable();

            using var command = new SqliteCommand(query, _connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            using var reader = command.ExecuteReader();
            dataTable.Load(reader);

            return dataTable;
        }

        public int ExecuteNonQuery(string query, params SqliteParameter[] parameters)
        {
            EnsureConnection();

            using var command = new SqliteCommand(query, _connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            return command.ExecuteNonQuery();
        }

        public object? ExecuteScalar(string query, params SqliteParameter[] parameters)
        {
            EnsureConnection();

            using var command = new SqliteCommand(query, _connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

return command.ExecuteScalar();
        }

        public int ExecuteInsertAndGetId(string insertQuery, params SqliteParameter[] parameters)
        {
            EnsureConnection();
            using var transaction = _connection.BeginTransaction();
            using var cmd = new SqliteCommand(insertQuery, _connection, transaction);
            if (parameters != null && parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
            using var cmd2 = new SqliteCommand("SELECT last_insert_rowid();", _connection, transaction);
            var val = cmd2.ExecuteScalar();
            transaction.Commit();
            return Convert.ToInt32(val);
        }

        public void Disconnect()
        {
            _connection?.Close();
            _connection = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Close();
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}

