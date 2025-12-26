using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ProjectReport.Services
{
    public class DatabaseService : IDisposable
    {
        private SqlConnection? _connection;
        private bool _disposed = false;

        public bool IsConnected => _connection?.State == ConnectionState.Open;

        public bool Connect(string connectionString, out string? errorMessage)
        {
            try
            {
                _connection = new SqlConnection(connectionString);
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
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Database connection is not open");

            var dataTable = new DataTable();
            
            using var command = new SqlCommand(query, _connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            
            return dataTable;
        }

        public int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Database connection is not open");

            using var command = new SqlCommand(query, _connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            return command.ExecuteNonQuery();
        }

        public object? ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Database connection is not open");

            using var command = new SqlCommand(query, _connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            return command.ExecuteScalar();
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

