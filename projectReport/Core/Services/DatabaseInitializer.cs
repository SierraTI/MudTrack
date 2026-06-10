using System;
using Microsoft.Data.Sqlite;

namespace ProjectReport.Services
{
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Ensures minimal DB schema required at app startup. Add additional repository schemas here.
        /// </summary>
        public static void Initialize()
        {
            using var db = new DatabaseService();

            // Inventory tables (kept in sync with SqliteInventoryRepository)
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS InventoryProduct (
                Code TEXT PRIMARY KEY,
                Name TEXT,
                Description TEXT,
                OtherNames TEXT,
                PhysicalState TEXT,
                Presentation TEXT,
                Quantity REAL,
                Category TEXT,
                Unit TEXT,
                SG REAL,
                QtyPackage INTEGER,
                StockQty REAL,
                CurrentUnitCost REAL,
                Status INTEGER,
                IsSelectedForReport INTEGER
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS InventoryMovement (
                MovementId TEXT PRIMARY KEY,
                TicketId TEXT,
                Date TEXT,
                ProductCode TEXT,
                ProductName TEXT,
                Type INTEGER,
                Quantity REAL,
                UnitPrice REAL,
                OriginOrUse TEXT,
                ShipmentReference TEXT,
                SupplierName TEXT,
                ShipmentMethod TEXT,
                UserName TEXT,
                Observations TEXT,
                IsAddedToFluid INTEGER,
                StockBefore REAL,
                StockAfter REAL,
                Requisition TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS InventoryMeta (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );");

            // Ensure LastRequisition meta exists
            var dt = db.ExecuteQuery("SELECT Value FROM InventoryMeta WHERE Key = @k", new SqliteParameter("@k", "LastRequisition"));
            if (dt.Rows.Count == 0)
            {
                db.ExecuteNonQuery("INSERT INTO InventoryMeta (Key, Value) VALUES (@k, @v)", new SqliteParameter("@k", "LastRequisition"), new SqliteParameter("@v", "0"));
            }

            // Wells
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Well (
                idW INTEGER PRIMARY KEY AUTOINCREMENT,
                wellName TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS WellInfo (
                idW INTEGER,
                Operator TEXT,
                FluidType TEXT,
                Spud_date TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS WellDesign (
                idW INTEGER,
                Trajectory TEXT,
                Welltype TEXT,
                RigName TEXT,
                Rigtype TEXT,
                Contractor TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS WellLocation (
                idW INTEGER,
                Location TEXT,
                Country TEXT,
                Basin TEXT,
                State TEXT,
                Block TEXT,
                Latitud REAL,
                Longitud REAL
            );");

            // Reports
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Report (
                idRep INTEGER PRIMARY KEY AUTOINCREMENT,
                idW INTEGER,
                Interval INTEGER,
                Interval_size TEXT,
                ReportDate TEXT,
                Report_MD REAL,
                Report_TVD REAL
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS OperationalDetail (
                idRep INTEGER PRIMARY KEY,
                Well_Section TEXT,
                Max_BHT REAL,
                Present_Activity TEXT,
                Fluid TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Personnel (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                idRep INTEGER,
                Role TEXT,
                PersonName TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS ReportPump (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                idRep INTEGER,
                PumpNo INTEGER,
                PumpName TEXT,
                LinerSize REAL,
                StrokeLength REAL,
                Efficiency REAL,
                SPM REAL,
                Pressure REAL
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS ReportScreen (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                idRep INTEGER,
                ShakerName TEXT,
                ScreenType TEXT,
                Quantity INTEGER
            );");

            // Users
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Users (
                user_id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT UNIQUE NOT NULL,
                email TEXT UNIQUE NOT NULL,
                first_name TEXT,
                last_name TEXT,
                role TEXT DEFAULT 'User',
                is_active INTEGER DEFAULT 1,
                created_at TEXT,
                last_login TEXT
            );");

            // Projects
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Projects (
                project_id INTEGER PRIMARY KEY AUTOINCREMENT,
                project_name TEXT NOT NULL,
                well_name TEXT,
                last_modified TEXT,
                active_well_id INTEGER,
                created_by TEXT,
                created_at TEXT,
                modified_by TEXT,
                modified_at TEXT
            );");

            // Rig profiles and equipment
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS RigProfiles (
                rig_profile_id INTEGER PRIMARY KEY AUTOINCREMENT,
                well_id INTEGER,
                rig_name TEXT NOT NULL,
                contractor TEXT,
                rig_type TEXT,
                rkb_elevation REAL,
                casing_head_elevation REAL,
                created_at TEXT,
                modified_at TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS RigSurfaceEquipment (
                equipment_id INTEGER PRIMARY KEY AUTOINCREMENT,
                rig_profile_id INTEGER,
                sequence_no INTEGER,
                component_name TEXT,
                internal_diameter REAL,
                length REAL,
                description TEXT,
                friction_coefficient REAL,
                created_at TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS RigPumps (
                pump_id INTEGER PRIMARY KEY AUTOINCREMENT,
                rig_profile_id INTEGER,
                pump_name TEXT,
                liner_size REAL,
                stroke_length REAL,
                efficiency REAL,
                created_at TEXT
            );");

            // Wellbore components and drill string
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS WellboreGeometry (
                rowId INTEGER PRIMARY KEY AUTOINCREMENT,
                idRep INTEGER,
                Component TEXT,
                Description TEXT,
                TopMD REAL,
                BottomMD REAL,
                OD REAL,
                ID REAL,
                Washout REAL
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS DrillString (
                idDS INTEGER PRIMARY KEY AUTOINCREMENT,
                idW INTEGER,
                idRep INTEGER,
                ComponentType TEXT,
                Description TEXT,
                OD REAL,
                ID REAL,
                Weight REAL,
                Length REAL,
                CumulativeLength REAL,
                Displacement REAL,
                Capacity REAL
            );");

            // Surveys and thermal gradients
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS WellSurvey (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                idW INTEGER,
                MD REAL,
                Inclination REAL,
                Azimuth REAL,
                TVD REAL,
                NS_Coord REAL,
                EW_Coord REAL,
                Dogleg REAL
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS ThermalGradient (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                idW INTEGER,
                MD REAL,
                Temperature REAL
            );");

            // Well tests
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS WellTest (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                idW INTEGER,
                Section TEXT,
                TestType TEXT,
                MD REAL,
                TVD REAL,
                TestValue REAL,
                TestPressurePsi REAL
            );");

            // Catalog, products, inventory items, tickets
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Products (
                product_id INTEGER PRIMARY KEY AUTOINCREMENT,
                product_code TEXT,
                product_name TEXT,
                status TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS InventoryItems (
                item_id INTEGER PRIMARY KEY AUTOINCREMENT,
                product_code TEXT,
                product_name TEXT,
                quantity REAL,
                unit TEXT,
                location TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Tickets (
                ticket_id INTEGER PRIMARY KEY AUTOINCREMENT,
                ticket_date TEXT,
                ticket_type TEXT,
                status TEXT,
                user_name TEXT,
                supplier_name TEXT
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS TicketLines (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                ticket_id INTEGER,
                product_code TEXT,
                product_name TEXT,
                quantity REAL,
                unit TEXT,
                unit_price REAL
            );");

        }
    }
}
