using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models;
using ByBitBots.DTOs;
using ByBItBots.Constants;
using ByBItBots.Results;
using ByBItBots.Services.Interfaces;
using Newtonsoft.Json;

namespace ByBItBots.Services.Implementations
{
    public class CoinDataService : ICoinDataService
    {
        private readonly BybitMarketDataService _marketService;

        public CoinDataService(BybitMarketDataService marketService)
        {
            _marketService = marketService;
        }

        public async Task SetLeverageAndFundingRate(List<CoinShortInfo> coins)
        {
            foreach (var c in coins)
            {
                c.Leverage = await GetLeverageAsync(c.Symbol);

                c.FundingRate *= 100;
            }
        }

        public async Task<decimal> GetLeverageAsync(string symbol)
        {
            var instrumentInfo = await _marketService.GetInstrumentInfo(Category.LINEAR, symbol);

            if (instrumentInfo == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_RETRIVE_COIN_INFO);
            }

            var arr = new string[100];

            arr = instrumentInfo.Split(":").ToArray();

            return decimal.Parse(arr[17].Split("\",")[0].Replace("\"", ""));
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol, Category category)
        {
            var marketTickers = await _marketService.GetMarketTickers(category, symbol);
            ApiResponseResult<ResultCoinInfo> info = JsonConvert.DeserializeObject<ApiResponseResult<ResultCoinInfo>>(marketTickers);

            if (info == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_RETRIVE_MARKET_INFO);
            }

            var coin = new CoinShortInfo();

            if (category == Category.SPOT)
            {
                coin = info.Result.List
                .Where(c => c.Symbol.Contains("USDT"))
                .Select(c => new CoinShortInfo
                {
                    Symbol = c.Symbol,
                    Price = decimal.Parse(c.LastPrice)
                })
                .FirstOrDefault();
            }
            else
            {
               coin = info.Result.List
              .Where(c => c.Symbol.Contains("USDT"))
              .Select(c => new CoinShortInfo
              {
                  FundingRate = decimal.Parse(c.FundingRate),
                  NextFunding = long.Parse(c.NextFundingTime),
                  Symbol = c.Symbol,
                  Price = decimal.Parse(c.LastPrice)
              })
              .FirstOrDefault();
            }

            return coin.Price;
        }
    }
}
