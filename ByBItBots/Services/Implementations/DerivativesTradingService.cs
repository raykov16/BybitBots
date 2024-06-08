using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models;
using bybit.net.api.Models.Trade;
using ByBitBots.DTOs;
using ByBItBots.Constants;
using ByBItBots.Results;
using ByBItBots.Services.Interfaces;
using Newtonsoft.Json;
using System.Drawing;

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

        #region Scalp Volatile Movements
        public async Task ScalpVolatileMovementsOld(string coin, decimal capitalPercentage)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin">The symbol of the coin - BTCUSDT</param>
        /// <param name="capital">Your capital</param>
        /// <param name="consideredMoveStartPercentage">The rise of price in percents that we consider is a start of a move. For example for a 6% move we consider 3% as the start</param>
        /// <param name="wholeMovePercentage">The percent of the whole move - start + TP</param>
        /// <param name="secondsBetweenUpdates">seconds bettwen each check for new price</param>
        /// <param name="leverage">The leverage for the trade</param>
        /// <param name="presetBottom">Set this parameter only if you have already chosen a bottom by looking at the chart</param>
        /// <returns></returns>
        public async Task ScalpVolatileLongsAsync(string coin, decimal capital, decimal consideredMoveStartPercentage, decimal wholeMovePercentage,
            int secondsBetweenUpdates, int leverage, int decimals, int multiple, decimal presetBottom = -1)
        {
            // TO DO
            // sus 1000 dolara i 100 leverage vsushnost otvori poziciq s 10$ zashtoto 10 * 100 = 1000. Nie vsushnost iskame 1000 * 10 = 10000.
            // Suotvetno izmeni capital *= leverage! vuzmojno da e bug ot testnet ne go probvai predi da potvurdish na mainnet che tova e istina
            coin += "USDT";

            decimal bottomPrice;
            var currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);

            if (presetBottom != -1)
                bottomPrice = presetBottom;
            else
                bottomPrice = currentPrice;

            consideredMoveStartPercentage /= 100;
            wholeMovePercentage /= 100;
            decimal targetPriceForMoveStart = bottomPrice + (bottomPrice * consideredMoveStartPercentage); // bottom + ? = consideredMoveStartPercentage pokachvane

            bool tradePlaced = false;
            bool hasMoveStarted = false;

            Console.WriteLine("Pre loop information:");
            Console.WriteLine($"Current Price: {currentPrice}");
            Console.WriteLine($"First Bottom: {bottomPrice}");
            Console.WriteLine($"Targeted price for move start: {targetPriceForMoveStart}");
            Console.WriteLine("-------------------------------------------------------------\n");
            Console.WriteLine("Pre trade information:");

            while (!tradePlaced)
            {
                currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);

                if (currentPrice < bottomPrice) // new bottom has been found
                {
                    bottomPrice = currentPrice;
                    targetPriceForMoveStart = bottomPrice + (bottomPrice * consideredMoveStartPercentage); // recalculate targeted price for move start
                    Console.WriteLine($"New bottom set: {bottomPrice}");
                    Console.WriteLine($"New Targeted price for move start: {targetPriceForMoveStart}");
                }
                else if (currentPrice >= targetPriceForMoveStart)
                    hasMoveStarted = true;

                if (!hasMoveStarted)
                {
                    Thread.Sleep(1000 * secondsBetweenUpdates);
                    continue;
                }

                // calculate TP and SL
                decimal takeProfit = bottomPrice + (bottomPrice * wholeMovePercentage);
                decimal stopLoss = bottomPrice; // if we get stopped often at retest change to bottom - 1%;
                decimal quantityToBuy = capital * leverage / currentPrice;
                string orderQuantity = FormatQuantity(quantityToBuy, decimals, multiple);

                var setLeverageResult = await _orderService.SetCoinLeverageAsync(coin, leverage);
                Console.WriteLine($"Leverage message: {setLeverageResult}");

                var placeOrderResult = await _orderService.PlaceOrderAsync(Category.LINEAR, coin, Side.BUY, OrderType.MARKET, orderQuantity,
                    currentPrice.ToString(), takeProfit.ToString(), stopLoss.ToString());

                if (placeOrderResult.RetMsg == "OK")
                {
                    tradePlaced = true;

                    Console.WriteLine("-------------------------------------------------------------\n");
                    Console.WriteLine("Move started information:");
                    Console.WriteLine("Trade placed successfuly");
                    var openOrder = await _orderService.GetOpenOrdersAsync(coin, Category.LINEAR);
                    var stopLossOrder = openOrder.Result.List[0];
                    var takeProfitOrder = openOrder.Result.List[1];

                    Console.WriteLine($"Entry price: {takeProfitOrder.LastPriceOnCreated}");
                    Console.WriteLine($"Take profit price: {takeProfitOrder.TriggerPrice}");
                    Console.WriteLine($"Stop loss price: {stopLossOrder.TriggerPrice}");
                }
                else
                {
                    Console.WriteLine("-------------------------------------------------------------\n");
                    Console.WriteLine($"PLACE ORDER ERROR: {placeOrderResult.RetMsg}");
                    Console.WriteLine($"Quantity: {orderQuantity}");
                    Console.WriteLine($"Take profit: {takeProfit}");
                    Console.WriteLine($"Stop loss: {stopLoss}");
                    break;
                }
            }


        }

        #endregion

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

        private string FormatQuantity(decimal calculatedQuantity, int decimals, int multiple)
        {
            string result = string.Empty;
            calculatedQuantity = Math.Round(calculatedQuantity, decimals);
            result = calculatedQuantity.ToString();

            if (multiple == 10)
            {
                int roundResult = RoundToNearestTenth((int)calculatedQuantity);
                result = roundResult.ToString();
            }
            else if (multiple == 100)
            {
                int roundResult = RoundToNearestHundred((int)calculatedQuantity);
                result = roundResult.ToString();
            }

            return result;
        }

        private int RoundToNearestTenth(int number)
        {
            return (int)Math.Round(number / 10.0) * 10;
        }

        private int RoundToNearestHundred(int number)
        {
            return (int)Math.Round(number / 100.0) * 100;
        }
    }
}
