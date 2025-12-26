using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ProjectReport.Services;

namespace ProjectReport.Views.Modals
{
    public partial class ValidationResultModal : Window
    {
        public class ItemViewModel
        {
            public string Message { get; set; }
            public Brush SeverityColor { get; set; }
        }

        public class GroupViewModel
        {
            public string SectionName { get; set; } = string.Empty;
            public string Icon { get; set; } = "❌";
            public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();
        }

        public List<GroupViewModel> GroupedItems { get; private set; }
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool CanContinue { get; private set; }

        public bool ContinueConfirmed { get; private set; } = false;

        private readonly GeometryValidationService.ValidationResult _validationResult;

        public ValidationResultModal(GeometryValidationService.ValidationResult result)
        {
            InitializeComponent();
            
            _validationResult = result; // Store for copy functionality
            
            ErrorCount = result.Items.Count(x => x.Severity == GeometryValidationService.ValidationSeverity.Error);
            WarningCount = result.Items.Count(x => x.Severity == GeometryValidationService.ValidationSeverity.Warning);
            
            // Allow continue ONLY if there are NO Critical Errors, but there ARE Warnings
            CanContinue = ErrorCount == 0 && WarningCount > 0;

            // Group logic
            GroupedItems = result.Items
                .GroupBy(e => new { e.ComponentId, e.ComponentName })
                .Select(g => 
                {
                    bool hasError = g.Any(x => x.Severity == GeometryValidationService.ValidationSeverity.Error);
                    string icon = hasError ? "❌" : "⚠️";
                    string sectionInfo = (g.Key.ComponentId == "-") ? "General" : $"Sección {g.Key.ComponentId} - {g.Key.ComponentName}";
                    
                    return new GroupViewModel
                    {
                        SectionName = sectionInfo,
                        Icon = icon,
                        Items = g.Select(x => new ItemViewModel 
                        { 
                            Message = x.Message,
                            SeverityColor = x.Severity == GeometryValidationService.ValidationSeverity.Error ? Brushes.Red : Brushes.Orange
                        }).ToList()
                    };
                })
                .ToList();
                
            DataContext = this;
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            ContinueConfirmed = true;
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var report = new StringBuilder();
                
                // Header
                report.AppendLine("═══════════════════════════════════════════════════════════");
                report.AppendLine("    WELLBORE GEOMETRY VALIDATION REPORT");
                report.AppendLine("    Sierra Alta Project Report Manager");
                report.AppendLine("═══════════════════════════════════════════════════════════");
                report.AppendLine();
                report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine();
                
                // Summary Statistics
                report.AppendLine("SUMMARY");
                report.AppendLine("───────────────────────────────────────────────────────────");
                report.AppendLine($"  Critical Errors:  {ErrorCount}  🔴 (Blocks saving)");
                report.AppendLine($"  Warnings:         {WarningCount}  🟡 (Can save with confirmation)");
                report.AppendLine();
                
                // Validation Items
                report.AppendLine("VALIDATION ISSUES");
                report.AppendLine("───────────────────────────────────────────────────────────");
                report.AppendLine();
                
                foreach (var group in GroupedItems)
                {
                    report.AppendLine($"{group.Icon} {group.SectionName}");
                    report.AppendLine();
                    
                    foreach (var item in group.Items)
                    {
                        string severityTag = item.SeverityColor == Brushes.Red ? "[ERROR]" : "[WARNING]";
                        report.AppendLine($"  {severityTag} {item.Message}");
                    }
                    
                    report.AppendLine();
                }
                
                // Footer
                report.AppendLine("═══════════════════════════════════════════════════════════");
                report.AppendLine();
                
                if (ErrorCount > 0)
                {
                    report.AppendLine("⚠️ ACTION REQUIRED:");
                    report.AppendLine("   Critical errors must be fixed before the wellbore");
                    report.AppendLine("   geometry can be saved.");
                }
                else if (WarningCount > 0)
                {
                    report.AppendLine("ℹ️ WARNINGS DETECTED:");
                    report.AppendLine("   You can save with warnings by clicking");
                    report.AppendLine("   'Save with Warnings' in the dialog.");
                }
                
                // Copy to clipboard
                Clipboard.SetText(report.ToString());
                
                // Show success message
                MessageBox.Show(
                    "Validation report copied to clipboard successfully!",
                    "Copy Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy report to clipboard: {ex.Message}",
                    "Copy Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
