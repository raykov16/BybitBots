using bybit.net.api.Models;
using ByBitBots.DTOs;

namespace ByBItBots.Services.Interfaces
{
    public interface ICoinDataService
    {
        Task<decimal> GetCurrentPriceAsync(string symbol, Category category);
        Task<decimal> GetLeverageAsync(string symbol);
        Task SetLeverageAndFundingRate(List<CoinShortInfo> coins);
    }
}
