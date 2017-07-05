using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.Devi.Services;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;

namespace Emzi0767.Devi
{
    public class DeviCommandModule
    {
        private DeviSettingStore Settings { get; }
        private DeviEmojiMap EmojiMap { get; }
        private DeviDongerMap Dongers { get; }
        private DeviGuildEmojiMap GuildEmoji { get; }

        public DeviCommandModule()
        {
            this.Settings = Program.Settings;
            this.EmojiMap = Program.EmojiMap;
            this.Dongers = Program.Dongers;
            this.GuildEmoji = Program.GuildEmoji;
        }

        [Command("random"), Description("Generates a random number between *min* and *max*.")]
        public async Task GenRandom(CommandContext ctx, int min, int max)
        {
            var msg = ctx.Message;

            var rnd = new Random();
            var num = rnd.Next(min, max);

            await this.SendEmbedAsync(ctx, BuildEmbed("Random Number", num.ToString("#,##0"), 0));
        }

        [Command("eval"), Description("Evaluates C# code.")]
        public async Task Eval(CommandContext ctx, [RemainingText] string code)
        {
            var result = await EvaluateAsync(ctx, code);

            if (result != null && result.ReturnValue != null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                await this.SendEmbedAsync(ctx, BuildEmbed("Evaluation Result", result.ReturnValue.ToString(), 2));
            else
                await this.SendEmbedAsync(ctx, BuildEmbed("Evaluation Successful", "No result was returned.", 2));
        }

        [Command("inspect"), Description("Evaluates a snippet of code, and inspects the result.")]
        public async Task Inspect(CommandContext ctx, [RemainingText] string code)
        {
            var result = await EvaluateAsync(ctx, code);

            if (result != null && result.ReturnValue != null)
            {
                var t = result.ReturnValue.GetType();
                var ti = t.GetTypeInfo();
                var embed = BuildEmbed("Return value inspection", string.Concat("Return type:", t.ToString()), 2);
                
                if (ti.IsPrimitive || ti.IsEnum || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan))
                    embed.Fields.Add(new DiscordEmbedField { Name = "Value", Value = this.ObjectToString(result.ReturnValue), Inline = false });
                else
                {
                    var rv = result.ReturnValue;
                    var psr = ti.GetProperties();
                    var ps = psr.Take(25);
                    var efs = ps.Select(xp => new DiscordEmbedField { Name = string.Concat(xp.Name, " (", xp.DeclaringType.ToString(), ")"), Value = this.ObjectToString(rv), Inline = true });
                    embed.Fields.AddRange(efs);

                    if (psr.Length > 25)
                        embed.Description = string.Concat(embed.Description, "\n\n**Warning**: Property count exceeds 25. Not all properties are displayed.");
                }

                await this.SendEmbedAsync(ctx, embed);
            }
            else
                await this.SendEmbedAsync(ctx, BuildEmbed("Inspection failed", "No result was returned.", 1));
        }

        [Command("nitro")]
        public async Task Nitro(CommandContext ctx, ulong guild, ulong channel)
        {
            var cln = ctx.Client;
            var gld = cln.Guilds.FirstOrDefault(xg => xg.Key == guild);
            var chn = gld.Value.Channels.FirstOrDefault(xc => xc.Id == channel);

            var embed = new DiscordEmbed()
            {
                Color = 5267072,
                Url = "https://discordapp.com/nitro",
                Description = "**Discord Nitro** is required to view this message.",
                Thumbnail = new DiscordEmbedThumbnail { Url = "http://i.imgur.com/1dH8EJa.png" },
                Author = new DiscordEmbedAuthor()
                {
                    Name = "Discord Nitro Message",
                    IconUrl = "https://cdn.discordapp.com/emojis/261735650192130049.png"
                }
            };

            await chn.SendMessageAsync("", false, embed);
        }

        [Command("quote")]
        public async Task Quote(CommandContext ctx, ulong msqid, [RemainingText] string message = null)
        {
            var msg = ctx.Message;
            var chn = msg.Channel;
            var mss = await chn.GetMessagesAsync(around: msqid, limit: 3);
            var msq = mss.FirstOrDefault(xm => xm.Id == msqid);

            await this.QuoteAsync(ctx, msq, message);
        }

        [Command("quoteuser")]
        public async Task Quote(CommandContext ctx, DiscordMember user, [RemainingText] string message = null)
        {
            var msg = ctx.Message;
            var chn = msg.Channel;
            var mss = await chn.GetMessagesAsync(limit: 100);
            var msq = mss.OrderBy(xmsg => xmsg.Timestamp).LastOrDefault(xmsg => xmsg.Author != null && xmsg.Author.Id == user.Id);

            await this.QuoteAsync(ctx, msq, message);
        }

        [Command("emoji")]
        public async Task Emoji(CommandContext ctx, string emoji)
        {
            if (emoji.StartsWith("="))
            {
                var e = emoji.Substring(1);
                
                if (this.EmojiMap.Mapping.ContainsKey(e))
                {
                    e = this.EmojiMap.Mapping[e];

                    var utf32 = new UTF32Encoding(true, false);
                    var eids = this.EmojiMap.ReverseMapping[e];
                    var xchr = utf32.GetBytes(e);
                    var echr = string.Concat(xchr.Select(xb => xb.ToString("X2")));
                    echr = string.Concat("U+", echr);

                    var estr = string.Concat("Character: `", e, "`");

                    var einf = string.Concat("Emoji: ", e, "\n", estr, "\n", echr);

                    if (eids != null && eids.Count() > 0)
                        einf = string.Concat(einf, "\nKnown names: `", string.Join(", ", eids), "`");

                    await this.SendTextAsync(ctx, einf);
                }
                else
                {
                    await this.SendTextAsync(ctx, string.Concat(this.EmojiMap.Mapping["poop"], " (this is an error)"));
                }
            }
            else
            {
                var utf32 = new UTF32Encoding(true, false);
                var eids = this.EmojiMap.ReverseMapping.ContainsKey(emoji) ? this.EmojiMap.ReverseMapping[emoji] : null;

                if (!emoji.StartsWith("<:"))
                {
                    var xchr = utf32.GetBytes(emoji);
                    var echr = string.Concat(xchr.Select(xb => xb.ToString("X2")));
                    echr = echr.StartsWith("0000") ? echr.Substring(4) : echr;
                    echr = string.Concat("U+", echr);

                    var estr = string.Concat("Character: `", emoji, "`");

                    var einf = string.Concat("Emoji: ", emoji, ")\n", estr, "\n", echr);

                    if (eids != null && eids.Count() > 0)
                        einf = string.Concat(einf, "\nKnown names: `", string.Join(", ", eids), "`");

                    await this.SendTextAsync(ctx, einf);
                }
                else
                {
                    await this.SendTextAsync(ctx, string.Concat("Emoji: ", emoji, " (`", emoji, "`)"));
                }
            }
        }

        [Command("guildemojishow")]
        public async Task GuildEmojiShow(CommandContext ctx)
        {
            var emoji = this.GuildEmoji.Mapping.OrderBy(xkvp => xkvp.Key).Select(xkvp => string.Concat(xkvp.Key.Replace("_", @"\_"), ": ", xkvp.Value));
            var sb = new StringBuilder();
            var embed = BuildEmbed("All guild emoji", emoji.Count().ToString("#,### total"), 0);
            embed.Fields = new List<DiscordEmbedField>();
            foreach (var e in emoji)
            {
                if (sb.Length + 1 + e.Length >= 1023)
                {
                    embed.Fields.Add(new DiscordEmbedField { Name = "Emoji", Value = sb.ToString(), Inline = true });
                    sb = new StringBuilder();
                }
                sb.Append(e).Append("\n");
            }
            embed.Fields.Add(new DiscordEmbedField { Name = "Emoji", Value = sb.ToString(), Inline = true });
            await this.SendEmbedAsync(ctx, embed);
        }

        [Command("guildemoji")]
        public async Task GuildEmojiShow(CommandContext ctx, string emoji)
        {
            var e = this.GuildEmoji.Mapping[emoji];
            await this.SendEmbedAsync(ctx, BuildEmbed(null, e, 0), "");
        }

        [Command("dong")]
        public async Task Dong(CommandContext ctx, string dong)
        {
            await this.SendTextAsync(ctx, this.Dongers.Dongers[dong]);
        }

        [Command("imply")]
        public async Task Imply(CommandContext ctx, params string[] implication_content)
        {
            // <:Implying:261288929628651540>
            var implication = string.Join(" ", implication_content);

            var alph = "abcdefghijklmnopqrstuvwxyz".ToDictionary(xc => xc, xc => string.Concat("regional_indicator_", xc));
            foreach (var xc in "0123456789")
                alph.Add(xc, string.Concat("number_", xc));

            alph = alph
                .Select(xkvp => new KeyValuePair<char, string>(xkvp.Key, this.EmojiMap.Mapping[xkvp.Value]))
                .ToDictionary(xkvp => xkvp.Key, xkvp => xkvp.Value);

            var impc = implication
                .ToLower()
                .Select(xc => alph.ContainsKey(xc) ? alph[xc] : xc.ToString());

            var impl = string.Join(" ", impc);
            await this.SendTextAsync(ctx, string.Concat("<:Implying:261288929628651540> ", impl));
        }

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            var client = ctx.Client;

            var sw = new Stopwatch();
            sw.Start();
            var msg = await this.SendTextAsync(ctx, "Performing pings...");
            sw.Stop();

            await this.SendTextAsync(ctx, string.Concat("**Socket latency**: ", client.Ping.ToString("#,##0"), "ms\n**API latency**: ", sw.ElapsedMilliseconds.ToString("#,##0"), "ms"));
        }

        [Command("settings")]
        public async Task SettingsControl(CommandContext ctx, string setting, string operation, string value)
        {
            var gld = ctx.Guild;
            if (gld == null)
                throw new Exception("Invalid state");

            var st = setting.ToLower();
            var op = operation.ToLower();
            var rs = 3;

            if (st == "prefix")
            {
                if (op == "set")
                    this.Settings.Prefix = value;
                else if (op == "del")
                    this.Settings.Prefix = "devi:";
                else rs ^= 1;
            }
            else rs ^= 2;

            if (rs == 3)
                await this.SendEmbedAsync(ctx, BuildEmbed("Success", "Setting changed successfully", 2));
            else if (rs == 2)
                await this.SendEmbedAsync(ctx, BuildEmbed("Failure", "Invalid operation", 1));
            else if (rs == 1)
                await this.SendEmbedAsync(ctx, BuildEmbed("Failure", "Invalid setting", 1));
        }

        [Command("save")]
        public async Task Save(CommandContext ctx)
        {
            var json = JsonConvert.SerializeObject(this.Settings, Formatting.None);
            var l = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            l = Path.Combine(l, "devi.json");
            File.WriteAllText(l, json, new UTF8Encoding(false));

            await this.SendTextAsync(ctx, "All settings saved");
        }

        public async Task<ScriptState<object>> EvaluateAsync(CommandContext ctx, string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
                throw new ArgumentException("You need to wrap the code into a code block.");

            var cs = code.Substring(cs1, cs2 - cs1);

            var nmsg = await this.SendEmbedAsync(ctx, BuildEmbed("Evaluating...", null, 0));

            try
            {
                var globals = new DeviVariables()
                {
                    Message = ctx.Message,
                    Client = ctx.Client
                };

                var sopts = ScriptOptions.Default;
                sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext");
                sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                var script = CSharpScript.Create(cs, sopts, typeof(DeviVariables));
                script.Compile();
                var result = await script.RunAsync(globals);

                return result;
            }
            catch (Exception ex)
            {
                await this.SendEmbedAsync(ctx, BuildEmbed("Evaluation Failure", string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message), 1));
            }

            return null;
        }

        public string ObjectToString(object o)
        {
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

        private async Task QuoteAsync(CommandContext ctx, DiscordMessage msg, string qmsg)
        {
            var txt = qmsg ?? this.EmojiMap.Mapping["speech_balloon"];
            txt = txt.Trim();

            var embed = (DiscordEmbed)null;
            if (msg != null)
            {
                embed = BuildQuoteEmbed(msg, ctx);
            }
            else
                embed = BuildEmbed("Failed to quote message", null, 1);

            await this.SendEmbedAsync(ctx, embed, txt);
        }

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

        private static DiscordEmbed BuildQuoteEmbed(DiscordMessage msq, CommandContext ctx)
        {
            var author = msq.Author;
            var author1 = ctx.Guild.GetMemberAsync(author.Id).GetAwaiter().GetResult();

            var color = (int?)null;
            var roles = author1.Roles.OrderByDescending(xr => xr.Position);
            var role = roles.FirstOrDefault(xr => xr.Color != 0);
            color = role != null ? (int?)role.Color : null;

            var embed = BuildEmbed(null, msq.Content, 0);
            embed.Color = color != null ? color.Value : 0;
            embed.Timestamp = msq.Timestamp;
            embed.Author = new DiscordEmbedAuthor()
            {
                IconUrl = author.AvatarUrl,
                Name = author1 != null ? author1.Nickname ?? author.Username : author.Username
            };

            var att = msq.Attachments.FirstOrDefault();
            if (att != null)
            {
                embed.Fields = new List<DiscordEmbedField>
                {
                    new DiscordEmbedField
                    {
                        Inline = false,
                        Name = "Attachment",
                        Value = string.Concat("[Link](", att.Url, ")")
                    }
                };

                if (att.Width != 0 && att.Height != 0)
                    embed.Image = new DiscordEmbedImage { Url = att.Url };
            }

            return embed;
        }
    }
}
