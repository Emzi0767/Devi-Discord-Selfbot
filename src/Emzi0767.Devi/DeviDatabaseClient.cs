using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace Emzi0767.Devi
{
    public class DeviDatabaseClient
    {
        private DeviDatabaseSettings Settings { get; }
        private string ConnectionString { get; }

        public DeviDatabaseClient(DeviDatabaseSettings settings)
        {
            this.Settings = settings;

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

        public async Task LogMessageCreateAsync(DiscordMessage msg)
        {
            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync();

                var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(message_id, author_id, channel_id, created, edits, contents, embeds, attachment_urls, deleted, edited) VALUES(@message_id, @author_id, @channel_id, @created, @edits, @contents, @embeds, @attachment_urls, @deleted, @edited);");
                cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                cmd.Parameters.AddWithValue("author_id", NpgsqlDbType.Bigint, (long)msg.Author.Id);
                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                cmd.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTZ, msg.CreationDate);
                cmd.Parameters.AddWithValue("edits", NpgsqlDbType.Text | NpgsqlDbType.Array, new string[] {});
                cmd.Parameters.AddWithValue("contents", NpgsqlDbType.Text | NpgsqlDbType.Array, new[] { msg.Content });
                cmd.Parameters.AddWithValue("embeds", NpgsqlDbType.Jsonb | NpgsqlDbType.Array, JsonConvert.SerializeObject(msg.Embeds));
                cmd.Parameters.AddWithValue("attachment_urls", NpgsqlDbType.Text | NpgsqlDbType.Array, msg.Attachments.Select(xa => xa.Url).ToArray());
                cmd.Parameters.AddWithValue("deleted", NpgsqlDbType.Boolean, false);
                cmd.Parameters.AddWithValue("edited", NpgsqlDbType.Boolean, false);
                cmd.Prepare();

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task LogMessageDeleteAsync(DiscordMessage msg)
        {
            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync();

                var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                cmd.CommandText = string.Concat("UPDATE ", tbl, " SET deleted=1 WHERE message_id=@message_id AND channel_id=@channel_id");
                cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                cmd.Prepare();

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task LogMessageEditAsync(DiscordMessage msg)
        {
            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync();

                var tbl = string.Concat(this.Settings.TablePrefix, "message_log");

                cmd.CommandText = string.Concat("UPDATE ", tbl, " SET edited=@edited, contents=array_append(contents, @contents), embeds=array_append(embeds, @embeds), edits=array_append(edits, @edit) WHERE message_id=@message_id AND channel_id=@channel_id");
                cmd.Parameters.AddWithValue("edited", NpgsqlDbType.Boolean, true);
                cmd.Parameters.AddWithValue("message_id", NpgsqlDbType.Bigint, (long)msg.Id);
                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)msg.Channel.Id);
                cmd.Parameters.AddWithValue("contents", msg.Content);
                cmd.Parameters.AddWithValue("embeds", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(msg.Embeds));
                cmd.Parameters.AddWithValue("edit", NpgsqlDbType.TimestampTZ, msg.EditedTimestamp);
                cmd.Prepare();

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task LogReactionAsync(DiscordEmoji emote, DiscordUser user, DiscordMessage message, DiscordChannel channel, bool action)
        {
            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync();

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
    }
}