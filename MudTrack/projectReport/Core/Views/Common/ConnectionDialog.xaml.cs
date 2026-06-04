using System;
using System.Windows;
using ProjectReport.Services;

namespace ProjectReport.Views
{
    public partial class ConnectionDialog : Window
    {
        private readonly DatabaseService _databaseService;
        public string ConnectionString { get; private set; } = string.Empty;

        public ConnectionDialog()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            
            // Handle authentication type change
            AuthComboBox.SelectionChanged += AuthComboBox_SelectionChanged;
        }

        private void AuthComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (AuthComboBox.SelectedIndex == 0)
            {
                SqlAuthGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                SqlAuthGrid.Visibility = Visibility.Visible;
            }
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Testing connection...");
                
                // Build connection string
                string connectionString = BuildConnectionString();
                
                // Test connection
                bool success = _databaseService.TestConnection(connectionString);
                
                if (success)
                {
                    MessageBox.Show("Connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatus("Connection test successful");
                }
                else
                {
                    MessageBox.Show("Connection failed. Please check your settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("Connection test failed");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectionString = BuildConnectionString();
                
                if (_databaseService.Connect(ConnectionString, out string? errorMessage))
                {
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show($"Failed to establish connection: {errorMessage ?? "Unknown error"}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"Connection failed: {errorMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private string BuildConnectionString()
        {
            string server = ServerTextBox.Text.Trim();
            string database = DatabaseTextBox.Text.Trim();
            
            if (AuthComboBox.SelectedIndex == 0) // Windows Authentication
            {
                return $"Server={server};Database={database};Integrated Security=true;TrustServerCertificate=true";
            }
            else // SQL Server Authentication
            {
                string username = UsernameTextBox.Text.Trim();
                string password = PasswordBox.Password;
                return $"Server={server};Database={database};User Id={username};Password={password};TrustServerCertificate=true";
            }
        }

        private void UpdateStatus(string message)
        {
            // Status can be updated here if a status label is added
        }
    }
}


