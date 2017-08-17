using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emzi0767.Devi.Crypto;
using Newtonsoft.Json;

namespace Emzi0767.Devi.DataEncryptor
{
    internal static class Program
    {
        private static KeyManager KeyManager { get; set; }

        internal static void Main(string[] args)
        {
            Console.WriteLine("Loading keystore");
            KeyManager = new KeyManager("keystore.json");
            if (!KeyManager.HasKey("pgmain"))
                KeyManager.AddKey("pgmain", 256);
            Console.WriteLine("Keystore loaded");

            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Loading database config");
            var json = "{}";
            using (var fs = File.OpenRead("devidb.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();
            var cfg = JsonConvert.DeserializeObject<DeviDatabaseSettings>(json);
            Console.WriteLine("Database config loaded");

            Console.WriteLine("Beginning encryption, this will take a while...");
            using (var db = new DeviDatabaseClient(cfg, KeyManager))
                await db.EncryptMessageContentsAsync();
            Console.WriteLine("Encryption completed");
            
            Console.WriteLine("Reading an entry to confirm decryptability");
            Console.WriteLine();
            using (var db = new DeviDatabaseClient(cfg, KeyManager))
            {
                var cnts = await db.GetMessageContentsAsync();
                foreach (var xc in cnts)
                {
                    KeyManager.Decrypt(xc.FromBase64(), out var xcnts, "pgmain");
                    Console.WriteLine(xcnts);
                }
            }
        }

        public static string ToBase64(this byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static byte[] FromBase64(this string data)
        {
            return Convert.FromBase64String(data);
        }
    }
}
