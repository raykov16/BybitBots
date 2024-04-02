using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models;
using bybit.net.api.Models.Trade;
using ByBitBots.DTOs;
using ByBItBots.Constants;
using ByBItBots.Results;
using ByBItBots.Services.Interfaces;
using Newtonsoft.Json;

namespace ByBItBots.Services.Implementations
{
    public class DerivativesTradingService : IDerivativesTradingService
    {
        private readonly BybitMarketDataService _marketService;
        private readonly IOrderService _orderService;
        private readonly ICoinDataService _coinDataService;
        private readonly IBybitTimeService _timeService;
        private readonly IPrinterService _printService;

        public DerivativesTradingService(BybitMarketDataService marketService
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

        public async Task ScalpVolatileMovements(string coin, decimal capitalPercentage)
        {
            Side side = default;
            var secondsToWait = 30;
            string quantity = string.Empty;
            decimal currentPrice = default;
            decimal previousPrice = default;
            decimal percentWins = 0;
            decimal percentLoses = 0;
            bool profitableTrading = true;

            while (profitableTrading) 
            {
                previousPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);
                Thread.Sleep(secondsToWait * 1000);
                currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);

                var percentageDiff = CalculatePercentageDifference(previousPrice, currentPrice);

                if (percentageDiff < 0.012m)
                {
                    continue;
                }

                side = currentPrice > previousPrice ? Side.BUY : Side.SELL;
                var placeOrderResult = await _orderService.PlaceOrderAsync(Category.LINEAR, coin, side, OrderType.MARKET, quantity, currentPrice.ToString());

                //while(tradeIsOpen)
                //{
                //  if(UnrealisedPL >= 25%)
                //  {
                //   position.Stoploss = entry;
                //  }
                //
                //  if (UnrealisedPL >= 50 %)
                //  {
                //   position.Stoploss = 25%;
                //  }     
                //  if (UnrealisedPL >= 75%)
                //  {
                //   position.Stoploss = 50%;
                //   position.TakeProfit = 100%;
                //  }
                //  if (UnrealisedPL >= 90%)
                //  {
                //   position.Stoploss = 80%;
                //   position.TakeProfit = 110%;
                //  }
                //  if (UnrealisedPL >= 100%)
                //  {
                //   position.Stoploss = 90%;
                //   position.TakeProfit = 120%;
                //  }
                // var previousPL = unrealisedPL
                //  while(tradeIsOpen)
                // {
                // var currentPL = undrealisedPL
                //   if(currentPL > s 10% ot previousPL)
                //   {
                //   position.Stoploss += 15%
                //   position.TP += 10%
                //   }
                // }
                //
                // if(closedTrade.OrderPrice < MarketPrice) imame stoploss
                //  {
                //    percentLoses += 100%;
                //  }
                // else
                // {
                //   percentWins += percentWon;
                // }
                // if(percentLosses >= 150% && percentLosses > percentWins)
                // profitableTrading = false;
                //}
            }
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
                _printService.PrintMessage(string.Format(InterfaceCommunicationMessages.BYBIT_TIME, bybitTime));

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

        private decimal CalculatePercentageDifference(decimal num1, decimal num2)
        {
            decimal absoluteDifference = Math.Abs(num1 - num2);
            decimal average = (num1 + num2) / 2;

            decimal percentageDifference = (absoluteDifference / average) * 100;

            return Math.Round(percentageDifference, 3);
        }
    }
}
