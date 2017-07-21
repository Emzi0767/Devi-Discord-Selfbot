using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Emzi0767.Devi.Services.Data;
using Newtonsoft.Json;

namespace Emzi0767.Devi.Services
{
    public class CryptonatorApiClient
    {
        public static IReadOnlyDictionary<string, CryptoCurrency> CryptoCurrencies { get; }
        public static IReadOnlyDictionary<string, FiatCurrency> FiatCurrencies { get; }
        public static IReadOnlyDictionary<string, ICurrency> AllCurrencies { get; }

        private HttpClient Http { get; }

        public CryptonatorApiClient(HttpClient http)
        {
            this.Http = http;
        }

        public async Task<CryptonatorApiResponse> GetConversionRateAsync(ICurrency primary, ICurrency secondary)
        {
            if (primary == null || secondary == null)
                throw new ArgumentNullException("You cannot specify null as currency.");

            if ((primary.Type & CurrencyType.Primary) != CurrencyType.Primary)
                throw new ArgumentException("Specified primary currency is not usable as primary currency.", nameof(primary));
            
            if ((secondary.Type & CurrencyType.Secondary) != CurrencyType.Secondary)
                throw new ArgumentException("Specified secondary currency is not usable as secondary currency.", nameof(primary));
            
            var url = string.Concat("https://api.cryptonator.com/api/full/", primary.Code.ToLower(), "-", secondary.Code.ToLower());
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var res = await this.Http.SendAsync(req);
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Cryptonator API returned an error.");
                
                var resd = JsonConvert.DeserializeObject<CryptonatorApiResponse>(await res.Content.ReadAsStringAsync());
                return resd;
            }
        }

        static CryptonatorApiClient()
        {
            CryptoCurrencies = new ReadOnlyDictionary<string, CryptoCurrency>(new Dictionary<string, CryptoCurrency>()
            {
                ["BTC"] = new CryptoCurrency("Bitcoin", "BTC", CurrencyType.Primary | CurrencyType.Secondary),
                ["ETH"] = new CryptoCurrency("Ethereum", "ETH", CurrencyType.Primary | CurrencyType.Secondary),
                ["ETC"] = new CryptoCurrency("Ethereum Classic", "ETC", CurrencyType.Primary),
                ["ZEC"] = new CryptoCurrency("ZCash", "ZEC", CurrencyType.Primary),
                ["XMR"] = new CryptoCurrency("Monero", "XMR", CurrencyType.Primary | CurrencyType.Secondary),
                ["LTC"] = new CryptoCurrency("Litecoin", "LTC", CurrencyType.Primary | CurrencyType.Secondary),
                ["DOGE"] = new CryptoCurrency("Dogecoin", "DOGE", CurrencyType.Primary | CurrencyType.Secondary),
                ["DASH"] = new CryptoCurrency("Dash", "DASH", CurrencyType.Primary | CurrencyType.Secondary)
            });

            FiatCurrencies = new ReadOnlyDictionary<string, FiatCurrency>(new Dictionary<string, FiatCurrency>()
            {
                ["USD"] = new FiatCurrency("US Dollar", "USD", CurrencyType.Primary | CurrencyType.Secondary),
                ["EUR"] = new FiatCurrency("Euro", "EUR", CurrencyType.Primary | CurrencyType.Secondary),
                ["PLN"] = new FiatCurrency("Polish Złoty", "PLN", CurrencyType.Primary | CurrencyType.Secondary),
                ["CAD"] = new FiatCurrency("Canadian Dollar", "CAD", CurrencyType.Secondary),
                ["GBP"] = new FiatCurrency("British Pound Sterling", "GBP", CurrencyType.Secondary)
            });

            AllCurrencies = new ReadOnlyDictionary<string, ICurrency>(CryptoCurrencies.Values
                .OfType<ICurrency>()
                .Concat(FiatCurrencies.Values.OfType<ICurrency>())
                .ToDictionary(xc => xc.Code, xc => xc));
        }
    }
}