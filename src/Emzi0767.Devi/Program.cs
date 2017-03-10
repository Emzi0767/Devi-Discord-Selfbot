using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Emzi0767.Devi.Services;
using Newtonsoft.Json;

// DEvI: Dynamic Evaluation Implement

namespace Emzi0767.Devi
{
    internal static class Program
    {
        #region Discord Client
        private static DiscordSocketClient DeviClient { get; set; }
        private static CommandService DeviCommands { get; set; }
        private static DependencyMap DeviDependencies { get; set; }
        #endregion

        #region Settings and configuration
        private static DeviSettingStore Settings { get; set; }
        private static DeviEmojiMap EmojiMap { get; set; }
        private static DeviDongerMap Dongers { get; set; }
        private static DeviGuildEmojiMap GuildEmoji { get; set; }
        #endregion

        #region Tracking and temporary storage
        private static List<SocketUserMessage> DeviMessageTracker { get; set; }
        #endregion

        internal static void Main(string[] args)
        {
            Order66().GetAwaiter().GetResult();
        }

        private static async Task Order66()
        {
            var l = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            GuildEmoji = new DeviGuildEmojiMap(new Dictionary<string, string>());

            var stx = Path.Combine(l, "devi.json");
            var emx = Path.Combine(l, "emoji.json");
            var dgx = Path.Combine(l, "donger.json");

            if (File.Exists(stx) && File.Exists(emx) && File.Exists(dgx))
            {
                stx = File.ReadAllText(stx, new UTF8Encoding(false));
                Settings = JsonConvert.DeserializeObject<DeviSettingStore>(stx);
            }
            else
            {
                throw new FileNotFoundException("Unable to load configuration file (devi.json)!");
            }

            if (File.Exists(emx))
            {
                emx = File.ReadAllText(emx, new UTF8Encoding(false));
                var edict = JsonConvert.DeserializeObject<Dictionary<string, string>>(emx);
                EmojiMap = new DeviEmojiMap(edict);
            }
            else
            {
                EmojiMap = new DeviEmojiMap(new Dictionary<string, string>());
            }

            if (File.Exists(dgx))
            {
                dgx = File.ReadAllText(dgx, new UTF8Encoding(false));
                Dongers = JsonConvert.DeserializeObject<DeviDongerMap>(dgx);
                Dongers.HookAliases();
            }
            else
            {
                Dongers = new DeviDongerMap();
                Dongers.Dongers = new Dictionary<string, string>();
                Dongers.Aliases = new Dictionary<string, List<string>>();
            }

            var depmap = new DependencyMap();
            depmap.Add(Settings);
            depmap.Add(EmojiMap);
            depmap.Add(Dongers);
            depmap.Add(GuildEmoji);
            DeviDependencies = depmap;

            var discord = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Verbose });
            DeviClient = discord;

            var commands = new CommandService();
            DeviCommands = commands;

            DeviMessageTracker = new List<SocketUserMessage>();

            discord.GuildAvailable += Discord_GuildAvailable;
            discord.MessageReceived += Discord_MessageReceived;
            discord.Ready += Discord_Ready;
            discord.Log += Discord_Log;
            discord.ReactionAdded += Discord_ReactionAdded;

            await commands.AddModuleAsync<DeviCommandModule>();

            await discord.LoginAsync(TokenType.User, Settings.Token);
            await discord.StartAsync();

            await Task.Delay(-1);
        }

        private static Task Discord_GuildAvailable(SocketGuild arg)
        {
            var emoji = arg.Emojis;
            if (emoji == null) return Task.CompletedTask;
            foreach (var e in emoji)
            {
                var es = string.Concat("<:X:", e.Id, ">");
                GuildEmoji.Mapping[e.Name.ToLower()] = es;
            }
            return Task.CompletedTask;
        }

        private static async Task Discord_ReactionAdded(ulong arg1, Optional<SocketUserMessage> arg2, SocketReaction arg3)
        {
            var chn = arg3.Channel as SocketTextChannel;
            if (chn == null || chn.Guild == null)
                return;
            
            if (!arg3.Message.IsSpecified)
                return;

            var msg = DeviMessageTracker.FirstOrDefault(xmsg => xmsg.Id == arg3.MessageId);
            if (msg == null)
                return;

            if (msg.Author.Id != DeviClient.CurrentUser.Id)
                return;

            if (arg3.UserId != msg.Author.Id)
                return;

            if (arg3.Emoji.Name == EmojiMap.Mapping["x"])
                await msg.DeleteAsync();
        }

        private static Task Discord_Log(LogMessage arg)
        {
            Console.WriteLine(string.Concat("[", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"), "] [", arg.Severity, "] ", arg.Message));
            return Task.CompletedTask;
        }

        private static Task Discord_Ready()
        {
            var timer1 = new Timer(new TimerCallback(DeviTimerCallback), null, 5000, 300000);
            var timer2 = new Timer(new TimerCallback(DeviSettingsCallback), null, 5000, 1800000);
            return Task.CompletedTask;
        }

        private static async Task Discord_MessageReceived(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null)
                return;

            var chn = msg.Channel as SocketTextChannel;
            if (chn == null || chn.Guild == null)
                return;
            
            if (msg.Author.Id != DeviClient.CurrentUser.Id)
                return;

            if (msg.Author.Id == DeviClient.CurrentUser.Id)
                DeviMessageTracker.Add(msg);
            int apos = 0;
            if (!msg.HasStringPrefix(Settings.Prefix, ref apos))
                return;

            DeviMessageTracker.Add(msg);
            await Task.Delay(DeviClient.Latency);

            var ctx = new CommandContext(DeviClient, msg);
            var res = await DeviCommands.ExecuteAsync(ctx, apos, DeviDependencies);
            if (!res.IsSuccess)
            {
                var embed = new EmbedBuilder();
                embed.Color = new Color(255, 127, 0);
                embed.ThumbnailUrl = "http://i.imgur.com/F9HGvxs.jpg";
                embed.Title = "Evaluation error";
                embed.Description = res.ErrorReason;
                
                await SendEmbedAsync(embed, msg);
            }
        }

        private static void DeviTimerCallback(object _)
        {
            var delmg = new List<SocketUserMessage>();
            foreach (var msg in DeviMessageTracker)
                if (msg.CreatedAt.AddMinutes(30).ToLocalTime() < DateTimeOffset.Now)
                    delmg.Add(msg);
            foreach (var msg in delmg)
                DeviMessageTracker.Remove(msg);
        }

        private static void DeviSettingsCallback(object _)
        {
            var json = JsonConvert.SerializeObject(Settings, Formatting.None);
            var l = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            l = Path.Combine(l, "devi.json");
            File.WriteAllText(l, json, new UTF8Encoding(false));
        }

        #region T/E
        private static async Task<IUserMessage> SendTextAsync(string content, IUserMessage nmsg)
        {
            var msg = nmsg;
            var mod = msg.Author.Id == DeviClient.CurrentUser.Id;

            if (mod)
                await msg.ModifyAsync(x => x.Content = content);
            else
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content));

            return msg;
        }

        private static Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed, IUserMessage nmsg)
        {
            return SendEmbedAsync(embed, null, nmsg);
        }

        private static async Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed, string content, IUserMessage nmsg)
        {
            var msg = nmsg;
            var mod = msg.Author.Id == DeviClient.CurrentUser.Id;

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
        #endregion
    }
}
