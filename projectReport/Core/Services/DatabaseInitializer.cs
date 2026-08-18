using Microsoft.Data.Sqlite;
using ProjectReport.Core.Seeders;
using System;

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


            //PRODUCTOS EN INVENTARIO
            db.ExecuteNonQuery(@"
    CREATE TABLE IF NOT EXISTS inventory_product (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        code TEXT NOT NULL UNIQUE,
        name TEXT NOT NULL,
        description TEXT,
        physical_state TEXT NOT NULL,
        presentation TEXT NOT NULL,
        package_quantity REAL NOT NULL,
        package_unit TEXT NOT NULL,
        sg REAL,
        category TEXT NOT NULL,
        status INTEGER NOT NULL DEFAULT 1,
        is_selected_for_report INTEGER NOT NULL DEFAULT 0
    );
");

            InventoryProductSeeder.Seed(db);

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

            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS ReportFluids (
    id INTEGER PRIMARY KEY AUTOINCREMENT,

    idRep INTEGER NOT NULL,
    idWellFluid INTEGER NOT NULL,

    -- snapshot histórico del fluido
    FluidName TEXT NOT NULL,
    FluidType TEXT NOT NULL,

    FOREIGN KEY (idRep)
        REFERENCES Report(idRep),

    FOREIGN KEY (idWellFluid)
        REFERENCES WellFluids(id)
);
");

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

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS RigSolidsControl (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                rig_profile_id INTEGER,
                style TEXT,
                manufacturer TEXT,
                model TEXT,
                number_of_screens INTEGER,
                nominal_rpm INTEGER,
                cap_flow_gpm REAL,
                desilter_cones INTEGER,
                desilter_cone_size REAL,
                desander_cones INTEGER,
                desander_cone_size REAL
            );");

            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS RigPits (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                rig_profile_id INTEGER,
                pit_name TEXT,
                shape TEXT,
                dimensions TEXT,
                max_capacity REAL,
                is_active INTEGER
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

            // Fluid catalog (master list of fluid names)
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS FluidCatalog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    FluidName TEXT NOT NULL UNIQUE,
    FluidType TEXT NOT NULL
);");


            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('WEL-GEL','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('G-GEL','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('G-GEL MAX','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('WEL-DRIL RDF','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('WEL-DRIL','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('KCL BRINE','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('Na FORMATE BRINE','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('K FORMATE BRINE','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('NaCl BRINE','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('CaCl2 BRINE','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('G-DRILL REL','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('G-DRILL RDF','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('AGUA','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('AGUA INHIBIDA','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('APHRON ULTRASEAL® ICS','WBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('DIESEL','OBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('SBM','SBM');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('BRINE','BRINE');");
            db.ExecuteNonQuery("INSERT OR IGNORE INTO FluidCatalog (FluidName, FluidType) VALUES ('OTHER','OTHER');");

            // WellFluids: many-to-many / one-to-many mapping between wells and fluid names
            db.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS WellFluids (
    id INTEGER PRIMARY KEY AUTOINCREMENT,

    idW INTEGER NOT NULL,
    FluidCatalogId INTEGER NOT NULL,

    FOREIGN KEY (idW)
        REFERENCES Well(idW),

    FOREIGN KEY (FluidCatalogId)
        REFERENCES FluidCatalog(id)
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




            //----------------------------
            //Tablas Volumenes
            //---------------------------------

            //Volumen Balance
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS volume_balance (
    volume_balance_id INTEGER PRIMARY KEY AUTOINCREMENT,
    well_id INTEGER NOT NULL,
    report_date TEXT NOT NULL,
    shift TEXT NOT NULL,
    status TEXT NOT NULL,
    engineer TEXT,
    remarks TEXT,
    created_by TEXT NOT NULL,
    created_date DATETIME NOT NULL,
    modified_by TEXT,
    modified_date DATETIME,
    FOREIGN KEY (well_id) REFERENCES Well(idW)
);");

            //Volumen Balance Evento
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS volume_balance_event (
    volume_balance_event_id INTEGER PRIMARY KEY AUTOINCREMENT,

    volume_balance_id INTEGER NOT NULL,

    event_no INTEGER NOT NULL,

    event_date_time DATETIME NOT NULL,

    activity TEXT NOT NULL,

    current_depth REAL,

    description TEXT,

    remarks TEXT,

    created_by TEXT NOT NULL,

    created_date DATETIME NOT NULL,

    modified_by TEXT,

    modified_date DATETIME,

    FOREIGN KEY (volume_balance_id)
        REFERENCES volume_balance(volume_balance_id),

    UNIQUE (
        volume_balance_id,
        event_no
    )
);");

            //Sistemas de pits
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS pit_system_options (
    pit_system_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);");
            PitSystemSeeder.Seed(db);


            // Configuración de fluidos por evento

            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS event_fluid_system (
    event_fluid_system_id INTEGER PRIMARY KEY AUTOINCREMENT,

    volume_balance_event_id INTEGER NOT NULL,

    pit_name_id INTEGER NOT NULL,

    pit_system_id INTEGER NOT NULL,

    fluid_type_id INTEGER,

    fluid_sub_type TEXT,

    FOREIGN KEY (volume_balance_event_id)
        REFERENCES volume_balance_event(volume_balance_event_id),

    FOREIGN KEY (pit_name_id)
        REFERENCES RigPits(id),

    FOREIGN KEY (pit_system_id)
        REFERENCES pit_system_options(pit_system_id),

    FOREIGN KEY (fluid_type_id)
        REFERENCES ReportFluids(id),

    UNIQUE (
        volume_balance_event_id,
        pit_name_id
    )
);");
            //Hay que normalizar la tabla de reportsfluids


            // Volumen Sistema
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS vol_system (
    vol_system_id INTEGER PRIMARY KEY AUTOINCREMENT,

    event_fluid_system_id INTEGER NOT NULL,

    previous_volume REAL,

    current_volume REAL,

    density REAL,

    remarks TEXT,

    FOREIGN KEY (event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id),

    UNIQUE (
        event_fluid_system_id
    )
);");

            // Adiciones
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS addition (
    addition_id INTEGER PRIMARY KEY AUTOINCREMENT,
    volume_balance_event_id INTEGER NOT NULL,
    remarks TEXT,

    FOREIGN KEY (volume_balance_event_id)
        REFERENCES volume_balance_event(volume_balance_event_id)
);");


            // Adición Volumen Líquido
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS additions_liquid_volume (
    additions_liquid_id INTEGER PRIMARY KEY AUTOINCREMENT,
    addition_id INTEGER NOT NULL,
    event_fluid_system_id INTEGER NOT NULL,
    water REAL,
    dewatering_water REAL,
    osmosis_water REAL,
    oil_based REAL,
    iflux REAL,

    FOREIGN KEY (addition_id)
        REFERENCES addition(addition_id),

    FOREIGN KEY (event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id)
);");

            // Adición Volumen Fluido
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS additions_fluid_volume (
    additions_fluid_id INTEGER PRIMARY KEY AUTOINCREMENT,
    addition_id INTEGER NOT NULL,
    event_fluid_system_id INTEGER NOT NULL,
    fluid_name TEXT NOT NULL,
    volume REAL,
    concentration REAL,

    FOREIGN KEY (addition_id)
        REFERENCES addition(addition_id),

    FOREIGN KEY (event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id)
);");

            // Adición Volumen Quimica
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS additions_chemical_volume (
    additions_chemical_id INTEGER PRIMARY KEY AUTOINCREMENT,
    addition_id INTEGER NOT NULL,
    event_fluid_system_id INTEGER NOT NULL,
    chemical_id INTEGER NOT NULL,
    volume REAL,
    used_quantity REAL,

    FOREIGN KEY (addition_id)
        REFERENCES addition(addition_id),

    FOREIGN KEY (event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id),

    FOREIGN KEY (chemical_id)
        REFERENCES InventoryProduct(Code)

);");
            //Toca revisar la tabla de inventarios para ver como se esta manejando

            // Transferencias
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS transfers (
    transfer_id INTEGER PRIMARY KEY AUTOINCREMENT,
    from_event_fluid_system_id INTEGER NOT NULL,
    to_event_fluid_system_id INTEGER NOT NULL,
    volume REAL,
    remarks TEXT,

    CHECK (from_event_fluid_system_id <> to_event_fluid_system_id),

    FOREIGN KEY (from_event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id),

    FOREIGN KEY (to_event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id)
);");

            // LossesType
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS losses_type
(
    losses_type_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    is_active INTEGER NOT NULL DEFAULT 1
);
");

            db.ExecuteNonQuery(@"
INSERT OR IGNORE INTO losses_type (losses_type_id, name)
VALUES
    (1, 'SCE'),
    (2, 'MISCELANEOUS'),
    (3, 'DOWN HOLE');
");

            // LossesSubType
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS losses_subtype
(
    losses_subtype_id INTEGER PRIMARY KEY AUTOINCREMENT,
    losses_type_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY(losses_type_id) REFERENCES losses_type(losses_type_id)
);
");

            db.ExecuteNonQuery(@"
INSERT OR IGNORE INTO losses_subtype (losses_subtype_id, losses_type_id, name)
VALUES
    -- SCE (Id = 1)
    (1, 1, 'SHAKERS'),
    (2, 1, 'SHAKERS LOST OF CUTTINGS'),
    (3, 1, 'MUD CLEANER'),
    (4, 1, 'CENTRIFUGES'),
    (5, 1, 'OTHER SCE'),

    -- MISCELANEOUS (Id = 2)
    (6, 2, 'EVAPORATION'),
    (7, 2, 'TRIPS'),
    (8, 2, 'OTHERS IF'),
    (9, 2, 'DISPLACEMENT'),
    (10, 2, 'CONTAMINED'),
    (11, 2, 'LEFT BEHIND CSG'),
    (12, 2, 'RESIDUAL TANK'),

    -- DOWN HOLE (Id = 3)
    (13, 3, 'FILTRATION'),
    (14, 3, 'LOST IN HOLE');
");

            // Perdidas Losses
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS losses
(
    losses_id INTEGER PRIMARY KEY AUTOINCREMENT,
    event_fluid_system_id INTEGER NOT NULL,
    losses_subtype_id INTEGER NOT NULL,
    volume REAL NOT NULL,
    remarks TEXT,

    FOREIGN KEY (event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id),

    FOREIGN KEY (losses_subtype_id)
        REFERENCES losses_subtype(losses_subtype_id)
);");

            // Concentraciones
            db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS concentration
(
    concentration_id INTEGER PRIMARY KEY AUTOINCREMENT,
    event_fluid_system_id INTEGER NOT NULL,
    chemical_id INTEGER NOT NULL,
    concentration REAL,

    FOREIGN KEY (event_fluid_system_id)
        REFERENCES event_fluid_system(event_fluid_system_id),

    FOREIGN KEY (chemical_id)
        REFERENCES InventoryProduct(Code)
);");
            //Toca revisar bien como se esta manejando la quimica

        }
    }
}
