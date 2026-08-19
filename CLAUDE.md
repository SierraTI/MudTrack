# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build the application
```powershell
dotnet build projectReport\ProjectReport.csproj -c Debug
```

### Run the application
```powershell
dotnet run --project projectReport\ProjectReport.csproj
```
Alternatively, open `ProjectReport.sln` in Visual Studio and press F5.

### Run all tests
```powershell
dotnet test projectReport.Tests\projectReport.Tests.csproj
```

### Run a single test (using test filter)
```powershell
dotnet test projectReport.Tests\projectReport.Tests.csproj --filter "FullyQualifiedName~<Namespace.Class.Method>"
```
Replace `<Namespace.Class.Method>` with the specific test method.

### Database
- The app uses SQLite (`projectReport.db`) created at runtime next to the built `.exe`.
- Connection string defined in `projectReport\App.config` under `DefaultConnection`.
- Schema is initialized on startup via `Core/Services/DatabaseInitializer.cs`.
- For smoke tests, a utility exists: `tools/DbCreateRunner`.

### Cleanup after merge
If you see duplicate folders (`projectReport/projectReport/` or `MudTrack/`) left from a merge, close the running app and delete them:
```powershell
Remove-Item -Recurse -Force projectReport\projectReport, MudTrack
```

## Architecture Overview

### Solution Structure
- **ProjectReport.sln**: Main solution containing two projects:
  - `projectReport`: WPF desktop application (net8.0-windows)
  - `projectReport.Tests`: xUnit test project

### High-Level Architecture
The application follows the **MVVM (Model-View-ViewModel)** pattern with a modular feature-based folder structure.

#### Core Layer (`projectReport\Core`)
- **Models**: Base model (`BaseModel.cs`) and shared domain models (`Project.cs`, `Report.cs`, `Well.cs`, `User.cs`, etc.) and rig-related models.
- **Services**: Singleton services handling cross-cutting concerns:
  - `DatabaseService.cs`: SQLite database access.
  - `DatabaseInitializer.cs`: Creates schema on first launch.
  - `HydraulicsCalculationService.cs`: Core calculations for mud reporting.
  - `WellContextService.cs`: Manages current well/context state.
  - `NavigationService.cs`: Handles page navigation.
  - `ToastNotificationService.cs`: UI notifications.
  - `DataPersistenceService.cs`: Handles data saving/loading.
- **Helpers**: Utility classes (`ConfigHelper.cs`, `RelayCommand.cs`).
- **Converters**: Value converters for XAML binding (e.g., `BooleanToVisibilityConverter`, number formatters, enum-to-string).
- **Views**: Shared UI components:
  - `Common/`: Reusable dialogs (e.g., `ConnectionDialog`).
  - `Controls/`: Custom controls (e.g., `ToastNotificationControl`).
  - `Dialogs/`: Generic input dialogs.

#### Feature Modules (`projectReport\Modules`)
Each feature is encapsulated in its own folder under `Modules`, containing:
- **Models**: Domain models specific to the feature (e.g., Geometry models for drill string, mud motor, PDC bit).
- **ViewModels**: MVVM ViewModels exposing data and commands to the view.
- **Views**: XAML views for the feature (often organized in subfolders).
- **Services**: Services scoped to the feature (if any).

Current modules include:
- `Geometry`: Wellbore geometry, volume calculations, drill string components.
- `Inventory`: Mud inventory tracking, fluid lines, tickets.
- `ReportDetail`: Displaying and editing daily reports.
- `ReportWizard`: Guided report creation.
- `RigProfile`: Managing rig configurations.
- `Shell`: Main application shell (likely hosts navigation).
- `VolumeBalance`: Mud volume tracking and balancing.
- `Well`: Well master data management.
- `Home`: Landing page or dashboard.

#### Data Access
Repositories are located in `projectReport\Core\Data` (e.g., `WellRepository.cs`, `ReportRepository.cs`) and encapsulate SQLite operations via `Microsoft.Data.Sqlite`.

### Key Technologies
- **.NET 8 WPF** (`net8.0-windows`)
- **SQLite** for local file-based storage.
- **CommunityToolkit.Mvvm** (version 8.0.0) for MVVM helpers.
- **LiveCharts.Wpf** for charting.
- **ClosedXML** for Excel report generation.
- **PdfiumViewer** for PDF display.
- **Microsoft.Web.WebView2** for embedded web content (if used).

### Conventions
- ViewModels typically inherit from `Core\ViewModels\BaseViewModel.cs` (which likely implements `INotifyPropertyChanged`).
- Commands often use the shared `RelayCommand.cs` implementation.
- Views are XAML with code-behind limited to UI logic only.
- Dependency injection is not explicitly shown; services appear to be instantiated as singletons or via locators (check `App.xaml.cs` for details).

Understanding this structure will allow you to navigate the codebase efficiently: look in `Modules` for feature-specific logic, `Core` for shared infrastructure, and the test project for validation.