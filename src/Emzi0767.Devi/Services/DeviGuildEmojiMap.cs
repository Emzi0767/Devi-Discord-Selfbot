using System.Collections.Generic;

namespace Emzi0767.Devi.Services
{
    public class DeviGuildEmojiMap
    {
        public IDictionary<string, string> Mapping { get; private set; }

        public DeviGuildEmojiMap(IDictionary<string, string> map)
        {
            this.Mapping = map;
        }
    }
}
