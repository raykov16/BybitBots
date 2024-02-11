using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models.Trade;
using bybit.net.api.Models;
using ByBItBots.Results;
using Newtonsoft.Json;
using ByBItBots.Services.Interfaces;
using ByBitBots.DTOs;

namespace ByBItBots.Services.Implementations
{
    public class SpotTradingService : ISpotTradingService
    {
        private readonly BybitMarketDataService _marketService;
        private readonly IOrderService _orderService;
        private readonly ICoinDataService _coinDataService;

        public SpotTradingService(BybitMarketDataService marketService
            , IOrderService orderService
            , ICoinDataService coinDataService)
        {
            _marketService = marketService;
            _orderService = orderService;
            _coinDataService = coinDataService;
        }

        // ako predniq order ne e fillnat predi zapochvaneto - cancelirash go i go puskash na novo na segashnite ceni
        public async Task FarmSpotVolumeAsync(string coin, decimal capital, decimal requiredVolume, int requestInterval = 5, decimal maxPricePercentDiff = 0.01m, int minutesWithoutTrade = 5)
        {
            decimal tradedVolume = 0m;
            bool shouldBuy = true;
            decimal quantity;
            int timesWithouthTrade = 0;

            if (minutesWithoutTrade > 0)
                timesWithouthTrade = (minutesWithoutTrade * 60) / requestInterval; // Example 300 / reqInterval.
                                                                                   // 300 representva 5 minuti v sekundi - 60 * 5 = 300 sekundi.
            var currentTimesWithoutTrade = 0;
            requestInterval *= 1000; // transforming seconds to MS

            var timeStarted = DateTime.UtcNow;

            while (tradedVolume < requiredVolume)
            {
                // check for open orders
                var openOrdersResult = await _orderService.GetOpenOrdersAsync(coin);
                Console.WriteLine("Got open orders");

                if (openOrdersResult.RetMsg != "OK")
                {
                    Console.WriteLine(openOrdersResult.RetMsg);
                    break;
                }

                if (openOrdersResult.Result.List.Count > 0)
                    Console.WriteLine("Order price " + openOrdersResult.Result.List[0].Price);

                var currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.SPOT);
                Console.WriteLine("Got price");
                Console.WriteLine($"market price {currentPrice}");

                //ako ima otvoren order i cenata na toq order se razminava ot segashnata cena - vlizame
                if (openOrdersResult.Result.List.Count == 0)
                {
                    Console.WriteLine("No orders found, opening an order!");

                    if (tradedVolume >= requiredVolume)
                        break;

                    var previousOrderInfo = await _orderService.GetLastOrderInfoAsync(coin);

                    string previousSide;
                    decimal previousPrice;

                    if (previousOrderInfo == null)
                    {
                        shouldBuy = true;
                        previousPrice = currentPrice;
                    }
                    else
                    {
                        previousSide = previousOrderInfo.Side;
                        previousPrice = decimal.Parse(previousOrderInfo.Price);

                        if (previousSide == Side.BUY)
                        {
                            shouldBuy = false;
                        }
                        else
                        {
                            shouldBuy = true;
                        }

                        Console.WriteLine("Previous side: " + previousSide);
                    }

                    var side = shouldBuy ? Side.BUY : Side.SELL;

                    Console.WriteLine("Current side: " + side);

                    currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.SPOT);
                    Console.WriteLine("getting price");

                    var priceDiff = CalculatePercentageDifference(previousPrice, currentPrice);

                    if (side == Side.SELL && priceDiff > maxPricePercentDiff && currentPrice <= previousPrice && currentTimesWithoutTrade < timesWithouthTrade)
                    {
                        Console.WriteLine($"Price diff: {priceDiff}, sell order will not be placed!");
                        Console.WriteLine($"Times without trade: {++currentTimesWithoutTrade}");
                        Thread.Sleep(requestInterval);
                        continue;
                    }
                    else
                    {
                        currentTimesWithoutTrade = 0;
                        Console.WriteLine($"Reseting times without trade to: {currentTimesWithoutTrade}");
                    }

                    if (side == Side.SELL)
                    {
                        quantity = Math.Round(decimal.Parse(previousOrderInfo.Quantity), 2);
                    }
                    else
                    {
                        quantity = Math.Round(capital / currentPrice, 2);
                    }

                    var placeOrderResult = await _orderService.PlaceOrderAsync(Category.SPOT, coin, side, OrderType.LIMIT, $"{quantity}", $"{currentPrice}");

                    Console.WriteLine("Placed order result: " + placeOrderResult.RetMsg);

                    if (placeOrderResult.RetMsg == "OK")
                    {
                        tradedVolume += capital;
                    }
                }
                else if (decimal.Parse(openOrdersResult.Result.List[0].Price) != currentPrice)
                {
                    Console.WriteLine("Open order price difference, changing price!");
                    // pak vzimame  segashnata cena i izchislqvame quantitito          

                    var previousOrderInfo = await _orderService.GetLastOrderInfoAsync(coin);

                    var previousSide = previousOrderInfo.Side;
                    var previousPrice = decimal.Parse(previousOrderInfo.Price);

                    currentPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.SPOT);
                    Console.WriteLine("getting price");

                    var pricePercentDiff = CalculatePercentageDifference(previousPrice, currentPrice);

                    if (previousSide == Side.BUY && pricePercentDiff > maxPricePercentDiff && currentPrice < previousPrice && currentTimesWithoutTrade <= timesWithouthTrade)
                    {
                        Console.WriteLine($"Price diff: {pricePercentDiff}, existing order price will not be changed!");
                        Console.WriteLine($"Times without trade: {++currentTimesWithoutTrade}");
                        Thread.Sleep(requestInterval);
                        continue;
                    }
                    else
                    {
                        currentTimesWithoutTrade = 0;
                        Console.WriteLine($"Reseting times without trade to: {currentTimesWithoutTrade}");
                    }

                    // updeitvame ordera sus segashnata cena i podhodqshtoto quantity
                    var amendOrderResult = await _orderService.AmendOrderAsync(Category.SPOT,
                        coin,
                        openOrdersResult.Result.List[0].OrderId,
                        $"{currentPrice}");

                    Console.WriteLine("Ammended order result: " + amendOrderResult);
                }

                Console.WriteLine($"waiting {requestInterval / 1000} sec");
                Thread.Sleep(requestInterval);
                Console.WriteLine($"waited {requestInterval / 1000} sec, starting again");
                Console.WriteLine($"Accumulated volume : {tradedVolume} / {requiredVolume}");
            }

            Console.WriteLine($"Succesfuly acumulated {tradedVolume} volume for {DateTime.UtcNow.TimeOfDay - timeStarted.TimeOfDay}!");
        }

        public async Task BuySellSpotCoinFirstAsync(string symbol, decimal capital, Side side)
        {
            List<CoinShortInfo> fittingCoins = await GetSpotCoinsAsync(symbol);

            while (fittingCoins.Count == 0)
            {
                Console.WriteLine("Coin not listed yet!");
                fittingCoins = await GetSpotCoinsAsync(symbol);
            }

            var coin = fittingCoins[0];

            var currentPrice = await _coinDataService.GetCurrentPriceAsync(symbol, Category.SPOT);

            decimal quantity = 0;
            var previousOrderInfo = await _orderService.GetLastOrderInfoAsync(coin.Symbol);

            if (side == Side.SELL)
            {
                if (previousOrderInfo == null)
                {
                    Console.WriteLine("Can not sell something that you have not bought. Exiting...");
                    return;
                }

                quantity = decimal.Parse(previousOrderInfo.Quantity);
            }
            else
            {
                quantity = Math.Round(capital / currentPrice, 2);
            }

            var placeOrderResult = await _orderService.PlaceOrderAsync(Category.SPOT, coin.Symbol, side, OrderType.LIMIT, quantity.ToString(), currentPrice.ToString());
            Console.WriteLine($"Placed order result: {placeOrderResult.RetMsg}");

            while (placeOrderResult.RetMsg != "OK")
            {
                placeOrderResult = await _orderService.PlaceOrderAsync(Category.SPOT, coin.Symbol, side, OrderType.LIMIT, quantity.ToString(), currentPrice.ToString());
                Console.WriteLine($"Placed order result: {placeOrderResult.RetMsg}");
            }

            var openOrdersResult = await _orderService.GetOpenOrdersAsync(coin.Symbol);

            while (openOrdersResult.Result.List.Count > 0)
            {
                currentPrice = await _coinDataService.GetCurrentPriceAsync(coin.Symbol, Category.SPOT);

                if (side == Side.SELL)
                {
                    quantity = decimal.Parse(previousOrderInfo.Quantity);
                }
                else
                {
                    quantity = Math.Round(capital / currentPrice, 2);
                }

                var amendOrderResult = await _orderService.AmendOrderAsync(Category.SPOT,
                        coin.Symbol,
                        orderId: openOrdersResult.Result.List[0].OrderId,
                        $"{quantity}",
                        $"{currentPrice}");

                Console.WriteLine("Ammended order result: " + amendOrderResult);

                openOrdersResult = await _orderService.GetOpenOrdersAsync(coin.Symbol);
            }

            Console.WriteLine($"Successfuly traded {quantity} {coin}");
        }

        /// <summary>
        /// Returns Info for coins and their price. If no coin is specified all coins on the market will be returned in alphabetical order
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<CoinShortInfo>> GetSpotCoinsAsync(string? symbol = null)
        {
            var marketTickers = await _marketService.GetMarketTickers(Category.SPOT, symbol);
            ApiResponseResult<ResultCoinInfo> info = JsonConvert.DeserializeObject<ApiResponseResult<ResultCoinInfo>>(marketTickers);

            if (info == null || info.Result.List.Count == 0)
            {
                throw new InvalidOperationException("Market info could not be read, please check if the provided coin was correct");
            }

            return info.Result.List
                .Where(c => c.Symbol.Contains("USDT"))
                .Select(c => new CoinShortInfo
                {
                    Symbol = c.Symbol,
                    Price = decimal.Parse(c.LastPrice)
                })
                .OrderBy(c => c.Symbol)
                .ToList();
        }

        private decimal CalculatePercentageDifference(decimal num1, decimal num2)
        {
            decimal absoluteDifference = Math.Abs(num1 - num2);
            decimal average = (num1 + num2) / 2;

            decimal percentageDifference = (absoluteDifference / average) * 100;

            return Math.Round(percentageDifference, 2);
        }
    }
}
