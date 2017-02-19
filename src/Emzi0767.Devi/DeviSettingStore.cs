using Newtonsoft.Json;

namespace Emzi0767.Devi
{
    public class DeviSettingStore
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("prefix")]
        public string Prefix { get; set; }
    }
}
