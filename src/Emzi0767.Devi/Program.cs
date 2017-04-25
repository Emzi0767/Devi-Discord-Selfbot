using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Emzi0767.Devi.Services;
using Newtonsoft.Json;

// DEvI: Dynamic Evaluation Implement

namespace Emzi0767.Devi
{
    internal static class Program
    {
        #region Discord Client
        private static DiscordClient DeviClient { get; set; }
        private static CommandsNextModule DeviCommands { get; set; }
        #endregion

        #region Settings and configuration
        internal static DeviSettingStore Settings { get; set; }
        internal static DeviEmojiMap EmojiMap { get; set; }
        internal static DeviDongerMap Dongers { get; set; }
        internal static DeviGuildEmojiMap GuildEmoji { get; set; }
        #endregion

        #region Tracking and temporary storage
        private static List<DiscordMessage> DeviMessageTracker { get; set; }
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
                Dongers = new DeviDongerMap()
                {
                    Dongers = new Dictionary<string, string>(),
                    Aliases = new Dictionary<string, List<string>>()
                };
            }

            var discord = new DiscordClient(new DiscordConfig() { LogLevel = LogLevel.Debug, Token = Settings.Token, TokenType = TokenType.User });
            DeviClient = discord;

#if NET47
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                DeviClient.SetSocketImplementation<WebSocketSharpClient>();
#endif

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration { Prefix = Settings.Prefix, SelfBot = true, EnableDefaultHelp = false, EnableMentionPrefix = false });
            DeviCommands = commands;
            DeviCommands.RegisterCommands<DeviCommandModule>();

            DeviMessageTracker = new List<DiscordMessage>();

            discord.GuildAvailable += Discord_GuildAvailable;
            discord.MessageCreated += Discord_MessageReceived;
            discord.Ready += Discord_Ready;
            discord.DebugLogger.LogMessageReceived += Discord_Log;
            discord.MessageReactionAdd += Discord_ReactionAdded;
            
            await discord.ConnectAsync();

            await Task.Delay(-1);
        }

        private static Task Discord_GuildAvailable(GuildCreateEventArgs ea)
        {
            var arg = ea.Guild;
            var emoji = arg.Emojis;
            if (emoji == null) return Task.CompletedTask;
            foreach (var e in emoji)
            {
                var es = string.Concat("<:X:", e.Id, ">");
                GuildEmoji.Mapping[e.Name.ToLower()] = es;
            }
            return Task.CompletedTask;
        }

        //private static async Task Discord_ReactionAdded(ulong arg1, Optional<SocketUserMessage> arg2, SocketReaction arg3)
        private static async Task Discord_ReactionAdded(MessageReactionAddEventArgs ea)
        {
            var arg1 = ea.MessageID;
            var arg2 = ea.Channel;

            var chn = arg2;
            if (chn == null)
                return;

            var msg = DeviMessageTracker.FirstOrDefault(xmsg => xmsg.Id == arg1);
            if (msg == null)
                return;

            if (msg.Author.Id != DeviClient.Me.Id)
                return;

            if (ea.UserID != msg.Author.Id)
                return;

            if (ea.Emoji.Name == EmojiMap.Mapping["x"])
                await msg.DeleteAsync();
        }

        private static void Discord_Log(object sender, DebugLogMessageEventArgs arg)
        {
            Console.WriteLine(string.Concat("[", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"), "] [", arg.Level, "] ", arg.Message));
        }

        private static Task Discord_Ready(ReadyEventArgs ea)
        {
            var timer1 = new Timer(new TimerCallback(DeviTimerCallback), null, 5000, 300000);
            var timer2 = new Timer(new TimerCallback(DeviSettingsCallback), null, 5000, 1800000);
            return Task.CompletedTask;
        }

        private static async Task Discord_MessageReceived(MessageCreateEventArgs ea)
        {
            var arg = ea.Message;
            var msg = arg;
            if (msg == null)
                return;

            var chn = msg.Channel;
            if (chn == null || chn.Guild == null)
                return;
            
            if (msg.Author.Id != DeviClient.Me.Id)
                return;

            if (msg.Author.Id == DeviClient.Me.Id)
                DeviMessageTracker.Add(msg);

            DeviMessageTracker.Add(msg);
            await Task.Delay(DeviClient.Ping);
        }

        private static void DeviTimerCallback(object _)
        {
            var delmg = new List<DiscordMessage>();
            foreach (var msg in DeviMessageTracker)
                if (msg.CreationDate.AddMinutes(30).ToLocalTime() < DateTimeOffset.Now)
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
        private static async Task<DiscordMessage> SendTextAsync(string content, DiscordMessage nmsg)
        {
            var msg = nmsg;
            var mod = msg.Author.Id == DeviClient.Me.Id;

            if (mod)
                await msg.EditAsync(content);
            else
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content));

            return msg;
        }

        private static Task<DiscordMessage> SendEmbedAsync(DiscordEmbed embed, DiscordMessage nmsg)
        {
            return SendEmbedAsync(embed, null, nmsg);
        }

        private static async Task<DiscordMessage> SendEmbedAsync(DiscordEmbed embed, string content, DiscordMessage nmsg)
        {
            var msg = nmsg;
            var mod = msg.Author.Id == DeviClient.Me.Id;

            if (mod)
                await msg.EditAsync(!string.IsNullOrWhiteSpace(content) ? content : msg.Content, embed);
            else if (!string.IsNullOrWhiteSpace(content))
                msg = await msg.Channel.SendMessageAsync(string.Concat(msg.Author.Mention, ": ", content), false, embed);
            else
                msg = await msg.Channel.SendMessageAsync(msg.Author.Mention, false, embed);

            return msg;
        }
        #endregion
    }
}
