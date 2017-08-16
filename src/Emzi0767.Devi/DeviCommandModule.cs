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
        private DeviUtilities Utilities { get; }

        public DeviCommandModule(DeviSettingStore settings, DeviEmojiMap emoji_map, DeviDongerMap donger_map, DeviGuildEmojiMap guild_emoji, DeviUtilities utils)
        {
            this.Settings = settings;
            this.EmojiMap = emoji_map;
            this.Dongers = donger_map;
            this.GuildEmoji = guild_emoji;
            this.Utilities = utils;
        }

        [Command("random"), Description("Generates a random number between *min* and *max*.")]
        public async Task GenRandomAsync(CommandContext ctx, int min, int max)
        {
            var msg = ctx.Message;

            var rnd = new Random();
            var num = rnd.Next(min, max);

            await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Random Number", num.ToString("#,##0"), 0).Build());
        }

        [Command("eval"), Description("Evaluates C# code.")]
        public async Task EvalAsync(CommandContext ctx, [RemainingText] string code)
        {
            try
            {
                var result = await EvaluateAsync(ctx, code);

                if (result != null && result.ReturnValue != null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Evaluation Result", result.ReturnValue.ToString(), 2).Build());
                else
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Evaluation Successful", "No result was returned.", 2).Build());
            }
            catch (Exception ex)
            {
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Evaluation Failure", string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message), 1).Build());
            }
        }

        [Command("inspect"), Description("Evaluates a snippet of code, and inspects the result.")]
        public async Task InspectAsync(CommandContext ctx, [RemainingText] string code)
        {
            try
            {
                var result = await EvaluateAsync(ctx, code);

                if (result != null && result.ReturnValue != null)
                {
                    var t = result.ReturnValue.GetType();
                    var ti = t.GetTypeInfo();
                    var embed = this.Utilities.BuildEmbed("Return value inspection", string.Concat("Return type:", t.ToString()), 2);
                    
                    if (ti.IsPrimitive || ti.IsEnum || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan))
                        embed.AddField("Value", this.Utilities.ObjectToString(result.ReturnValue), false);
                    else
                    {
                        var rv = result.ReturnValue;
                        var psr = ti.GetProperties();
                        var ps = psr.Take(25);
                        foreach (var xps in ps)
                            embed.AddField(string.Concat(xps.Name, " (", xps.PropertyType.ToString(), ")"), this.Utilities.ObjectToString(xps.GetValue(rv)), true);

                        if (psr.Length > 25)
                            embed.Description = string.Concat(embed.Description, "\n\n**Warning**: Property count exceeds 25. Not all properties are displayed.");
                    }

                    await this.Utilities.SendEmbedAsync(ctx, embed.Build());
                }
                else
                    await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Inspection Failed", "No result was returned.", 1).Build());
            }
            catch (Exception ex)
            {
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Inspection Failed", string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message), 1).Build());
            }
        }

        [Command("nitro")]
        public async Task NitroAsync(CommandContext ctx, ulong guild, ulong channel)
        {
            var cln = ctx.Client;
            var gld = cln.Guilds.FirstOrDefault(xg => xg.Key == guild);
            var chn = gld.Value.Channels.FirstOrDefault(xc => xc.Id == channel);

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(5267072),
                Url = "https://discordapp.com/nitro",
                Description = "**Discord Nitro** is required to view this message.",
                ThumbnailUrl = "http://i.imgur.com/1dH8EJa.png"
            };
            embed.WithAuthor("Discord Nitro Message", "https://cdn.discordapp.com/emojis/261735650192130049.png");

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("quote")]
        public async Task QuoteAsync(CommandContext ctx, ulong msqid, [RemainingText] string message = null)
        {
            var msg = ctx.Message;
            var chn = msg.Channel;
            var mss = await chn.GetMessagesAsync(around: msqid, limit: 3);
            var msq = mss.FirstOrDefault(xm => xm.Id == msqid);

            await this.Utilities.QuoteAsync(ctx, msq, message);
        }

        [Command("quoteuser")]
        public async Task QuoteAsync(CommandContext ctx, DiscordMember user, [RemainingText] string message = null)
        {
            var msg = ctx.Message;
            var chn = msg.Channel;
            var mss = await chn.GetMessagesAsync(limit: 100);
            var msq = mss.OrderBy(xmsg => xmsg.Timestamp).LastOrDefault(xmsg => xmsg.Author != null && xmsg.Author.Id == user.Id);

            await this.Utilities.QuoteAsync(ctx, msq, message);
        }

        [Command("emoji")]
        public async Task EmojiAsync(CommandContext ctx, string emoji)
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

                    await this.Utilities.SendTextAsync(ctx, einf);
                }
                else
                {
                    await this.Utilities.SendTextAsync(ctx, string.Concat(this.EmojiMap.Mapping["poop"], " (this is an error)"));
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

                    await this.Utilities.SendTextAsync(ctx, einf);
                }
                else
                {
                    await this.Utilities.SendTextAsync(ctx, string.Concat("Emoji: ", emoji, " (`", emoji, "`)"));
                }
            }
        }

        [Command("dong")]
        public async Task DongAsync(CommandContext ctx, string dong)
        {
            await this.Utilities.SendTextAsync(ctx, this.Dongers.Dongers[dong]);
        }

        [Command("imply")]
        public async Task ImplyAsync(CommandContext ctx, params string[] implication_content)
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
            await this.Utilities.SendTextAsync(ctx, string.Concat("<:Implying:261288929628651540> ", impl));
        }

        [Command("ping")]
        public async Task PingAsync(CommandContext ctx)
        {
            var client = ctx.Client;

            var sw = new Stopwatch();
            sw.Start();
            var msg = await this.Utilities.SendTextAsync(ctx, "Performing pings...");
            sw.Stop();

            await this.Utilities.SendTextAsync(ctx, string.Concat("**Socket latency**: ", client.Ping.ToString("#,##0"), "ms\n**API latency**: ", sw.ElapsedMilliseconds.ToString("#,##0"), "ms"));
        }

        [Command("settings")]
        public async Task SettingsControlAsync(CommandContext ctx, string setting, string operation, string value)
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
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Success", "Setting changed successfully", 2).Build());
            else if (rs == 2)
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Failure", "Invalid operation", 1).Build());
            else if (rs == 1)
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Failure", "Invalid setting", 1).Build());
        }

        [Command("save")]
        public async Task SaveAsync(CommandContext ctx)
        {
            var json = JsonConvert.SerializeObject(this.Settings, Formatting.None);
            var l = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            l = Path.Combine(l, "devi.json");
            File.WriteAllText(l, json, new UTF8Encoding(false));

            await this.Utilities.SendTextAsync(ctx, "All settings saved");
        }

        public async Task<ScriptState<object>> EvaluateAsync(CommandContext ctx, string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
                throw new ArgumentException("You need to wrap the code into a code block.");

            var cs = code.Substring(cs1, cs2 - cs1);

            var nmsg = await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Evaluating...", null, 0).Build());

            var globals = new DeviVariables(ctx);

            var sopts = ScriptOptions.Default;
            sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext");
            sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var script = CSharpScript.Create(cs, sopts, typeof(DeviVariables));
            script.Compile();
            var result = await script.RunAsync(globals);

            return result;
        }
    }
}
