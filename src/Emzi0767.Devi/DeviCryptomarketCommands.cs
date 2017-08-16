using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.Devi.Services;
using Emzi0767.Devi.Services.Data;

namespace Emzi0767.Devi
{
    [Group("crypto")]
    public class DeviCryptomarketCommands
    {
        private DeviUtilities Utilities { get; }
        private CryptonatorApiClient Cryptonator { get; }
        private NanopoolApiClient Nanopool { get; }
        private DeviCryptoSettings Settings { get; }

        public DeviCryptomarketCommands(DeviUtilities utils, CryptonatorApiClient cryptonator, NanopoolApiClient nanopool, DeviCryptoSettings settings)
        {
            this.Utilities = utils;
            this.Cryptonator = cryptonator;
            this.Nanopool = nanopool;
            this.Settings = settings;
        }

        [Command("convert"), Aliases("conv", "cv")]
        public async Task ConvertAsync(CommandContext ctx, ICurrency primary, ICurrency secondary = null, decimal amount = 1)
        {
            try
            {
                secondary = secondary ?? this.Settings.DefaultTargetCurrency;
                var resp = await this.Cryptonator.GetConversionRateAsync(primary, secondary);
                if (!resp.IsSuccessful)
                    throw new Exception(resp.ErrorMessage);

                var ticker = resp.Ticker;
                var kraken = ticker.Markets.FirstOrDefault(xm => xm.Name.ToLower() == "kraken");

                var sb = new StringBuilder();
                sb.AppendFormat("{0:#,##0.000000} {1} = {2:#,##0.000000} {3} ({4}{5:#,##0.00}%)", amount, Formatter.Bold(primary.Code), amount * ticker.Price, Formatter.Bold(secondary.Code), ticker.PriceChangePercent >= 0 ? "+" : "", ticker.PriceChangePercent);

                var embed = this.Utilities.BuildEmbed(string.Concat("Exchange rate for ", primary.Name, "/", secondary.Name), sb.ToString(), 0);
                if (!string.IsNullOrWhiteSpace(kraken.Name))
                {
                    sb = new StringBuilder();
                    sb.AppendFormat("{0:#,##0.000000} {1} = {2:#,##0.000000} {3}", amount, Formatter.Bold(primary.Code), amount * kraken.Price, Formatter.Bold(secondary.Code));

                    embed.AddField("Kraken rate", sb.ToString(), false);
                }

                if (amount != 1m)
                {
                    sb = new StringBuilder();
                    sb.AppendFormat("1.00 {0} = {1:#,##0.000000} {2}", Formatter.Bold(primary.Code), ticker.Price, Formatter.Bold(secondary.Code));

                    embed.AddField("Exchange rate", sb.ToString(), false);
                }

                if (ticker.ExchangedVolume != null)
                {
                    embed.AddField("Volume exchanged", ticker.ExchangedVolume.Value.ToString("#,##0.00"), false);
                }

                await this.Utilities.SendEmbedAsync(ctx, embed.Build());
            }
            catch (Exception ex)
            {
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Conversion failed", string.Concat("Cryptonator API responded with an error: ", Formatter.InlineCode(ex.Message)), 1).Build());
            }
        }

        [Command("balance"), Aliases("bal")]
        public async Task BalanceAsync(CommandContext ctx, ICurrency target = null, string address = null)
        {
            try
            {
                address = address ?? this.Settings.EthereumAddress;
                var npresp = await this.Nanopool.GetBalanceAsync(address);
                if (!npresp.IsSuccessful)
                    throw new Exception(npresp.ErrorMessage);
                var bal = npresp.GetData<decimal>();
                
                target = target ?? this.Settings.DefaultTargetCurrency;
                var cnresp = await this.Cryptonator.GetConversionRateAsync(CryptonatorApiClient.CryptoCurrencies["ETH"], target);
                if (!cnresp.IsSuccessful)
                    throw new Exception(cnresp.ErrorMessage);

                var ticker = cnresp.Ticker;

                var sb = new StringBuilder();
                sb.AppendFormat("{0:#,##0.000000} **ETH** ({1:#,##0.00} {2})", bal, bal * ticker.Price, Formatter.Bold(target.Code));

                var embed = this.Utilities.BuildEmbed(string.Concat("Balance for ", address), sb.ToString(), 0);

                sb = new StringBuilder();
                sb.AppendFormat("1.00 **ETH** = {1:#,##0.00} {2}", bal, ticker.Price, Formatter.Bold(target.Code));
                embed.AddField("Exchange rate", sb.ToString(), false);

                await this.Utilities.SendEmbedAsync(ctx, embed.Build());
            }
            catch (Exception ex)
            {
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Balance check failed", string.Concat("Nanopool API responded with an error: ", Formatter.InlineCode(ex.Message)), 1).Build());
            }
        }

        [Command("status")]
        public async Task StatusAsync(CommandContext ctx, string address = null)
        {
            try
            {
                address = address ?? this.Settings.EthereumAddress;
                var resp = await this.Nanopool.GetStatusAsync(address);
                if (!resp.IsSuccessful)
                    throw new Exception(resp.ErrorMessage);
                var dat = resp.GetData<NanopoolGeneralData>();

                var embed = this.Utilities.BuildEmbed(string.Concat("Status for ", address), "", 0)
                    .AddField("Current hashrate", string.Concat(dat.CurrentHashrate.ToString("#,##0.00"), "MH/s"), true)
                    .AddField("6h average hashrate", string.Concat(dat.AverageHashrate.Average6H.ToString("#,##0.00"), "MH/s"), true)
                    .AddField("Current balance", string.Concat(dat.Balance.ToString("#,##0.000000"), " ETH (", dat.UnconfirmedBalance.ToString("#,##0.000000"), " ETH unconfirmed)"), true)
                    .AddField("Worker count", string.Concat(dat.Workers.Count.ToString("#,##0")), true)
                    .AddField("Last share", dat.Workers.Max(xw => xw.LastShare).ToString("yyyy-MM-dd HH:mm:ss zzz"), true);

                await this.Utilities.SendEmbedAsync(ctx, embed.Build());
            }
            catch (Exception ex)
            {
                await this.Utilities.SendEmbedAsync(ctx, this.Utilities.BuildEmbed("Balance check failed", string.Concat("Nanopool API responded with an error: ", Formatter.InlineCode(ex.Message)), 1).Build());
            }
        }
    }
}