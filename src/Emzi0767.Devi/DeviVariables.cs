using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Emzi0767.Devi
{
    public class DeviVariables
    {
        public DiscordMessage Message { get { return this.Context.Message; } }
        public DiscordChannel Channel { get { return this.Message.Channel; } }
        public DiscordGuild Guild { get { return this.Channel.Guild; } }
        public DiscordUser User { get { return this.Message.Author; } }

        public DiscordClient Client { get { return this.Context.Client; } }

        public CommandContext Context { get; }

        public DeviVariables(CommandContext ctx)
        {
            this.Context = ctx;
        }
    }
}
