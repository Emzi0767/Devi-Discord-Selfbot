using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Emzi0767.Devi.Services.Data;
using Newtonsoft.Json;

namespace Emzi0767.Devi.Services
{
    public class NanopoolApiClient
    {
        private HttpClient Http { get; }

        public NanopoolApiClient(HttpClient http)
        {
            this.Http = http;
        }

        public async Task<NanopoolApiResponse> GetBalanceAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException("Address cannot be null.", nameof(address));
            
            if (!address.StartsWith("0x") || address.Length != 42)
                throw new ArgumentException("Address is malformed.", nameof(address));
            
            var url = string.Concat("https://api.nanopool.org/v1/eth/balance/", address);
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var res = await this.Http.SendAsync(req);
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Nanopool API returned an error.");
                
                var resd = JsonConvert.DeserializeObject<NanopoolApiResponse>(await res.Content.ReadAsStringAsync());
                return resd;
            }
        }

        public async Task<NanopoolApiResponse> GetStatusAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException("Address cannot be null.", nameof(address));
            
            if (!address.StartsWith("0x") || address.Length != 42)
                throw new ArgumentException("Address is malformed.", nameof(address));
            
            var url = string.Concat("https://api.nanopool.org/v1/eth/user/", address);
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var res = await this.Http.SendAsync(req);
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Nanopool API returned an error.");
                
                var resd = JsonConvert.DeserializeObject<NanopoolApiResponse>(await res.Content.ReadAsStringAsync());
                return resd;
            }
        }
    }
}