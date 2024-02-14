using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models;
using ByBitBots.DTOs;
using ByBItBots.Constants;
using ByBItBots.Results;
using ByBItBots.Services.Interfaces;
using Newtonsoft.Json;

namespace ByBItBots.Services.Implementations
{
    public class FundingTradingService : IFundingTradingService
    {
        private readonly BybitMarketDataService _marketService;
        private readonly IOrderService _orderService;
        private readonly ICoinDataService _coinDataService;
        private readonly IBybitTimeService _timeService;
        private readonly IPrinterService _printService;

        public FundingTradingService(BybitMarketDataService marketService
            , IOrderService orderService
            , ICoinDataService coinDataService
            , IBybitTimeService timeService
            , IPrinterService printerService)
        {
            _marketService = marketService;
            _orderService = orderService;
            _coinDataService = coinDataService;
            _timeService = timeService;
            _printService = printerService;
        }

        public async Task<List<CoinShortInfo>> GetCoinsForFundingTradingAsync()
        {
            List<CoinShortInfo> fittingCoins = await GetProfitableFundingsAsync();

            return fittingCoins.OrderByDescending(c => c.Profits).ToList();
        }

        public async Task<List<CoinShortInfo>> GetProfitableFundingsAsync()
        {
            var derivativeCoins = await GetDerivativesCoinsAsync();

            derivativeCoins = derivativeCoins.Where(c => c.FundingRate > 0.0012m || c.FundingRate < -0.0012m).ToList();

            await _coinDataService.SetLeverageAndFundingRate(derivativeCoins);

            return derivativeCoins;
        }

        // Work in progress
        public async Task OpenFundingCoinsTradesAsync(BybitMarketDataService _marketService, BybitPositionService position, BybitTradeService trade, decimal capitalPerCoin)
        {
            var bybitFundingTimes = new List<int>
            {
                4, 8, 12
            };

            while (true)
            {
                var fundingCoins = await GetCoinsForFundingTradingAsync();

                fundingCoins = fundingCoins.Where(c => c.Profits > 4).ToList();

                var mostProfitableCoin = fundingCoins[0];

                var bybitTime = await _timeService.GetCurrentBybitTimeAsync();
                _printService.PrintMessage($"Bybit time: {bybitTime}");

                if (fundingCoins.Count != 0)
                {
                    if (bybitFundingTimes.Contains(bybitTime.Hour) && bybitTime.Minute == 59 && bybitTime.Second >= 58)
                    {
                        var orders = await _orderService.CreateOrdersAsync(fundingCoins, capitalPerCoin);

                        await trade.PlaceBatchOrder(Category.LINEAR, orders.OpenRequests);

                        Thread.Sleep(2000);

                        await trade.PlaceBatchOrder(Category.LINEAR, orders.CloseRequests);

                        break;
                    }
                }

                var myTimeToBybitTime = DateTime.Now.AddHours(-2);

                var timeUntilNextFunding = new TimeSpan();

                if (mostProfitableCoin != null)
                {
                    timeUntilNextFunding = mostProfitableCoin.NextFundingHour - bybitTime;
                }
                else
                {
                    timeUntilNextFunding = myTimeToBybitTime - bybitTime;
                }
            }
        }

        public async Task<List<CoinShortInfo>> GetDerivativesCoinsAsync(string symbol = null)
        {
            var marketTickers = await _marketService.GetMarketTickers(Category.LINEAR, symbol);
            ApiResponseResult<ResultCoinInfo> info = JsonConvert.DeserializeObject<ApiResponseResult<ResultCoinInfo>>(marketTickers);

            if (info == null || info.Result.List.Count == 0)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_RETRIVE_DERIVATIVES_COINS);
            }

            return info.Result.List
               .Where(c => c.Symbol.Contains("USDT"))
               .Select(c => new CoinShortInfo
               {
                   FundingRate = decimal.Parse(c.FundingRate),
                   NextFunding = long.Parse(c.NextFundingTime),
                   Symbol = c.Symbol,
                   Price = decimal.Parse(c.LastPrice)
               })
               .ToList();
        }


    }
}
