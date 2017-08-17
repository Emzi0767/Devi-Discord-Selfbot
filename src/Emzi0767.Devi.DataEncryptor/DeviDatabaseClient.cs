using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emzi0767.Devi.Crypto;
using Emzi0767.Devi.DataEncryptor;
using Npgsql;
using NpgsqlTypes;

namespace Emzi0767.Devi
{
    internal class DeviDatabaseClient : IDisposable
    {
        private DeviDatabaseSettings Settings { get; }
        private string ConnectionString { get; }
        private NpgsqlConnection Connection { get; set; }
        private KeyManager KeyManager { get; }

        public DeviDatabaseClient(DeviDatabaseSettings settings, KeyManager key_manager)
        {
            this.Settings = settings;
            this.KeyManager = key_manager;

            var csb = new NpgsqlConnectionStringBuilder
            {
                Host = this.Settings.Hostname,
                Port = this.Settings.Port,
                Database = this.Settings.Database,
                Username = this.Settings.Username,
                Password = this.Settings.Password,
                Pooling = true,
                SslMode = SslMode.Prefer,
                TrustServerCertificate = true
            };
            this.ConnectionString = csb.ConnectionString;
        }

        public async Task EncryptMessageContentsAsync()
        {
            this.Connection = new NpgsqlConnection(this.ConnectionString);
            await this.Connection.OpenAsync();

            var lst = new List<(long, string[])>();
            var tbl = string.Concat(this.Settings.TablePrefix, "message_log");
            using (var cmd = this.Connection.CreateCommand())
            {
                cmd.CommandText = string.Concat("SELECT message_id, contents FROM ", tbl, ";");

                using (var rdr = await cmd.ExecuteReaderAsync())
                    while (await rdr.ReadAsync())
                        lst.Add(((long)rdr["message_id"], (string[])rdr["contents"]));
            }

            foreach (var (id, xarr) in lst)
            {
                for (var i = 0; i < xarr.Length; i++)
                {
                    this.KeyManager.Encrypt(xarr[i], out var encdata, "pgmain");
                    xarr[i] = encdata.ToBase64();
                }
            }

            using (var trn = this.Connection.BeginTransaction())
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    cmd.CommandText = string.Concat("UPDATE ", tbl, " SET contents = @contents WHERE message_id = @message_id;");

                    cmd.Parameters.Add("contents", NpgsqlDbType.Text | NpgsqlDbType.Array);
                    cmd.Parameters.Add("message_id", NpgsqlDbType.Bigint);
                    cmd.Prepare();

                    foreach (var (id, xarr) in lst)
                    {
                        cmd.Parameters["contents"].Value = xarr;
                        cmd.Parameters["message_id"].Value = id;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await trn.CommitAsync();
                }
            }
        }

        public async Task<string[]> GetMessageContentsAsync()
        {
            this.Connection = new NpgsqlConnection(this.ConnectionString);
            await this.Connection.OpenAsync();

            using (var cmd = this.Connection.CreateCommand())
            {
                var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                cmd.CommandText = string.Concat("SELECT contents FROM ", tbl, " WHERE array_length(contents, 1) > 1 ORDER BY id DESC LIMIT 1;");

                return (string[])await cmd.ExecuteScalarAsync();        
            }
        }

        public void Dispose()
        {
            if (this.Connection == null)
                return;

            this.Connection.Dispose();
            this.Connection = null;
        }
    }
}