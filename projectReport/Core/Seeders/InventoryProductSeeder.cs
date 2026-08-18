using Microsoft.Data.Sqlite;
using ProjectReport.Services;

namespace ProjectReport.Core.Seeders
{
    public static class InventoryProductSeeder
    {
        public static void Seed(DatabaseService db)
        {
            // ============================================================
            // SURFACTANTES
            // ============================================================

            InsertProduct(db, "10001", "G-WASH - 55 gal Can", "Surfactante completamiento",
                "Líquido", "Caneca", 55, "gal", 1.1, "Surfactantes");

            InsertProduct(db, "10002", "G-ROP - 55 gal Can", "Mejorador de ROP",
                "Líquido", "Caneca", 55, "gal", 0.93, "Surfactantes");

            InsertProduct(db, "10003", "KLEENFLOC - 55 gal Can", "Floculante WBCO",
                "Líquido", "Caneca", 55, "gal", 1.08, "Surfactantes");

            InsertProduct(db, "10004", "G-SURF 395 -55 gal", "Surfactante no ionico",
                "Líquido", "Caneca", 55, "gal", 1.0, "Surfactantes");

            InsertProduct(db, "10005", "G-SURF 393- 55gal", "Surfactante no ionico",
                "Líquido", "Caneca", 55, "gal", 1.0, "Surfactantes");

            InsertProduct(db, "10006", "G-ROP PLUS", "Antiacrecion - Mejorador ROP",
                "Líquido", "Caneca", 55, "gal", 0.93, "Surfactantes");


            // ============================================================
            // DENSIFICANTES
            // ============================================================

            InsertProduct(db, "1001", "G-BAR - 100 lb Bag", "Barita, Baritina",
                "Sólido", "Sacos", 100, "lb", 4.1, "Densificantes");

            InsertProduct(db, "1002", "G-BAR - 1 Ton Big Bag", "Barita, Baritina",
                "Sólido", "Big Bag", 1, "Ton", 4.1, "Densificantes");

            InsertProduct(db, "1003", "HEMATITA 1 Ton Big bag", "HEMATITA",
                "Sólido", "Big Bag", 1, "Ton", 5.1, "Densificantes");

            InsertProduct(db, "1004", "Carbonato de Calcio M10-40 - 110 lb Bag", "Carbonato de Calcio",
                "Sólido", "Sacos", 110, "lb", 2.7, "Densificantes");

            InsertProduct(db, "1005", "Carbonato de Calcio M40-100 - 110 lb Bag", "Carbonato de Calcio",
                "Sólido", "Sacos", 110, "lb", 2.7, "Densificantes");

            InsertProduct(db, "1006", "Carbonato de Calcio M200 - 110 lb Bag", "Carbonato de Calcio",
                "Sólido", "Sacos", 110, "lb", 2.7, "Densificantes");

            InsertProduct(db, "1007", "Carbonato de Calcio M325 - 110 lb Bag", "Carbonato de Calcio",
                "Sólido", "Sacos", 110, "lb", 2.7, "Densificantes");

            InsertProduct(db, "1008", "Carbonato de Calcio M600 - 55 lb Bag", "Carbonato de Calcio",
                "Sólido", "Sacos", 55, "lb", 2.7, "Densificantes");

            InsertProduct(db, "1009", "Carbonato de Calcio M1200 - 55 lb Bag", "Carbonato de Calcio",
                "Sólido", "Sacos", 55, "lb", 2.7, "Densificantes");

            InsertProduct(db, "1010", "MICROMAX 1 Ton Big bag", "MICROMAX",
                "Sólido", "Big Bag", 1, "Ton", 4.7, "Densificantes");

            InsertProduct(db, "1011", "HEMATITA 1 Ton Big bag", "HEMATITA",
                "Sólido", "Big Bag", 2200, "lb", 5.1, "Densificantes");

            InsertProduct(db, "1014", "Barita 4,1 Big Bag 1,5 Ton", "Barita, Baritina",
                "Sólido", "Big Bag", 3300, "lb", 4.1, "Densificantes");

            InsertProduct(db, "1015", "Carbonato de Calcio M600 - 55 lb Bag", "Carbonato de Calcio",
                "Sólido", "Sacos", 55, "lb", 2.7, "Densificantes");


            // ============================================================
            // ESTABILIZADORES DE HUECOS
            // ============================================================

            InsertProduct(db, "11001", "G-STAB - 50 lb Bag", "Asfalto sulfatado",
                "Sólido", "Sacos", 50, "lb", 1.4, "Estabilizadores huecos");

            InsertProduct(db, "11002", "G-GRAPH (Fine) - 50 lb Bag", "Grafito",
                "Sólido", "Sacos", 50, "lb", 1.75, "Estabilizadores huecos");

            InsertProduct(db, "11003", "G-GRAPH (Medium) - 50 lb Bag", "Grafito",
                "Sólido", "Sacos", 50, "lb", 1.75, "Estabilizadores huecos");

            InsertProduct(db, "11004", "G-STAB - 55 lb Bag", "Asfalto sulfatado",
                "Sólido", "Sacos", 55, "lb", 1.4, "Estabilizadores huecos");


            // ============================================================
            // ANTIESPUMANTES
            // ============================================================

            InsertProduct(db, "12001", "G-DEFOAM G - 5 gal Can", "Antiespumante a base Glicol",
                "Líquido", "Caneca", 5, "gal", 0.98, "Antiespumantes");

            InsertProduct(db, "12002", "G-DEFOAM A - 5 gal Can", "Antiespumante a base Alcohol",
                "Líquido", "Caneca", 5, "gal", 0.98, "Antiespumantes");

            InsertProduct(db, "12003", "G-DEFOAM S - 5 gal Can", "Antiespumante Siliconado",
                "Líquido", "Caneca", 5, "gal", 0.98, "Antiespumantes");


            // ============================================================
            // ENCAPSULANTES
            // ============================================================

            InsertProduct(db, "13001", "WEL HIB 40 - 55 gal Can",
                "Floculante da bajo o medio peso molecular",
                "Líquido", "Caneca", 55, "gal", 1.1, "Encapsulantes");

            InsertProduct(db, "13002", "G-CAP S - 50 lb bag", "Floculante en polvo",
                "Sólido", "Sacos", 50, "lb", 1.08, "Encapsulantes");

            InsertProduct(db, "13003", "G-CAP FS-L - 5 gal Can",
                "Floculante para alto pesos molecular",
                "Líquido", "Caneca", 5, "gal", 1.01, "Encapsulantes");


            // ============================================================
            // SPOTTING FLUIDS
            // ============================================================

            InsertProduct(db, "14001", "Black Magic - 55 lb Bag", "Spotting fluids",
                "Sólido", "Sacos", 55, "lb", 1.04, "Spotting fluids");

            InsertProduct(db, "14002", "G-SPOT - 55 gal Can", "Spotting fluids",
                "Líquido", "Caneca", 55, "gal", 1.0, "Spotting fluids");

            InsertProduct(db, "14003", "G-BREAKER - 55 gal", "Removedor de Cake",
                "Líquido", "Caneca", 55, "gal", 1.2, "Spotting fluids");


            // ============================================================
            // ALCALINIZANTES
            // ============================================================

            InsertProduct(db, "15001", "Cal Hidratada - 55 lb Bag", "Cal Hidratada",
                "Sólido", "Sacos", 55, "lb", 2.3, "Alcalinizantes");

            InsertProduct(db, "15002", "Soda Caustica - 55 lb Bag", "Soda Caustica",
                "Sólido", "Sacos", 55, "lb", 2.13, "Alcalinizantes");

            InsertProduct(db, "15003", "Bicarbonato de Sodio - 55 lb", "Bloquear",
                "Sólido", "Sacos", 55, "lb", 1.0, "Alcalinizantes");

            InsertProduct(db, "15004", "Potasa Caustica- 55 lb Bag", "Hidroxido de potasio",
                "Sólido", "Sacos", 55, "lb", 2.04, "Alcalinizantes");

            InsertProduct(db, "15005", "G-MEA- 55 gal", "MONOETANOLAMINA",
                "Líquido", "Caneca", 55, "gal", 1.1, "Alcalinizantes");


            // ============================================================
            // INHIBIDORES (CORROSIÓN, H2S)
            // ============================================================

            InsertProduct(db, "16001", "G-OX - 55 gal Can", "Secuestrante de Oxigeno",
                "Líquido", "Caneca", 55, "Gal", 1.26, "Inhibidores (corrosión, h2s)");

            InsertProduct(db, "16002", "WEL-GARD - 55 gal Can", "Secuestrante de Oxigeno",
                "Líquido", "Caneca", 55, "Gal", 1.0, "Inhibidores (corrosión, h2s)");

            InsertProduct(db, "16003", "G-FILM - 55 gal Can",
                "Inhibidor de corrosion a base de amina organica",
                "Líquido", "Caneca", 55, "Gal", 1.0, "Inhibidores (corrosión, h2s)");

            InsertProduct(db, "16004", "G-COR - 55 gal Can",
                "Inhibidor de corrosion a base de amina",
                "Líquido", "Caneca", 55, "Gal", 1.0, "Inhibidores (corrosión, h2s)");

            InsertProduct(db, "16005", "G-SCAV -55 gal Can", "Secuestrante de H2S",
                "Líquido", "Caneca", 55, "Gal", 1.01, "Inhibidores (corrosión, h2s)");


            // ============================================================
            // SOLVENTES
            // ============================================================

            InsertProduct(db, "17001", "G-SOLV", "Solvente Mutual",
                "Líquido", "Caneca", 55, "Gal", 0.93, "Solventes");

            InsertProduct(db, "17002", "G-DISORG", "Solvente Organico",
                "Líquido", "Caneca", 55, "Gal", 0.9, "Solventes");

            InsertProduct(db, "17003", "VARSOL", "Solvente No. 4",
                "Líquido", "Caneca", 55, "Gal", 0.8, "Solventes");

            InsertProduct(db, "17004", "WEL INHSCALE", "Inhibidor de incrustaciones",
                "Líquido", "Caneca", 1, "Gal", 1.2, "Solventes");

            InsertProduct(db, "17005", "WEL REMKLEEM", "Removedor de incrustaciones",
                "Líquido", "Caneca", 1, "Gal", 1.1, "Solventes");

            InsertProduct(db, "17006", "G-INHORG", "Inhibidor de parafinas",
                "Líquido", "Caneca", 55, "Gal", 1.0, "Solventes");

            InsertProduct(db, "17007", "WEL BREAKSTAR", "Starch Breaker",
                "Líquido", "Caneca", 55, "Gal", 1.0, "Solventes");

            InsertProduct(db, "17008", "WEL BREAKER XHA", "Xhantan Breaker",
                "Líquido", "Caneca", 55, "Gal", 1.0, "Solventes");


            // ============================================================
            // APHRON SYSTEM
            // ============================================================

            InsertProduct(db, "18001", "Ultra Aphronizer", "Aphrons",
                "Líquido", "Caneca", 5, "Gal", 1.01, "Aphron system");

            InsertProduct(db, "18002", "Ultra FL", "Control filtrado Aphron",
                "Sólido", "Sacos", 50, "libras", 0.468, "Aphron system");

            InsertProduct(db, "18003", "Ultra Buff", "Alcalinizante Aphron",
                "Sólido", "Sacos", 50, "libras", 0.468, "Aphron system");


            // ============================================================
            // INHIBIDORES DE ARCILLAS
            // ============================================================

            InsertProduct(db, "2001", "G-HIB A - 55 gal Can",
                "Inhibidor de arcillas tipo Amina",
                "Líquido", "Caneca", 55, "gal", 1.08, "Inhibidores arcillas");

            InsertProduct(db, "2002", "G-HIB A - 265 gal Isotanque",
                "Inhibidor de arcillas tipo Amina",
                "Líquido", "Isotanque", 265, "gal", 1.08, "Inhibidores arcillas");

            InsertProduct(db, "2003", "G-HIB A+ 55 GAL",
                "Inhibidor de arcillas",
                "Líquido", "Caneca", 55, "gal", 1.1, "Inhibidores arcillas");


            // ============================================================
            // LCM
            // ============================================================

            InsertProduct(db, "3001", "KWIK SEAL (Medium) - 40 lb Bag", "LCM",
                "Sólido", "Sacos", 40, "lb", 0.497, "LCM");

            InsertProduct(db, "3002", "G-PLUG - (Medium) - 50 lb Bag", "Cascara de Nuez",
                "Sólido", "Sacos", 50, "lb", 1.2, "LCM");

            InsertProduct(db, "3003", "G-FIBER (Fine) - 25 lb Bag", "Fibras vegetal",
                "Sólido", "Sacos", 25, "lb", 0.497, "LCM");

            InsertProduct(db, "3004", "G.FIBER (Medium) - 40 lb Bag", "Fibras vegetal",
                "Sólido", "Sacos", 40, "lb", 0.497, "LCM");

            InsertProduct(db, "3005", "WEL SQUEEZE - 25 lb", "bloquear",
                "Sólido", "Sacos", 25, "lb", 0.5, "LCM");

            InsertProduct(db, "3006", "Carbolite 16/20 - 1Ton Big Bag", "Carbolite",
                "Sólido", "Big Bag", 1, "Ton", 2.71, "LCM");

            InsertProduct(db, "3007", "WEL PLEX - 44 lb", "bloquear",
                "Sólido", "Sacos", 44, "lb", 0.5, "LCM");

            InsertProduct(db, "3008", "G-NUT (Fine) - 50 lb Bag", "Cascara de Nuez",
                "Sólido", "Sacos", 50, "lb", 1.2, "LCM");

            InsertProduct(db, "3009", "RICE HULLS 25 lb bag", "Cascara de arroz",
                "Sólido", "Sacos", 25, "lb", 0.4, "LCM");

            InsertProduct(db, "3010", "G-THIX PLUG 55 lb Bag", "Sistema LCM",
                "Sólido", "Sacos", 55, "lb", 2.4, "LCM");

            InsertProduct(db, "3011", "ULTRA SEAL ASF", "Fibra Acidificable F",
                "Sólido", "Sacos", 55, "lb", 1.4, "LCM");

            InsertProduct(db, "3012", "ULTRA SEAL ASF PLUS", "Fibra Acidificable M",
                "Sólido", "Sacos", 55, "lb", 1.4, "LCM");


            // ============================================================
            // LUBRICANTES
            // ============================================================

            InsertProduct(db, "4001", "Drill Beads - 88 lb Can", "Lubricante Mecanico",
                "Sólido", "Caneca", 88, "gal", 1.3, "Lubricantes");

            InsertProduct(db, "4002", "G-LUBE PLUS. - 55 gal Tam", "Lubricante Mineral",
                "Líquido", "Caneca", 55, "gal", 0.88, "Lubricantes");

            InsertProduct(db, "4003", "G-LUBE ULTRA. - 55 gal Tam", "Lubricante Sintetico",
                "Líquido", "Caneca", 55, "gal", 0.88, "Lubricantes");

            InsertProduct(db, "4004", "G-LUBE. - 55 gal Tam", "Lubricante Vegetal",
                "Líquido", "Caneca", 55, "gal", 0.89, "Lubricantes");


            // ============================================================
            // COMODITIES
            // ============================================================

            InsertProduct(db, "50001", "Bicarbonato de Sodio - 55 lb Bag",
                "Bicarbonato de Sodio",
                "Sólido", "Sacos", 55, "lb", 1.0, "Comodities");

            InsertProduct(db, "50002", "OXIDO DE ZINC",
                "Encapsulante de H2S",
                "Sólido", "Sacos", 55, "lb", 0.7, "Comodities");


            // ============================================================
            // CONTROLADORES DE FILTRADO
            // ============================================================

            InsertProduct(db, "5001", "G-PAC LV - 50 lb Bag",
                "Celulosa polianionica de baja viscocidad",
                "Sólido", "Sacos", 55, "lb", 0.8, "Controladores de filtrado");

            InsertProduct(db, "5002", "G-PAC LV - 55 lb Bag",
                "Celulosa polianionica de baja viscocidad",
                "Sólido", "Sacos", 55, "lb", 0.8, "Controladores de filtrado");

            InsertProduct(db, "5003", "G-STARCH - 50 lb Bag",
                "Almidon modificado",
                "Sólido", "Sacos", 50, "lb", 1.47, "Controladores de filtrado");

            InsertProduct(db, "5004", "G-PAC HV - 50 lb Bag",
                "Celulosa polianionica de alta viscocidad",
                "Sólido", "Sacos", 55, "lb", 0.88, "Controladores de filtrado");

            InsertProduct(db, "5005", "G-STARCH - 55 lb Bag",
                "Almidon modificado",
                "Sólido", "Sacos", 50, "lb", 1.47, "Controladores de filtrado");

            InsertProduct(db, "5006", "G-STAR HT- 50 lb Bag",
                "Almidon modificado alta temperatura",
                "Sólido", "Sacos", 50, "lb", 1.65, "Controladores de filtrado");

            InsertProduct(db, "5007", "G-SPA - 50 lb Bag",
                "Poliacrilato de sodio",
                "Sólido", "Sacos", 50, "lb", 0.8, "Controladores de filtrado");

            InsertProduct(db, "5010", "DRISTEMP - 50 lb Bag",
                "Polimero controlador de filtrado alta temperatura",
                "Sólido", "Sacos", 50, "lb", 1.44, "Controladores de filtrado");

            InsertProduct(db, "5011", "G-TEX - 55 gal Tam",
                "Latex",
                "Líquido", "Caneca", 55, "gal", 1.1, "Controladores de filtrado");

            InsertProduct(db, "5012", "G-PAC L - 41 lb Bag",
                "Celulosa polianionica de baja viscocidad",
                "Sólido", "Sacos", 41, "lb", 0.8, "Controladores de filtrado");


            // ============================================================
            // VISCOSIFICANTES
            // ============================================================

            InsertProduct(db, "6001", "G-EX - 2 lb Bag",
                "Extendedor de Bentonita",
                "Sólido", "Sacos", 2, "lb", 0.79, "Viscosificantes");

            InsertProduct(db, "6002", "G-GEL - 100 lb Bag",
                "Bentonita",
                "Sólido", "Sacos", 100, "lb", 0.8, "Viscosificantes");

            InsertProduct(db, "6003", "G-GEL - 55 lb Bag",
                "Bentonita",
                "Sólido", "Sacos", 55, "lb", 0.8, "Viscosificantes");

            InsertProduct(db, "6004", "G-ZAN - 55 lb Bag",
                "Goma Xantica",
                "Sólido", "Sacos", 55, "lb", 1.5, "Viscosificantes");

            InsertProduct(db, "6005", "FLOW ZAN - 25 lb Bag",
                "Biopolimero de alta pureza",
                "Sólido", "Sacos", 25, "lb", 1.6, "Viscosificantes");

            InsertProduct(db, "6006", "BENTONITA WYOMING- Big Bag 1 Ton",
                "Bentonita",
                "Sólido", "Big Bag", 2200, "lb", 0.8, "Viscosificantes");


            // ============================================================
            // SALES
            // ============================================================

            InsertProduct(db, "7001", "Formiato de Potasio - 1 Ton Isotanque",
                "Formiato de Potasio",
                "Líquido", "Isotanque", 264, "gal", 1.91, "Sales");

            InsertProduct(db, "7002", "Formiato de Potasio - 55 lb Bag",
                "Formiato de Potasio",
                "Sólido", "Sacos", 55, "lb", 1.91, "Sales");

            InsertProduct(db, "7003", "Formiato de Sodio - 55 lb Bag",
                "Formiato de Sodio",
                "Sólido", "Sacos", 55, "lb", 1.3, "Sales");

            InsertProduct(db, "7004", "Cloruro de Potasio - 55 lb Bag",
                "Cloruro de Potasio",
                "Sólido", "Sacos", 55, "lb", 1.987, "Sales");

            InsertProduct(db, "7005", "Cloruro de Potasio - 110 lb Bag",
                "Cloruro de Potasio",
                "Sólido", "Sacos", 110, "Lb", 1.987, "Sales");

            InsertProduct(db, "7006", "Cloruro de Sodio - 55 lb Bag",
                "Cloruro de Sodio",
                "Sólido", "Sacos", 55, "Lb", 2.16, "Sales");

            InsertProduct(db, "7007", "Cloruro de Sodio - 110 lb Bag",
                "Cloruro de Sodio",
                "Sólido", "Sacos", 110, "Lb", 2.16, "Sales");

            InsertProduct(db, "7008", "Cloruro de amonio -55 lb Bag",
                "Cloruroi de amonio",
                "Sólido", "Sacos", 55, "Lb", 1.53, "Sales");


            // ============================================================
            // DISPERSANTES O DEFLOCULANTES
            // ============================================================

            InsertProduct(db, "8001", "DESCO - 25 lb Bag",
                "Dispersante libre de cromo",
                "Sólido", "Sacos", 25, "lb", 1.5, "Dispersantes o defloculantes");

            InsertProduct(db, "8002", "G-THIN - 5 gl Can",
                "Copolimero de acrilato",
                "Líquido", "Caneca", 5, "gal", 1.24, "Dispersantes o defloculantes");

            InsertProduct(db, "8003", "G-LIG",
                "Lignito",
                "Sólido", "Sacos", 50, "lb", 1.1, "Dispersantes o defloculantes");


            // ============================================================
            // BACTERICIDAS
            // ============================================================

            InsertProduct(db, "9001", "G-CIDE - 5 gal Can",
                "Biocida",
                "Líquido", "Caneca", 5, "gal", 1.06, "Bactericidas");

            InsertProduct(db, "9002", "Biocide TH- 5 gal Can",
                "Biocida THPS",
                "Líquido", "Caneca", 5, "gal", 1.06, "Bactericidas");
        }


        // ================================================================
        // MÉTODO PARA INSERTAR PRODUCTO
        // ================================================================

        private static void InsertProduct(
            DatabaseService db,
            string code,
            string name,
            string description,
            string physicalState,
            string presentation,
            double packageQuantity,
            string packageUnit,
            double sg,
            string category)
        {
            db.ExecuteNonQuery(@"
                INSERT OR IGNORE INTO inventory_product
                (
                    code,
                    name,
                    description,
                    physical_state,
                    presentation,
                    package_quantity,
                    package_unit,
                    sg,
                    category,
                    status,
                    is_selected_for_report
                )
                VALUES
                (
                    @code,
                    @name,
                    @description,
                    @physical_state,
                    @presentation,
                    @package_quantity,
                    @package_unit,
                    @sg,
                    @category,
                    1,
                    0
                );
            ",
                new SqliteParameter("@code", code),
                new SqliteParameter("@name", name),
                new SqliteParameter("@description", description),
                new SqliteParameter("@physical_state", physicalState),
                new SqliteParameter("@presentation", presentation),
                new SqliteParameter("@package_quantity", packageQuantity),
                new SqliteParameter("@package_unit", packageUnit),
                new SqliteParameter("@sg", sg),
                new SqliteParameter("@category", category)
            );
        }
    }
}