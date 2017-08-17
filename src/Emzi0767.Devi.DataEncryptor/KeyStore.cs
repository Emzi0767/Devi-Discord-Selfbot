using System.Collections.Generic;
using Newtonsoft.Json;

namespace Emzi0767.Devi.Crypto
{
    internal struct KeyStore
    {
        [JsonProperty("xor_key")]
        public byte XorKey { get; set; }

        [JsonProperty("keys")]
        public Dictionary<string, string> RawKeys { get; set; }
    }
}
