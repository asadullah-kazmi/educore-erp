using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace SchoolERP.DatabaseSetup
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: SchoolERP.DatabaseSetup.exe <server> <schema.sql> <seed.sql>");
                return 2;
            }

            try
            {
                var connectionString = "Server=" + args[0] + ";Database=master;Integrated Security=True;TrustServerCertificate=True;";
                WaitForServer(connectionString);
                ExecuteScript(connectionString, args[1]);
                ExecuteScript(connectionString, args[2]);
                Console.WriteLine("SchoolERP database configured successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Database setup failed: " + ex.Message);
                return 1;
            }
        }

        private static void WaitForServer(string connectionString)
        {
            Exception lastError = null;
            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(2000);
                }
            }

            throw new InvalidOperationException("SQL Server did not become available within 60 seconds.", lastError);
        }

        private static void ExecuteScript(string connectionString, string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Database script was not found.", scriptPath);
            }

            var script = File.ReadAllText(scriptPath);
            var batches = Regex.Split(script, @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }

                    using (var command = new SqlCommand(batch, connection))
                    {
                        command.CommandTimeout = 180;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
