using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

class Program
{
    static int Main()
    {
        try
        {
            var initPath = Path.GetFullPath(Path.Combine("..","projectReport","Core","Services","DatabaseInitializer.cs"));
            if (!File.Exists(initPath))
            {
                Console.Error.WriteLine($"Initializer file not found: {initPath}");
                return 2;
            }
            var content = File.ReadAllText(initPath);
            var rx = new Regex("ExecuteNonQuery\\(@\"(?<sql>[\\s\\S]*?)\"\)\\;", RegexOptions.Singleline);
            var matches = rx.Matches(content);
            if (matches.Count == 0)
            {
                Console.Error.WriteLine("No SQL blocks found in DatabaseInitializer.cs");
                return 3;
            }

            var dbPath = Path.GetFullPath(Path.Combine("..","projectReport","projectReport.db"));
            var connString = $"Data Source={dbPath};Cache=Shared";
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? ".");

            using var conn = new SqliteConnection(connString);
            conn.Open();

            foreach (Match m in matches)
            {
                var sql = m.Groups["sql"].Value;
                sql = sql.Replace("\r\n", "\n");
                // Trim surrounding whitespace
                sql = sql.Trim();
                if (string.IsNullOrWhiteSpace(sql)) continue;
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Executed SQL block (length: {0})", sql.Length);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error executing SQL block: " + ex.Message);
                    Console.Error.WriteLine(sql.Substring(0, Math.Min(sql.Length, 4000)));
                }
            }

            Console.WriteLine("Listing tables in DB: {0}", dbPath);
            using var listCmd = conn.CreateCommand();
            listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
            using var reader = listCmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(reader.GetString(0));
            }

            conn.Close();
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.ToString());
            return 1;
        }
    }
}
