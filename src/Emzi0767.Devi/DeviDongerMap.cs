using System.Collections.Generic;
using Newtonsoft.Json;

namespace Emzi0767.Devi
{
    public class DeviDongerMap
    {
        [JsonProperty("dong")]
        public Dictionary<string, string> Dongers { get; set; }

        [JsonProperty("alias")]
        public Dictionary<string, List<string>> Aliases { get; set; }

        public void HookAliases()
        {
            foreach (var ag in this.Aliases)
            {
                var dong = this.Dongers[ag.Key];
                foreach (var a in ag.Value)
                    this.Dongers.Add(a, dong);
            }
        }
    }
}