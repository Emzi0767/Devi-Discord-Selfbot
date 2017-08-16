using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace Emzi0767.Devi.Services
{
    public class DeviUtilities
    {
        public async Task<DiscordMessage> SendTextAsync(CommandContext ctx, string content)
        {
            var msg = ctx.Message;
            var mod = msg.Author.Id == ctx.Client.CurrentUser.Id;

            if (mod)
                await msg.EditAsync(content);
            else
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content));

            return msg;
        }

        public Task<DiscordMessage> SendEmbedAsync(CommandContext ctx, DiscordEmbed embed)
        {
            return this.SendEmbedAsync(ctx, embed, null);
        }

        public async Task<DiscordMessage> SendEmbedAsync(CommandContext ctx, DiscordEmbed embed, string content)
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

        public DiscordEmbedBuilder BuildEmbed(string title, string desc, int type)
        {
            var embed = new DiscordEmbedBuilder()
            {
                Title = title,
                Description = desc
            };
            switch (type)
            {
                default:
                case 0:
                    embed.Color = new DiscordColor(0x007FFF);
                    break;

                case 1:
                    embed.Color = new DiscordColor(0xFF0000);
                    break;

                case 2:
                    embed.Color = new DiscordColor(0x7FFF00);
                    break;
            }
            if (type == 1)
                embed.ThumbnailUrl = "http://i.imgur.com/F9HGvxs.jpg";
            return embed;
        }

        public string ObjectToString(object o)
        {
            if (o == null)
                return "*null*";

            switch (o)
            {
                case DateTime dt:
                    return dt.ToString("yyyy-MM-dd HH:mm:ss zzz");
                
                case DateTimeOffset dto:
                    return dto.ToString("yyyy-MM-dd HH:mm:ss zzz");
                
                case TimeSpan ts:
                    return ts.ToString("c");
                
                case Enum e:
                    var flags = Enum.GetValues(e.GetType())
                        .OfType<Enum>()
                        .Where(xev => e.HasFlag(xev))
                        .Select(xev => xev.ToString());
                    return string.Concat(", ", flags);
                
                default:
                    return o.ToString();
            }
        }

        public async Task QuoteAsync(CommandContext ctx, DiscordMessage msg, string qmsg)
        {
            var txt = qmsg ?? ctx.Dependencies.GetDependency<DeviEmojiMap>().Mapping["speech_balloon"];
            txt = txt.Trim();

            DiscordEmbedBuilder embed = null;
            if (msg != null)
            {
                embed = BuildQuoteEmbed(msg, ctx);
            }
            else
                embed = BuildEmbed("Failed to quote message", null, 1);

            await this.SendEmbedAsync(ctx, embed.Build(), txt);
        }

        public DiscordEmbedBuilder BuildQuoteEmbed(DiscordMessage msq, CommandContext ctx)
        {
            var author = msq.Author;
            var author1 = ctx.Guild.GetMemberAsync(author.Id).GetAwaiter().GetResult();

            var roles = author1.Roles.OrderByDescending(xr => xr.Position);
            var role = roles.FirstOrDefault(xr => xr.Color.Value != 0);
            var color = role.Color;

            var embed = this.BuildEmbed(null, msq.Content, 0);
            embed.Color = color;
            embed.Timestamp = msq.Timestamp;
            embed.WithAuthor(author1 != null ? author1.Nickname ?? author.Username : author.Username, null, author.AvatarUrl);

            var att = msq.Attachments.FirstOrDefault();
            if (att != null)
            {
                embed.AddField("Attachment", string.Concat("[Link](", att.Url, ")"), false);

                if (att.Width != 0 && att.Height != 0)
                    embed.ImageUrl = att.Url;
            }

            return embed;
        }
    }
}