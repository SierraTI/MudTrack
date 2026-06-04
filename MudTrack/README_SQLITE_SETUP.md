MudTrack — SQLite-only setup

Summary
- MudTrack now uses SQLite exclusively. PostgreSQL and SQL Server artifacts were removed.
- App connection string is in projectReport\app.config as DefaultConnection (Data Source=projectReport.db).
- Database schema is created at app startup by ProjectReport.Core.Services.DatabaseInitializer.

Initializing the DB
- Run the WPF app or execute Tools\DbCreateRunner to create projectReport\projectReport.db and DDL.
- The DbCreateRunner tool is at Tools\DbCreateRunner and also used for smoke tests.

Smoke tests
- Inventory flows: InventoryProduct, InventoryMovement, InventoryMeta exist and were exercised.
- Reports/Volumes/Wells flows also smoke-tested.

Notes
- All SQL Server / PostgreSQL-specific constructs were audited and ported to SQLite where used (INSERT...OUTPUT -> last_insert_rowid()).
- If you need a schema migration/versioning system, add a simple SchemaVersion table and apply incremental DDL.

Contact
- For questions, review Core/Services/DatabaseInitializer.cs and Core/Services/DatabaseService.cs.
