using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dotmim.Sync;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Sqlite;
using Dotmim.Sync.Web.Client;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Clear();

            // var clientDatabaseName = Path.GetRandomFileName().Replace(".", "").ToLowerInvariant() + ".db";
            var clientDatabaseName = "Client.db";
            DropSqliteDatabase(clientDatabaseName);
            var clientProvider = new SqliteSyncProvider(clientDatabaseName);

            Console.WriteLine($"Creating {clientDatabaseName} SQLite database");

            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip };
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };

            var proxyClientProvider = new WebClientOrchestrator("https://localhost:5001/api/Sync", client: client);

            var options = new SyncOptions
            {
                BatchDirectory = Path.Combine(SyncOptions.GetDefaultUserBatchDiretory(), "Tmp"),
                BatchSize = 2000,
            };

            var localProgress = new SynchronousProgress<ProgressArgs>(s =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{s.ProgressPercentage:p}:\t{s.Message}");
                Console.ResetColor();
            });

            var agent = new SyncAgent(clientProvider, proxyClientProvider, options);

            Console.WriteLine("Press a key to start (be sure web api is running ...)");
            Console.ReadLine();
            do
            {
                Console.WriteLine("Web sync start");
                try
                {

                    var s = await agent.SynchronizeAsync(localProgress);
                    Console.WriteLine(s);

                }
                catch (SyncException e)
                {
                    Console.WriteLine(e);
                }
                catch (Exception e)
                {
                    Console.WriteLine("UNKNOW EXCEPTION : " + e.Message);
                }


                Console.WriteLine("Sync Ended. Press a key to start again, or Escapte to end");
            } while (Console.ReadKey().Key != ConsoleKey.Escape);

            Console.WriteLine("End");
        }

        static void DropSqliteDatabase(string dbName)
        {
            string filePath = null;
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                filePath = GetSqliteFilePath(dbName);

                if (File.Exists(filePath))
                    File.Delete(filePath);

            }
            catch (Exception)
            {
                Console.WriteLine($"Sqlite file seems loked. ({filePath})");
            }

        }
        static string GetSqliteFilePath(string dbName)
        {
            var fi = new FileInfo(dbName);

            if (string.IsNullOrEmpty(fi.Extension))
                dbName = $"{dbName}.db";

            return Path.Combine(Directory.GetCurrentDirectory(), dbName);

        }        
    }
}
