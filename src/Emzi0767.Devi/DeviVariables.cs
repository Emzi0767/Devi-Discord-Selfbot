using Discord.WebSocket;

namespace Emzi0767.Devi
{
    public class DeviVariables
    {
        public SocketUserMessage Message { get; set; }
        public SocketTextChannel Channel { get { return this.Message.Channel as SocketTextChannel; } }
        public SocketGuild Guild { get { return this.Channel.Guild; } }

        public DiscordSocketClient Client { get { return this.Message.Discord; } }
    }
}
