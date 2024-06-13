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
        public async Task ScalpLongsAsync(string coin, decimal capital, decimal consideredMoveStartPercentage, decimal wholeMovePercentage,
            int secondsBetweenUpdates, int leverage, int decimals, int multiple, decimal presetBottom = -1, bool trackTrade = false)
        {
            #region Set Up Trade
            coin += "USDT";

            decimal bottomPrice;
            var currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);

            if (presetBottom != -1)
                bottomPrice = presetBottom;
            else
                bottomPrice = currentPrice;

            consideredMoveStartPercentage /= 100;
            wholeMovePercentage /= 100;
            decimal targetPriceToConsiderMoveStarted = bottomPrice + (bottomPrice * consideredMoveStartPercentage); // bottom + ? = consideredMoveStartPercentage pokachvane

            bool tradePlaced = false;
            bool hasMoveStarted = false;

            Console.WriteLine("Pre loop information:");
            Console.WriteLine($"Current Price: {currentPrice}");
            Console.WriteLine($"First Bottom: {bottomPrice}");
            Console.WriteLine($"Targeted price to consider move started: {targetPriceToConsiderMoveStarted}");
            Console.WriteLine($"Take profit will be placed at: {bottomPrice + (bottomPrice * wholeMovePercentage)}");
            Console.WriteLine("-------------------------------------------------------------\n");
            Console.WriteLine("Pre trade information:");
            var setLeverageResult = await _orderService.SetCoinLeverageAsync(coin, leverage);
            Console.WriteLine($"Leverage message: {setLeverageResult}");
            #endregion

            while (!tradePlaced)
            {
                #region Find Move
                currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);

                if (currentPrice < bottomPrice) // new bottom has been found
                {
                    bottomPrice = currentPrice;
                    targetPriceToConsiderMoveStarted = bottomPrice + (bottomPrice * consideredMoveStartPercentage); // recalculate targeted price for move start
                    Console.WriteLine($"New bottom set: {bottomPrice}");
                    Console.WriteLine($"New Targeted price for move start: {targetPriceToConsiderMoveStarted}");
                    Console.WriteLine($"Take profit will be placed at: {bottomPrice + (bottomPrice * wholeMovePercentage)}");

                    decimal expectedQuantity = capital * leverage / currentPrice;
                    Console.WriteLine($"Expected Formated Quantity: {FormatQuantity(expectedQuantity, decimals, multiple)}\n");
                }
                else if (currentPrice >= targetPriceToConsiderMoveStarted)
                    hasMoveStarted = true;

                if (!hasMoveStarted)
                {
                    Thread.Sleep(1000 * secondsBetweenUpdates);
                    continue;
                }
                #endregion

                #region Place Trade
                // calculate TP and SL
                decimal takeProfit = bottomPrice + (bottomPrice * wholeMovePercentage);
                decimal stopLoss = bottomPrice; // if we get stopped often at retest change to bottom - 1%;
                decimal quantityToBuy = capital * leverage / currentPrice;
                string orderQuantity = FormatQuantity(quantityToBuy, decimals, multiple);

                var placeOrderResult = await _orderService.PlaceOrderAsync(Category.LINEAR, coin, Side.BUY, OrderType.MARKET, orderQuantity,
                    currentPrice.ToString(), takeProfit.ToString(), stopLoss.ToString());

                if (placeOrderResult.RetMsg == "OK")
                {
                    tradePlaced = true;

                    Console.WriteLine("-------------------------------------------------------------\n");
                    Console.WriteLine("Move started information:");
                    Console.WriteLine("Trade placed successfuly");
                    var openOrder = await _orderService.GetOpenOrdersAsync(coin, Category.LINEAR);
                    var tpslstats = openOrder.Result.List.OrderByDescending(o => o.TriggerPrice);
                    var takeProfitOrder = openOrder.Result.List[0]; // old TP
                    var stopLossOrder = openOrder.Result.List[1]; // old SL

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
                #endregion
            }

            if (trackTrade)
            {
                var intialTakeProfit = bottomPrice + (bottomPrice * wholeMovePercentage);
                var profitRangePercentage = wholeMovePercentage - consideredMoveStartPercentage; // the price range for the trade from entry to TP in percentages - Whole move 5%, Move start 3%, Range = 2% (5 - 3)
                await TrackTradeAsync(bottomPrice, profitRangePercentage, coin, targetPriceToConsiderMoveStarted, intialTakeProfit);
            }
        }

        private async Task TrackTradeAsync(decimal bottomPrice, decimal profitRangePercentage, string coin, decimal entry, decimal initialTakeProfit)
        {
            Console.WriteLine("Trade tracking started");
            #region Update Stoploss
            var fiftyPercentOfProfitRange =  profitRangePercentage * 0.5m; // For 2% range, This value be 1%
            var fiftyPercentOfProfitRangeAsPrice = entry + bottomPrice * fiftyPercentOfProfitRange;

            var seventyFivePercentOfProfitRange = profitRangePercentage * 0.9m;
            var seventyFivePercentOfProfitRangeAsPrice = entry + bottomPrice * seventyFivePercentOfProfitRange;

            var nintyPercentOfProfitRange = profitRangePercentage * 0.9m;
            var nintyPercentOfProfitRangeAsPrice = entry + bottomPrice * nintyPercentOfProfitRange;

            var nintyFivePercentOfProfitRange = profitRangePercentage * 0.95m;
            var nintyFivePercentOfProfitRangeAsPrice = entry + bottomPrice * nintyFivePercentOfProfitRange;

            var startChasingProfits = false;
            var openOrder = (await _orderService.GetOpenOrdersAsync(coin, Category.LINEAR)).Result.List[0];

            var profitRangeAsPrice = initialTakeProfit - entry;
            var tpAfterIncrease = initialTakeProfit;
            decimal slAfterIncrease = 0;

            while (openOrder == null)
            {
                Console.WriteLine("Couldnt get open order, retry");
                openOrder = (await _orderService.GetOpenOrdersAsync(coin, Category.LINEAR)).Result.List[0];
            }

            while (!startChasingProfits)
            {
                var currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);

                // bottom price * Percent gives us percentage as price. Then we check if we rised with that much $

                if (currentPrice >= nintyFivePercentOfProfitRangeAsPrice) // Set SL to 90% profits, increase tp to 110%
                {
                    //  change SL to 90% profits
                    slAfterIncrease = nintyPercentOfProfitRangeAsPrice;
                    // set TP to 110%;

                    tpAfterIncrease = IncreaseTargetRangeWithTenPercent(initialTakeProfit, profitRangeAsPrice);
                    var amendOrderResult = await _orderService.AmendTPSLAsync(coin, openOrder.OrderId, tpAfterIncrease.ToString(), slAfterIncrease.ToString());
                    if (amendOrderResult.RetMsg != "OK")
                        Console.WriteLine($"Amend orded failed, message: {amendOrderResult.RetMsg}");

                    startChasingProfits = true;
                }
                else if (currentPrice >= seventyFivePercentOfProfitRangeAsPrice) // set SL to 50% profits
                {
                    var amendOrderResult = await _orderService.AmendSLAsync(coin, openOrder.OrderId, fiftyPercentOfProfitRangeAsPrice.ToString());

                    if (amendOrderResult.RetMsg != "OK")
                        Console.WriteLine($"Amend orded failed, message: {amendOrderResult.RetMsg}");
                }
                else if (currentPrice >= fiftyPercentOfProfitRangeAsPrice) // check if the price has reached 50% of the trade (the price has rised ? % from the bottom), set SL to Entry
                {
                    //change SL to Entry
                    var amendOrderResult = await _orderService.AmendSLAsync(coin, openOrder.OrderId, entry.ToString());

                    if (amendOrderResult.RetMsg != "OK")
                        Console.WriteLine($"Amend orded failed, message: {amendOrderResult.RetMsg}");
                }
            }
            #endregion

            #region Start Chasing Profits
            // at this moment tp is 110%, SL is 90%, price is 95%+
            openOrder = (await _orderService.GetOpenOrdersAsync(coin, Category.LINEAR)).Result?.List[0];

            while (openOrder != null)
            {
                var currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.LINEAR);

                if (currentPrice >= AlmostHitTpPrice(tpAfterIncrease, profitRangeAsPrice)) // tp almost hit, example 107%, TP becomes 120%, sl becomes 100%
                {
                    slAfterIncrease = IncreaseTargetRangeWithTenPercent(slAfterIncrease, profitRangeAsPrice);
                    tpAfterIncrease = IncreaseTargetRangeWithTenPercent(tpAfterIncrease, profitRangeAsPrice);

                    var amendOrderResult = await _orderService.AmendTPSLAsync(coin, openOrder.OrderId, tpAfterIncrease.ToString(), slAfterIncrease.ToString());
                    if (amendOrderResult.RetMsg != "OK")
                        Console.WriteLine($"Amend orded failed, message: {amendOrderResult.RetMsg}");
                }

                openOrder = (await _orderService.GetOpenOrdersAsync(coin, Category.LINEAR)).Result?.List[0];
            }
            #endregion

            Console.WriteLine($"Track order complete, Original TP: {initialTakeProfit}, final TP: {tpAfterIncrease}, final SL: {slAfterIncrease}");
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

        #region Private Methods
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

        private decimal IncreaseTargetRangeWithTenPercent(decimal currentTargetPrice, decimal profitRangeAsPrice)
        {
            return currentTargetPrice + profitRangeAsPrice * 0.1m; //
        }

        private decimal AlmostHitTpPrice(decimal currentTpPrice, decimal profitRangeAsPrice)
        {
            return currentTpPrice - profitRangeAsPrice * 0.03m; // reduce TP with 3% to check if we are close to hitting the real TP
        }
        #endregion
    }
}
