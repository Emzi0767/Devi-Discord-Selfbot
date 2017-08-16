using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.Devi.Services;

namespace Emzi0767.Devi
{
    [Group("log")]
    public sealed class DeviLogManagementModule
    {
        private DeviDatabaseClient DatabaseClient { get; }
        private DeviUtilities Utilities { get; }

        public DeviLogManagementModule(DeviUtilities utils, DeviDatabaseClient database)
        {
            this.Utilities = utils;
            this.DatabaseClient = database;
        }

        [Command("ignore")]
        public async Task IgnoreAsync(CommandContext ctx, params DiscordGuild[] glds)
        {
            foreach (var gld in glds)
                await this.DatabaseClient.ConfigureGuildAsync(gld, true);
            var gls = string.Join(", ", glds.Select(xg => string.Concat("`", Formatter.Strip(xg.Name), "`")));
            await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Configration successful", string.Concat("Following guilds are now exempt from logging:\n", gls), 0).Build());
        }

        [Command("unignore")]
        public async Task UnignoreAsync(CommandContext ctx, params DiscordGuild[] glds)
        {
            foreach (var gld in glds)
                await this.DatabaseClient.ConfigureGuildAsync(gld, false);
            var gls = string.Join(", ", glds.Select(xg => string.Concat("`", Formatter.Strip(xg.Name), "`")));
            await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Configration successful", string.Concat("Following guilds are no longer exempt from logging:\n", gls), 0).Build());
        }

        [Group("query")]
        public class LogQuery
        {
            private DeviDatabaseClient DatabaseClient { get; }
            private DeviUtilities Utilities { get; }

            public LogQuery(DeviUtilities utils, DeviDatabaseClient database)
            {
                this.Utilities = utils;
                this.DatabaseClient = database;
            }

            [Command("edits")]
            public async Task EditsForAsync(CommandContext ctx, ulong id)
            {
                var mss = await ctx.Channel.GetMessagesAsync(around: id, limit: 3);
                var msq = mss.FirstOrDefault(xm => xm.Id == id);

                if (msq == null)
                {
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "Message with specified ID was not found in this channel. Perhaps try deleted messages?", 1).Build());
                    return;
                }

                var edits = await this.DatabaseClient.GetEditsAsync(msq);
                if (edits == null || edits.Count() <= 1)
                {
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "This message does not have any edits registered.", 1).Build());
                    return;
                }

                var editstr = string.Join("\n", edits.Select(xdto => xdto.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")));

                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Available edits", editstr, 0).Build());
            }

            [Command("edit")]
            public async Task ViewEditAsync(CommandContext ctx, ulong id, DateTimeOffset which)
            {
                var mss = await ctx.Channel.GetMessagesAsync(around: id, limit: 3);
                var msq = mss.FirstOrDefault(xm => xm.Id == id);

                if (msq == null)
                {
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "Message with specified ID was not found in this channel. Perhaps try deleted messages?", 1).Build());
                    return;
                }

                var edits = await this.DatabaseClient.GetEditsAsync(msq);
                if (edits == null || edits.Count() <= 1)
                {
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "This message does not have any edits registered.", 1).Build());
                    return;
                }

                var editstr = await this.DatabaseClient.GetEditAsync(msq, which);

                if (editstr != null)
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed(string.Concat("Edit from ", which.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")), editstr, 0).Build());
                else
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "Specfied edit was not found.", 1).Build());
            }

            [Command("deletes")]
            public async Task GetDeletesAsync(CommandContext ctx, int limit = 10, DiscordChannel chn = null)
            {
                chn = chn ?? ctx.Channel;
                var deletes = await this.DatabaseClient.GetDeletesAsync(chn, limit);
                var str = string.Join("\n\n", deletes.Select(xtpl => string.Concat(xtpl.Item1, " by <@!", xtpl.Item2, ">")));

                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed(string.Concat("Messages deleted in ", chn.Name), str, 0).Build());
            }

            [Command("deleted")]
            public async Task GetDeletedAsync(CommandContext ctx, ulong id, DiscordChannel chn = null)
            {
                chn = chn ?? ctx.Channel;
                var delete = await this.DatabaseClient.GetDeleteAsync(chn, id);

                if (delete != null)
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed(string.Concat("Messages ", id), delete, 0).Build());
                else
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "Specfied messages was not found or was not deleted.", 1).Build());
            }

            [Command("sql"), Hidden, RequireOwner]
            public async Task SqlAsync(CommandContext ctx, [RemainingText] string query)
            {
                var dat = await this.DatabaseClient.ExecuteQueryAsync(query);

                if (!dat.Any() || !dat.First().Any())
                {
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Given query produced no results.", string.Concat("Query: ", Formatter.InlineCode(query), "."), 0).Build());
                    return;
                }

                var d0 = dat.First().Select(xd => xd.Key).OrderByDescending(xs => xs.Length).First().Length + 1;

                var embed = this.Utilities.BuildEmbed(string.Concat("Results: ", dat.Count.ToString("#,##0")), string.Concat("Showing ", dat.Count > 24 ? "first 24" : "all", " results for query ", Formatter.InlineCode(query), ":"), 0);
                var adat = dat.Take(24);

                var i = 0;
                foreach (var xdat in adat)
                {
                    var sb = new StringBuilder();

                    foreach (var (k, v) in xdat)
                        sb.Append(k).Append(new string(' ', d0 - k.Length)).Append("| ").AppendLine(v);

                    embed.AddField(string.Concat("Result #", i++), Formatter.BlockCode(sb.ToString()), false);
                }

                if (dat.Count > 24)
                    embed.AddField("Display incomplete", string.Concat((dat.Count - 24).ToString("#,##0"), " results were omitted."), false);
                
                await this.Utilities.SendEmbedAsync(ctx, embed.Build());
            }
        }
    }
}