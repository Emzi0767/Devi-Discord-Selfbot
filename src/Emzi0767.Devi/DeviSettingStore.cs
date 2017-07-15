using Newtonsoft.Json;

namespace Emzi0767.Devi
{
    public class DeviSettingStore
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("cache_size")]
        public int CacheSize { get; set; }

        [JsonProperty("database_settings")]
        public DeviDatabaseSettings DatabaseSettings { get; set; }
    }

    public class DeviDatabaseSettings
    {
        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("database")]
        public string Database { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("table_prefix")]
        public string TablePrefix { get; set; }
    }
}
