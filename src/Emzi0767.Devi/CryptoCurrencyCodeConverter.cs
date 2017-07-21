using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using Emzi0767.Devi.Services;
using Emzi0767.Devi.Services.Data;

namespace Emzi0767.Devi
{
    public class CryptoCurrencyCodeConverter : IArgumentConverter<ICurrency>
    {
        public bool TryConvert(string input, CommandContext ctx, out ICurrency result)
        {
            input = input.ToUpper();
            if (CryptonatorApiClient.AllCurrencies.ContainsKey(input))
            {
                result = CryptonatorApiClient.AllCurrencies[input];
                return true;
            }

            result = null;
            return false;
        }
    }
}