using System;
using System.Windows;
using System.Windows.Threading;

namespace ProjectReport
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Handle unhandled exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "DispatcherUnhandledException");
            MessageBox.Show($"Unhandled exception: {e.Exception.Message}\n\nSee log file for details.", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex, "CurrentDomain_UnhandledException");
                MessageBox.Show($"Unhandled exception: {ex.Message}\n\nSee log file for details.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogException(Exception ex, string source)
        {
            try
            {
                var logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);

                var logFile = System.IO.Path.Combine(logDir, "exceptions.log");
                var text = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {source}: {ex.Message}\n{ex.StackTrace}\n--- Inner: {ex.InnerException}\n\n";
                System.IO.File.AppendAllText(logFile, text);
            }
            catch { /* Swallow to avoid recursive failures */ }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up resources here
            base.OnExit(e);
        }
    }
}

