using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Emzi0767.Devi.Crypto;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace Emzi0767.Devi.Services
{
    public class DeviDatabaseClient : IDisposable
    {
        private DeviDatabaseSettings Settings { get; }
        private string ConnectionString { get; }
        private List<ulong> IgnoredInternal { get; }
        public IReadOnlyList<ulong> Ignored { get; }
        private SemaphoreSlim Semaphore { get; }
        private NpgsqlConnection Connection { get; set; }
        private KeyManager KeyManager { get; }

        private PropertyInfo MessageDiscordProperty { get; }
        private PropertyInfo ChannelDiscordProperty { get; }

        public DeviDatabaseClient(DeviDatabaseSettings settings, KeyManager key_manager)
        {
            this.Settings = settings;
            this.IgnoredInternal = new List<ulong>();
            this.Ignored = new ReadOnlyCollection<ulong>(this.IgnoredInternal);
            this.Semaphore = new SemaphoreSlim(1, 1);
            this.KeyManager = key_manager;

            if (this.Settings.Enabled)
            {
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

            // this is really half-assed, but I don't care
            this.MessageDiscordProperty = typeof(DiscordMessage).GetTypeInfo()
                .GetDeclaredProperty("Discord");
            this.ChannelDiscordProperty = typeof(DiscordChannel).GetTypeInfo()
                .GetDeclaredProperty("Discord");
        }

        public async Task LogMessageCreateAsync(DiscordMessage msg)
        {
            if (!this.Settings.Enabled)
                return;

            await this.Semaphore.WaitAsync();

            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                    var contents = msg.Content ?? "";
                    this.KeyManager.Encrypt(contents, out var enconts, "pgmain");

                    cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(message_id, author_id, channel_id, created, edits, contents, embeds, attachment_urls, deleted, edited) VALUES(@message_id, @author_id, @channel_id, @created, @edits, @contents, @embeds, @attachment_urls, @deleted, @edited);");
                    cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                    cmd.Parameters.AddWithValue("author_id", NpgsqlDbType.Bigint, (long)msg.Author.Id);
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                    cmd.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTZ, msg.CreationTimestamp);
                    cmd.Parameters.AddWithValue("edits", NpgsqlDbType.TimestampTZ | NpgsqlDbType.Array, new DateTimeOffset[] { });
                    cmd.Parameters.AddWithValue("contents", NpgsqlDbType.Text | NpgsqlDbType.Array, new[] { enconts.ToBase64() });
                    cmd.Parameters.AddWithValue("embeds", NpgsqlDbType.Jsonb | NpgsqlDbType.Array, new[] { JsonConvert.SerializeObject(msg.Embeds) });
                    cmd.Parameters.AddWithValue("attachment_urls", NpgsqlDbType.Text | NpgsqlDbType.Array, msg.Attachments.Select(xa => xa.Url).ToArray());
                    cmd.Parameters.AddWithValue("deleted", NpgsqlDbType.Boolean, false);
                    cmd.Parameters.AddWithValue("edited", NpgsqlDbType.Boolean, false);
                    cmd.Prepare();

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                if (this.MessageDiscordProperty != null && msg != null)
                {
                    var discord = this.MessageDiscordProperty.GetValue(msg) as DiscordClient;

                    if (discord != null)
                        discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while logging message creation: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
                }
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        public async Task LogMessageDeleteAsync(DiscordMessage msg)
        {
            if (!this.Settings.Enabled)
                return;

            await this.Semaphore.WaitAsync();

            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                    cmd.CommandText = string.Concat("UPDATE ", tbl, " SET deleted=@deleted WHERE message_id=@message_id AND channel_id=@channel_id;");
                    cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                    cmd.Parameters.AddWithValue("deleted", NpgsqlDbType.Boolean, true);
                    cmd.Prepare();

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                var discord = this.MessageDiscordProperty.GetValue(msg) as DiscordClient;

                if (discord != null)
                    discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while logging message deletion: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        public async Task LogMessageEditAsync(DiscordMessage msg)
        {
            if (!this.Settings.Enabled)
                return;

            await this.Semaphore.WaitAsync();

            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                    var contents = msg.Content ?? "";
                    this.KeyManager.Encrypt(contents, out var enconts, "pgmain");

                    cmd.CommandText = string.Concat("UPDATE ", tbl, " SET edited=@edited, contents=array_append(contents, @contents), embeds=array_append(embeds, @embeds), edits=array_append(edits, @edit) WHERE message_id=@message_id AND channel_id=@channel_id");
                    cmd.Parameters.AddWithValue("edited", NpgsqlDbType.Boolean, true);
                    cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                    cmd.Parameters.AddWithValue("contents", NpgsqlDbType.Text, enconts.ToBase64());
                    cmd.Parameters.AddWithValue("embeds", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(msg.Embeds));
                    cmd.Parameters.AddWithValue("edit", NpgsqlDbType.TimestampTZ, msg.IsEdited ? msg.EditedTimestamp : DateTimeOffset.Now);
                    cmd.Prepare();

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                var discord = this.MessageDiscordProperty.GetValue(msg) as DiscordClient;

                if (discord != null)
                    discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while logging message edit: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        public async Task<IEnumerable<DateTime>> GetEditsAsync(DiscordMessage msg)
        {
            if (!this.Settings.Enabled)
                return null;

            await this.Semaphore.WaitAsync();

            var edits = new List<DateTime>();
            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                    cmd.CommandText = string.Concat("SELECT edits, created FROM ", tbl, " WHERE message_id=@message_id AND channel_id=@channel_id AND edited IS TRUE LIMIT 1;");
                    cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                    cmd.Prepare();

                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            edits.AddRange((DateTime[])rdr["edits"]);
                            edits.Insert(0, (DateTime)rdr["created"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var discord = this.MessageDiscordProperty.GetValue(msg) as DiscordClient;

                if (discord != null)
                    discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while retrieving message edits: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            }
            finally
            {
                this.Semaphore.Release();
            }

            if (edits.Any())
                return edits;
            return null;
        }

        public async Task<string> GetEditAsync(DiscordMessage msg, DateTimeOffset which)
        {
            if (!this.Settings.Enabled)
                return null;

            await this.Semaphore.WaitAsync();

            object res = null;
            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                    if (msg.CreationTimestamp.ToLocalTime() == which)
                        cmd.CommandText = string.Concat("SELECT contents[1] FROM ", tbl, " WHERE message_id=@message_id AND channel_id=@channel_id AND edited IS TRUE AND created=@which LIMIT 1;");
                    else
                        cmd.CommandText = string.Concat("SELECT contents[array_position(edits, @which) + 1] FROM ", tbl, " WHERE message_id=@message_id AND channel_id=@channel_id AND edited IS TRUE AND array_position(edits, @which) IS NOT NULL LIMIT 1;");
                    cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                    cmd.Parameters.AddWithValue("which", NpgsqlDbType.TimestampTZ, which);
                    cmd.Prepare();

                    res = await cmd.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                var discord = this.MessageDiscordProperty.GetValue(msg) as DiscordClient;

                if (discord != null)
                    discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while retrieving message edit: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            }
            finally
            {
                this.Semaphore.Release();
            }

            if (!(res is DBNull))
            {
                var x = (string)res;
                this.KeyManager.Decrypt(x.FromBase64(), out x, "pgmain");
                return x;
            }
            return null;
        }

        public async Task<IEnumerable<Tuple<ulong, ulong>>> GetDeletesAsync(DiscordChannel chn, int limit)
        {
            if (!this.Settings.Enabled)
                return null;

            await this.Semaphore.WaitAsync();

            var lst = new List<Tuple<ulong, ulong>>();
            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                    cmd.CommandText = string.Concat("SELECT message_id, author_id, contents[array_length(contents, 1)] FROM ", tbl, " WHERE channel_id=@channel_id AND deleted IS TRUE ORDER BY message_id DESC LIMIT @limit;");
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)chn.Id);
                    cmd.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);
                    cmd.Prepare();

                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                            lst.Add(Tuple.Create((ulong)(long)rdr["message_id"], (ulong)(long)rdr["author_id"]));
                    }
                }
            }
            catch (Exception ex)
            {
                var discord = this.ChannelDiscordProperty.GetValue(chn) as DiscordClient;

                if (discord != null)
                    discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while retrieving deleted messages: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            }
            finally
            {
                this.Semaphore.Release();
            }

            return lst;
        }

        public async Task<string> GetDeleteAsync(DiscordChannel chn, ulong id)
        {
            if (!this.Settings.Enabled)
                return null;

            await this.Semaphore.WaitAsync();

            object res = null;
            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                    cmd.CommandText = string.Concat("SELECT contents[array_length(contents, 1)] FROM ", tbl, " WHERE message_id=@message_id AND channel_id=@channel_id AND deleted IS TRUE LIMIT 1;");
                    cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)id);
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)chn.Id);
                    cmd.Prepare();

                    res = await cmd.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                var discord = this.ChannelDiscordProperty.GetValue(chn) as DiscordClient;

                if (discord != null)
                    discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while retrieving deleted message: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            }
            finally
            {
                this.Semaphore.Release();
            }

            if (!(res is DBNull))
            {
                var x = (string)res;
                this.KeyManager.Decrypt(x.FromBase64(), out x, "pgmain");
                return x;
            }
            return null;
        }

        public async Task LogReactionAsync(DiscordEmoji emote, DiscordUser user, DiscordMessage message, DiscordChannel channel, bool action)
        {
            if (!this.Settings.Enabled)
                return;

            await this.Semaphore.WaitAsync();

            try
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    var tbl = string.Concat(this.Settings.TablePrefix, "reaction_log");

                    cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(message_id, channel_id, user_id, reaction, action, action_timestamp) VALUES(@message_id, @channel_id, @user_id, @reaction, @action, @action_timestamp);");
                    cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)message.Id);
                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel.Id);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)user.Id);
                    cmd.Parameters.AddWithValue("reaction", NpgsqlDbType.Text, emote.ToString());
                    cmd.Parameters.AddWithValue("action", NpgsqlDbType.Boolean, action);
                    cmd.Parameters.AddWithValue("action_timestamp", NpgsqlDbType.TimestampTZ, DateTimeOffset.Now);
                    cmd.Prepare();

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                var discord = this.MessageDiscordProperty.GetValue(message) as DiscordClient;

                if (discord != null)
                    discord.DebugLogger.LogMessage(LogLevel.Error, "DEvI DB", string.Concat("An exception occured while logging message reaction: ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        public async Task ConfigureGuildAsync(DiscordGuild guild, bool ignore)
        {
            if (!this.Settings.Enabled)
                return;

            await this.Semaphore.WaitAsync();

            using (var cmd = this.Connection.CreateCommand())
            {
                var tbl = string.Concat(this.Settings.TablePrefix, "log_ignore");

                if (ignore)
                    cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(guild_id) VALUES(@guild_id) ON CONFLICT(guild_id) DO NOTHING;");
                else
                    cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WHERE guild_id=@guild_id;");
                cmd.Parameters.AddWithValue("guild_id", NpgsqlDbType.Bigint, (long)guild.Id);
                cmd.Prepare();

                await cmd.ExecuteNonQueryAsync();
            }

            this.Semaphore.Release();
        }

        public async Task PreconfigureAsync()
        {
            if (!this.Settings.Enabled)
                return;

            this.Connection = new NpgsqlConnection(this.ConnectionString);
            await this.Connection.OpenAsync();

            await this.Semaphore.WaitAsync();

            using (var cmd = this.Connection.CreateCommand())
            {
                var tbl = string.Concat(this.Settings.TablePrefix, "log_ignore");

                cmd.CommandText = string.Concat("SELECT guild_id FROM ", tbl, ";");
                cmd.Prepare();

                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        this.IgnoredInternal.Add((ulong)(long)rdr["guild_id"]);
                    }
                }
            }

            this.Semaphore.Release();
        }

        public async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ExecuteQueryAsync(string query)
        {
            if (!this.Settings.Enabled)
                return null;

            await this.Semaphore.WaitAsync();

            var dicts = new List<IReadOnlyDictionary<string, string>>();
            using (var cmd = this.Connection.CreateCommand())
            {
                cmd.CommandText = query;

                try
                {
                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            var dict = new Dictionary<string, string>();

                            for (var i = 0; i < rdr.FieldCount; i++)
                                dict[rdr.GetName(i)] = rdr[i] is DBNull ? "<null>" : rdr[i].ToString();

                            dicts.Add(new ReadOnlyDictionary<string, string>(dict));
                        }
                    }
                }
                catch { }
            }

            this.Semaphore.Release();

            if (dicts != null)
                return new ReadOnlyCollection<IReadOnlyDictionary<string, string>>(dicts);
            return null;
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