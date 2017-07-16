using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;

namespace Emzi0767.Devi
{
    [Group("log")]
    public sealed class DeviLogManagementModule
    {
        private DeviDatabaseClient DatabaseClient { get; }

        public DeviLogManagementModule(DeviDatabaseClient database)
        {
            this.DatabaseClient = database;
        }

        [Command("ignore")]
        public async Task IgnoreAsync(CommandContext ctx, params DiscordGuild[] glds)
        {
            foreach (var gld in glds)
                await this.DatabaseClient.ConfigureGuildAsync(gld, true);
            var gls = string.Join(", ", glds.Select(xg => string.Concat("`", Formatter.Strip(xg.Name), "`")));
            await this.SendEmbedAsync(ctx, BuildEmbed("Configration successful", string.Concat("Following guilds are now exempt from loggin:\n", gls), 0));
        }

        [Command("unignore")]
        public async Task UnignoreAsync(CommandContext ctx, params DiscordGuild[] glds)
        {
            foreach (var gld in glds)
                await this.DatabaseClient.ConfigureGuildAsync(gld, false);
            var gls = string.Join(", ", glds.Select(xg => string.Concat("`", Formatter.Strip(xg.Name), "`")));
            await this.SendEmbedAsync(ctx, BuildEmbed("Configration successful", string.Concat("Following guilds are no longer exempt from loggin:\n", gls), 0));
        }

        #region Messaging code
        private async Task<DiscordMessage> SendTextAsync(CommandContext ctx, string content)
        {
            var msg = ctx.Message;
            var mod = msg.Author.Id == ctx.Client.CurrentUser.Id;

            if (mod)
                await msg.EditAsync(content);
            else
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content));

            return msg;
        }

        private Task<DiscordMessage> SendEmbedAsync(CommandContext ctx, DiscordEmbed embed)
        {
            return this.SendEmbedAsync(ctx, embed, null);
        }

        private async Task<DiscordMessage> SendEmbedAsync(CommandContext ctx, DiscordEmbed embed, string content)
        {
            var msg = ctx.Message;
            var mod = msg.Author.Id == ctx.Client.CurrentUser.Id;

            if (mod)
                await msg.EditAsync(!string.IsNullOrWhiteSpace(content) ? content : msg.Content, embed);
            else if (!string.IsNullOrWhiteSpace(content))
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content), false, embed);
            else
                msg = await msg.Channel.SendMessageAsync(msg.Author.Mention, false, embed);

            return msg;
        }

        private static DiscordEmbed BuildEmbed(string title, string desc, int type)
        {
            var embed = new DiscordEmbed()
            {
                Title = title,
                Description = desc,
                Fields = new List<DiscordEmbedField>()
            };
            switch (type)
            {
                default:
                case 0:
                    embed.Color = 0x007FFF;
                    break;

                case 1:
                    embed.Color = 0xFF0000;
                    break;

                case 2:
                    embed.Color = 0x7FFF00;
                    break;
            }
            if (type == 1)
                embed.Thumbnail = new DiscordEmbedThumbnail { Url = "http://i.imgur.com/F9HGvxs.jpg" };
            return embed;
        }
        #endregion
    }
}