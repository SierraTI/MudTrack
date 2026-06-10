# Drilling Fluids Intelligence

Windows desktop app for drilling fluids / mud reporting: wells, geometry, volume balance, inventory, and daily reports.

## Tech stack

- **.NET 8** WPF (`net8.0-windows`)
- **SQLite** (`projectReport.db` — local file, no server)
- **MVVM** with modular feature folders

## Repository layout

```
drillingFluidsIntelligence/
├── ProjectReport.sln          # Open this in Visual Studio
├── projectReport/             # Main WPF application
├── projectReport.Tests/       # xUnit tests
├── tools/DbCreateRunner/      # DB smoke-test utility
├── samples/                   # Sample CSV data
└── projectReport.db           # SQLite database (gitignored, created at runtime)
```

## Run the app

```powershell
dotnet build projectReport\ProjectReport.csproj -c Debug
dotnet run --project projectReport\ProjectReport.csproj
```

Or open `ProjectReport.sln` in Visual Studio and press F5.

## Database

- Connection string: `projectReport\app.config` → `DefaultConnection`
- Default: `Data Source=projectReport.db;Cache=Shared`
- Schema is created on startup by `Core/Services/DatabaseInitializer.cs`
- DB file is created next to the built `.exe` (or run `tools/DbCreateRunner` for smoke tests)

## Run tests

```powershell
dotnet test projectReport.Tests\projectReport.Tests.csproj
```
