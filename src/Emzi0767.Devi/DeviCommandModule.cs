using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;

namespace Emzi0767.Devi
{
    public class DeviCommandModule : ModuleBase
    {
        [Command("random"), Summary("Generates a random number between *min* and *max*.")]
        public async Task GenRandom(int min, int max)
        {
            var msg = this.Context.Message;

            var rnd = new Random();
            var num = rnd.Next(min, max);
            
            await this.SendEmbedAsync(BuildEmbed("Random Number", num.ToString("#,##0"), 0));
        }

        [Command("eval", RunMode = RunMode.Async), Summary("Evaluates C# code.")]
        public async Task Eval([Remainder] string code)
        {
            var msg = this.Context.Message;

            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.IndexOf("```", cs1);
            var cs = code.Substring(cs1, cs2 - cs1);
            
            var nmsg = await this.SendEmbedAsync(BuildEmbed("Evaluating...", null, 0));

            try
            {
                var globals = new DeviVariables();
                globals.Message = this.Context.Message as SocketUserMessage;

                var sopts = ScriptOptions.Default;
                sopts = sopts.WithImports("System", "System.Linq", "Discord", "Discord.WebSocket");
                sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                var script = CSharpScript.Create(cs, sopts, typeof(DeviVariables));
                script.Compile();
                var result = await script.RunAsync(globals);

                if (result != null && result.ReturnValue != null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                    await this.SendEmbedAsync(BuildEmbed("Evaluation Result", result.ReturnValue.ToString(), 2), nmsg);
                else
                    await this.SendEmbedAsync(BuildEmbed("Evaluation Successful", "No result was returned.", 2), nmsg);
            }
            catch (Exception ex)
            {
                await this.SendEmbedAsync(BuildEmbed("Evaluation Failure", string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message), 1), nmsg);
            }
        }

        [Command("nitro")]
        public async Task Nitro(ulong guild, ulong channel)
        {
            var cln = this.Context.Client as DiscordSocketClient;
            var gld = cln.Guilds.FirstOrDefault(xg => xg.Id == guild) as SocketGuild;
            var chn = gld.Channels.FirstOrDefault(xc => xc.Id == channel) as SocketTextChannel;

            var embed = new EmbedBuilder();
            embed.Color = new Color(5267072);
            embed.Url = "https://discordapp.com/nitro";
            embed.Author = new EmbedAuthorBuilder();
            embed.Author.Name = "Discord Nitro Message";
            embed.Author.IconUrl = "https://cdn.discordapp.com/emojis/261735650192130049.png";
            embed.Description = "**Discord Nitro** is required to view this message.";
            embed.ThumbnailUrl = "http://i.imgur.com/1dH8EJa.png";

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("quote")]
        public async Task Quote(ulong id, [Remainder] string message = null)
        {
            var msg = this.Context.Message;
            var chn = msg.Channel as SocketTextChannel;
            var mss = await chn.GetMessagesAsync().Flatten();
            var msq = mss.FirstOrDefault(xmsg => xmsg.Id == id);

            await this.QuoteAsync(msq, message);
        }

        [Command("quote")]
        public async Task Quote(IUser user, [Remainder] string message = null)
        {
            var msg = this.Context.Message;
            var chn = msg.Channel as SocketTextChannel;
            var mss = await chn.GetMessagesAsync().Flatten();
            var msq = mss.OrderBy(xmsg => xmsg.Timestamp).LastOrDefault(xmsg => xmsg.Author != null && xmsg.Author.Id == user.Id);

            await this.QuoteAsync(msq, message);
        }

        [Command("emoji")]
        public async Task Emoji(string emoji)
        {
            if (emoji.StartsWith("="))
            {
                var e = emoji.Substring(1);
                
                if (Program.EmojiMap1.ContainsKey(e))
                {
                    e = Program.EmojiMap1[e];

                    var utf32 = new UTF32Encoding(true, false);
                    var eids = Program.EmojiMap2[e];
                    var xchr = utf32.GetBytes(e);
                    var echr = string.Concat(xchr.Select(xb => xb.ToString("X2")));
                    echr = echr.StartsWith("0000") ? echr.Substring(4) : echr;
                    echr = string.Concat("U+", echr);

                    var estr = string.Concat("Character: `", e, "`");

                    var einf = string.Concat("Emoji: ", e, " (\\", e, ")\n", estr, "\n", echr);

                    if (eids != null && eids.Count() > 0)
                        einf = string.Concat(einf, "\nKnown names: `", string.Join(", ", eids), "`");

                    await this.SendTextAsync(einf);
                }
                else
                {
                    await this.SendTextAsync(string.Concat(Program.EmojiMap1["poop"], " (this is an error)"));
                }
            }
            else
            {
                var utf32 = new UTF32Encoding(true, false);
                var eids = Program.EmojiMap2.ContainsKey(emoji) ? Program.EmojiMap2[emoji] : null;

                if (!emoji.StartsWith("<:"))
                {
                    var xchr = utf32.GetBytes(emoji);
                    var echr = string.Concat(xchr.Select(xb => xb.ToString("X2")));
                    echr = echr.StartsWith("0000") ? echr.Substring(4) : echr;
                    echr = string.Concat("U+", echr);

                    var estr = string.Concat("Character: `", emoji, "`");

                    var einf = string.Concat("Emoji: ", emoji, " (\\", emoji, ")\n", estr, "\n", echr);

                    if (eids != null && eids.Count() > 0)
                        einf = string.Concat(einf, "\nKnown names: `", string.Join(", ", eids), "`");

                    await this.SendTextAsync(einf);
                }
                else
                {
                    await this.SendTextAsync(string.Concat("Emoji: ", emoji, " (`", emoji, "`)"));
                }
            }
        }

        [Command("guildemoji")]
        public async Task GuildEmoji()
        {
            var emoji = Program.GuildEmoji.OrderBy(xkvp => xkvp.Key).Select(xkvp => string.Concat(xkvp.Key.Replace("_", @"\_"), ": ", xkvp.Value));
            var sb = new StringBuilder();
            var embed = BuildEmbed("All guild emoji", emoji.Count().ToString("#,### total"), 0);
            foreach (var e in emoji)
            {
                if (sb.Length + 1 + e.Length >= 1023)
                {
                    embed.AddField(x => { x.Name = "Emoji"; x.Value = sb.ToString(); x.IsInline = true; });
                    sb = new StringBuilder();
                }
                sb.Append(e).Append("\n");
            }
            embed.AddField(x => { x.Name = "Emoji"; x.Value = sb.ToString(); x.IsInline = true; });
            await this.SendEmbedAsync(embed);
        }

        [Command("guildemoji")]
        public async Task GuildEmoji(string emoji)
        {
            var e = Program.GuildEmoji[emoji];
            await this.SendEmbedAsync(BuildEmbed(null, e, 0), "");
        }

        [Command("dong")]
        public async Task Dong(string dong)
        {
            await this.SendTextAsync(Program.Dongers.Dongers[dong]);
        }

        [Command("ping")]
        public async Task Ping()
        {
            var client = this.Context.Client as DiscordSocketClient;

            var sw = new Stopwatch();
            sw.Start();
            var msg = await this.SendTextAsync("Performing pings...");
            sw.Stop();

            await this.SendTextAsync(string.Concat("**Socket latency**: ", client.Latency.ToString("#,##0"), "ms\n**API latency**: ", sw.ElapsedMilliseconds.ToString("#,##0"), "ms"), msg);
        }

        [Command("settings")]
        public async Task Settings(string setting, string operation, string value)
        {
            var gld = this.Context.Guild as SocketGuild;
            if (gld == null)
                throw new Exception("Invalid state");

            var st = setting.ToLower();
            var op = operation.ToLower();
            var rs = 3;

            if (st == "prefix")
            {
                if (op == "set")
                    Program.Settings.Prefix = value;
                else if (op == "del")
                    Program.Settings.Prefix = "devi:";
                else rs ^= 1;
            }
            else rs ^= 2;

            if (rs == 3)
                await this.SendEmbedAsync(BuildEmbed("Success", "Setting changed successfully", 2));
            else if (rs == 2)
                await this.SendEmbedAsync(BuildEmbed("Failure", "Invalid operation", 1));
            else if (rs == 1)
                await this.SendEmbedAsync(BuildEmbed("Failure", "Invalid setting", 1));
        }

        [Command("save")]
        public async Task Save()
        {
            var json = JsonConvert.SerializeObject(Program.Settings, Formatting.None);
            var l = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            l = Path.Combine(l, "devi.json");
            File.WriteAllText(l, json, new UTF8Encoding(false));

            await this.SendTextAsync("All settings saved");
        }

        private async Task QuoteAsync(IMessage msg, string qmsg)
        {
            var txt = qmsg ?? Program.EmojiMap1["speech_balloon"];
            txt = txt.Trim();

            var embed = (EmbedBuilder)null;
            if (msg != null && (msg.Author is SocketGuildUser || msg.Author is RestUser))
            {
                embed = BuildQuoteEmbed(msg, this.Context);
            }
            else
                embed = BuildEmbed("Failed to quote message", null, 1);

            await this.SendEmbedAsync(embed, txt);
        }

        private Task<IUserMessage> SendTextAsync(string content)
        {
            return this.SendTextAsync(content, this.Context.Message);
        }

        private async Task<IUserMessage> SendTextAsync(string content, IUserMessage nmsg)
        {
            var msg = nmsg;
            var mod = msg.Author.Id == this.Context.Client.CurrentUser.Id;

            if (mod)
                await msg.ModifyAsync(x => x.Content = content);
            else
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content));

            return msg;
        }

        private Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed)
        {
            return this.SendEmbedAsync(embed, null, this.Context.Message);
        }

        private Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed, IUserMessage nmsg)
        {
            return this.SendEmbedAsync(embed, null, nmsg);
        }

        private Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed, string content)
        {
            return this.SendEmbedAsync(embed, content, this.Context.Message);
        }

        private async Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed, string content, IUserMessage nmsg)
        {
            var msg = nmsg;
            var mod = msg.Author.Id == this.Context.Client.CurrentUser.Id;

            if (mod)
                await msg.ModifyAsync(x =>
                {
                    x.Embed = embed.Build();
                    if (!string.IsNullOrWhiteSpace(content))
                        x.Content = content;
                    else
                        x.Content = msg.Content;
                });
            else if (!string.IsNullOrWhiteSpace(content))
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content), false, embed);
            else
                msg = await msg.Channel.SendMessageAsync(msg.Author.Mention, false, embed);

            return msg;
        }

        private static EmbedBuilder BuildEmbed(string title, string desc, int type)
        {
            var embed = new EmbedBuilder();
            embed.Title = title;
            embed.Description = desc;
            switch (type)
            {
                default:
                case 0:
                    embed.Color = new Color(0, 127, 255);
                    break;

                case 1:
                    embed.Color = new Color(255, 0, 0);
                    break;

                case 2:
                    embed.Color = new Color(127, 255, 0);
                    break;
            }
            if (type == 1)
                embed.ThumbnailUrl = "http://i.imgur.com/F9HGvxs.jpg";
            return embed;
        }

        private static EmbedBuilder BuildQuoteEmbed(IMessage msq, CommandContext ctx)
        {
            var author1 = msq.Author as SocketGuildUser;
            var author2 = msq.Author as RestUser;
            var author = (IUser)author1 ?? author2;

            var color = (Color?)null;
            if (author1 != null && (author1.RoleIds != null || author1.RoleIds.Any()))
            {
                var roles = author1.RoleIds.Select(xid => ctx.Guild.GetRole(xid)).OrderByDescending(xrole => xrole.Position);
                var role = roles.FirstOrDefault(xr => xr.Color.RawValue != 0);
                color = role != null ? (Color?)role.Color : null;
            }

            var embed = BuildEmbed(null, msq.Content, 0);
            embed.Color = color;
            embed.Author = new EmbedAuthorBuilder();
            embed.Author.IconUrl = author.AvatarUrl;
            embed.Author.Name = author1 != null ? author1.Nickname ?? author.Username : author.Username;
            embed.Timestamp = msq.Timestamp;

            var att = msq.Attachments.FirstOrDefault();
            if (att != null)
            {
                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "Attachment";
                    x.Value = string.Concat("[Link](", att.Url, ")");
                });

                if (att.Width != null && att.Height != null)
                    embed.ImageUrl = att.Url;
            }

            return embed;
        }
    }
}
