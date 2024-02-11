using bybit.net.api.Models.Trade;
using ByBitBots.DTOs;

namespace ByBItBots.Services.Interfaces
{
    public interface ISpotTradingService
    {
        Task FarmSpotVolumeAsync(string coin, decimal capital, decimal requiredVolume, int requestInterval = 5, decimal maxPricePercentDiff = 0.01m, int minutesWithoutTrade = 5);
        Task BuySellSpotCoinFirstAsync(string symbol, decimal capital, Side side);

        /// <summary>
        /// Gets information about a specific coin. If no coin is specified information for all coins will be returned. Currently all coins cannot be returned on TestNet
        /// </summary>
        /// <param name="symbol">The coin whose information is required. Example - BTCUSDT</param>
        /// <returns></returns>
        Task<List<CoinShortInfo>> GetSpotCoinsAsync(string? symbol = null);
    }
}
