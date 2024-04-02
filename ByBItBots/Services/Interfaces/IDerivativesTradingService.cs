using bybit.net.api.ApiServiceImp;
using ByBitBots.DTOs;

namespace ByBItBots.Services.Interfaces
{
    public interface IDerivativesTradingService
    {
        /// <summary>
        /// Gets all derivatives coins whose funding can be used to make profits.
        /// </summary>
        /// <returns></returns>
        Task<List<CoinShortInfo>> GetCoinsForFundingTradingAsync();
        Task<List<CoinShortInfo>> GetProfitableFundingsAsync();
        Task OpenFundingCoinsTradesAsync(BybitMarketDataService _marketService, BybitPositionService position, BybitTradeService trade, decimal capitalPerCoin);

        /// <summary>
        /// Gets information about a specific coin. If no coin is specified information for all coins will be returned. Currently all coins cannot be returned on TestNet
        /// </summary>
        /// <param name="symbol">The coin whose information is required. Example - BTCUSDT</param>
        /// <returns></returns>
        Task<List<CoinShortInfo>> GetDerivativesCoinsAsync(string symbol = null);
    }
}
