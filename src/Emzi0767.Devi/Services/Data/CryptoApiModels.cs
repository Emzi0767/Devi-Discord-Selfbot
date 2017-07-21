using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emzi0767.Devi.Services.Data
{
    public interface ICurrency
    {
        string Name { get; }
        string Code { get; }
        CurrencyType Type { get; }
    }

    public struct CryptoCurrency : ICurrency
    {
        public string Name { get; }
        public string Code { get; }
        public CurrencyType Type { get; }

        public CryptoCurrency(string name, string code, CurrencyType type)
        {
            this.Name = name;
            this.Code = code;
            this.Type = type;
        }
    }

    public struct FiatCurrency : ICurrency
    {
        public string Name { get; }
        public string Code { get; }
        public CurrencyType Type { get; }

        public FiatCurrency(string name, string code, CurrencyType type)
        {
            this.Name = name;
            this.Code = code;
            this.Type = type;
        }
    }

    public enum CurrencyType : int
    {
        None = 0,
        Primary = 1,
        Secondary = 2
    }

    public struct CryptonatorApiResponse
    {
        [JsonProperty("timestamp")]
        private long TimestampRaw { get; set; }

        [JsonIgnore]
        public DateTimeOffset Timestamp 
        {
            get { return DateTimeOffset.FromUnixTimeSeconds(this.TimestampRaw); }
        }

        [JsonProperty("success")]
        public bool IsSuccessful { get; private set; }

        [JsonProperty("error")]
        public string ErrorMessage { get; private set; }

        [JsonProperty("ticker")]
        public CryptonatorTicker Ticker { get; private set; }
    }

    public struct CryptonatorTicker
    {
        [JsonProperty("base")]
        private string BaseRaw { get; set; }

        [JsonIgnore]
        public ICurrency BaseCurrency 
        {
            get { return CryptonatorApiClient.AllCurrencies[this.BaseRaw]; }
        }

        [JsonProperty("target")]
        private string TargetRaw { get; set; }

        [JsonIgnore]
        public ICurrency TargetCurrency 
        {
            get { return CryptonatorApiClient.AllCurrencies[this.TargetRaw]; }
        }

        [JsonProperty("price")]
        public decimal Price { get; private set; }

        [JsonProperty("volume")]
        public decimal? ExchangedVolume { get; private set; }

        [JsonProperty("change")]
        public decimal PriceChangePercent { get; private set; }

        [JsonProperty("markets")]
        public IReadOnlyList<CryptonatorMarket> Markets { get; private set; }
    }

    public struct CryptonatorMarket
    {
        [JsonProperty("market")]
        public string Name { get; private set; }

        [JsonProperty("price")]
        public decimal Price { get; private set; }

        [JsonProperty("volume")]
        public decimal ExchangedVolume { get; private set; }
    }

    public struct NanopoolApiResponse
    {
        [JsonProperty("status")]
        public bool IsSuccessful { get; private set; }

        [JsonProperty("error")]
        public string ErrorMessage { get; private set; }

        [JsonProperty("data")]
        public JToken Data { get; private set; }

        public T GetData<T>()
        {
            return this.Data.ToObject<T>();
        }
    }

    public struct NanopoolGeneralData
    {
        [JsonProperty("balance")]
        public decimal Balance { get; private set; }

        [JsonProperty("unconfirmed_balance")]
        public decimal UnconfirmedBalance { get; private set; }

        [JsonProperty("hashrate")]
        public float CurrentHashrate { get; private set; }

        [JsonProperty("avghashrate")]
        public NanopoolAverageHashrate AverageHashrate { get; private set; }

        [JsonProperty("workers")]
        public IReadOnlyList<NanopoolWorker> Workers { get; private set; }
    }

    public struct NanopoolAverageHashrate
    {
        [JsonProperty("h1")]
        public float Average1H { get; private set; }

        [JsonProperty("h3")]
        public float Average3H { get; private set; }

        [JsonProperty("h6")]
        public float Average6H { get; private set; }

        [JsonProperty("h12")]
        public float Average12H { get; private set; }

        [JsonProperty("h24")]
        public float Average24H { get; private set; }
    }

    public struct NanopoolWorker
    {
        [JsonProperty("id")]
        public string Id { get; private set; }

        [JsonProperty("hashrate")]
        public float Hashrate { get; private set; }

        [JsonProperty("lastShare")]
        public long LastShareRaw { get; private set; }

        [JsonIgnore]
        public DateTimeOffset LastShare 
        {
            get { return DateTimeOffset.FromUnixTimeSeconds(this.LastShareRaw); }
        }
        
        [JsonProperty("avg_h1")]
        public float HashrateAverage1H { get; private set; }

        [JsonProperty("avg_h3")]
        public float HashrateAverage3H { get; private set; }

        [JsonProperty("avg_h6")]
        public float HashrateAverage6H { get; private set; }

        [JsonProperty("avg_h12")]
        public float HashrateAverage12H { get; private set; }

        [JsonProperty("avg_h24")]
        public float HashrateAverage24H { get; private set; }
    }
}