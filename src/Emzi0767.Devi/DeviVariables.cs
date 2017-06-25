using DSharpPlus;

namespace Emzi0767.Devi
{
    public class DeviVariables
    {
        public DiscordMessage Message { get; set; }
        public DiscordChannel Channel { get { return this.Message.Channel; } }
        public DiscordGuild Guild { get { return this.Channel.Guild; } }
        public DiscordUser User { get { return this.Message.Author; } }

        public DiscordClient Client { get; set; }
    }
}
