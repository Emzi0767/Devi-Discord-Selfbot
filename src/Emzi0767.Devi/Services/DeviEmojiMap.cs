using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Emzi0767.Devi.Services
{
    public sealed class DeviEmojiMap
    {
        public IReadOnlyDictionary<string, string> Mapping { get; private set; }
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> ReverseMapping { get; private set; }

        public DeviEmojiMap(IDictionary<string, string> map)
        {
            this.Mapping = new ReadOnlyDictionary<string, string>(map);

            var rmap = map.GroupBy(xkvp => xkvp.Value, xkvp => xkvp.Key)
                    .ToDictionary(xg => xg.Key, xg => new ReadOnlyCollection<string>(xg.ToList()) as IReadOnlyCollection<string>);
            this.ReverseMapping = new ReadOnlyDictionary<string, IReadOnlyCollection<string>>(rmap);
        }
    }
}
