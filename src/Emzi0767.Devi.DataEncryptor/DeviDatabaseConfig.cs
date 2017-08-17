using Newtonsoft.Json;

namespace Emzi0767.Devi
{
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
