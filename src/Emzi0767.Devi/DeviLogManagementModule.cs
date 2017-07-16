using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.Devi.Services;
using Newtonsoft.Json;

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
            await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Configration successful", string.Concat("Following guilds are now exempt from logging:\n", gls), 0));
        }

        [Command("unignore")]
        public async Task UnignoreAsync(CommandContext ctx, params DiscordGuild[] glds)
        {
            foreach (var gld in glds)
                await this.DatabaseClient.ConfigureGuildAsync(gld, false);
            var gls = string.Join(", ", glds.Select(xg => string.Concat("`", Formatter.Strip(xg.Name), "`")));
            await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Configration successful", string.Concat("Following guilds are no longer exempt from logging:\n", gls), 0));
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
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "Message with specified ID was not found in this channel. Perhaps try deleted messages?", 1));
                    return;
                }

                var edits = await this.DatabaseClient.GetEditsAsync(msq);
                if (edits == null || edits.Count() <= 1)
                {
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Query failed", "This message does not have any edits registered.", 1));
                    return;
                }

                var editstr = string.Join(", ", edits.Select(xdto => string.Concat("`", xdto.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"), "`")));

                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Available edits", editstr, 0));
            }
        }
    }
}