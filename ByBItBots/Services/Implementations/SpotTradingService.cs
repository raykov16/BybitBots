using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models.Trade;
using bybit.net.api.Models;
using ByBItBots.Results;
using Newtonsoft.Json;
using ByBItBots.Services.Interfaces;
using ByBitBots.DTOs;
using ByBItBots.Constants;
using static ByBItBots.Constants.SpotTradingMessages;

namespace ByBItBots.Services.Implementations
{
    public class SpotTradingService : ISpotTradingService
    {
        private readonly BybitMarketDataService _marketService;
        private readonly IOrderService _orderService;
        private readonly ICoinDataService _coinDataService;
        private readonly IPrinterService _printerService;

        public SpotTradingService(BybitMarketDataService marketService
            , IOrderService orderService
            , ICoinDataService coinDataService
            , IPrinterService printerService)
        {
            _marketService = marketService;
            _orderService = orderService;
            _coinDataService = coinDataService;
            _printerService = printerService;
        }

        //TO DO:
        // create AmendOrderResult
        public async Task FarmSpotVolumeAsync(string coin, decimal capital, decimal requiredVolume, int requestInterval = 5, decimal maxPricePercentDiff = 0.01m, int minutesWithoutTrade = 5)
        {
            decimal tradedVolume = 0m;
            bool shouldBuy = true;
            string quantity;
            int timesWithouthTrade = 0;
            decimal initialTradeOpenPrice = 0m;

            if (minutesWithoutTrade > 0)
                timesWithouthTrade = (minutesWithoutTrade * 60) / requestInterval; // Example 300 / reqInterval.
                                                                                   // 300 representva 5 minuti v sekundi - 60 * 5 = 300 sekundi.
            var currentTimesWithoutTrade = 0;
            requestInterval *= 1000; // transforming seconds to MS

            var timeStarted = DateTime.UtcNow;

            while (tradedVolume < requiredVolume)
            {
                // check for open orders
                var openOrdersResult = await _orderService.GetOpenOrdersAsync(coin, Category.SPOT);
                _printerService.PrintMessage(GOT_OPEN_ORDERS);

                if (openOrdersResult.RetMsg != "OK")
                {
                    _printerService.PrintMessage(openOrdersResult.RetMsg);
                    break;
                }

                if (openOrdersResult.Result.List.Count > 0)
                    _printerService.PrintMessage(string.Format(ORDER_PRICE, openOrdersResult.Result.List[0].Price));

                var currentMarketPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.SPOT);
                _printerService.PrintMessage(string.Format(MARKET_PRICE, currentMarketPrice));
                var side = shouldBuy ? Side.BUY : Side.SELL;

                if (openOrdersResult.Result.List.Count == 0)
                {
                    _printerService.PrintMessage(OPENING_ORDER);

                    if (tradedVolume >= requiredVolume)
                        break;

                    var previousOrderInfo = await _orderService.GetLastOrderInfoAsync(coin);

                    string previousSide;
                    decimal previousPrice;

                    if (previousOrderInfo == null)
                    {
                        shouldBuy = true;
                        previousPrice = currentMarketPrice;
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

                        _printerService.PrintMessage(string.Format(PREVIOUS_SIDE, previousSide));
                    }

                    side = shouldBuy ? Side.BUY : Side.SELL;
                    _printerService.PrintMessage(string.Format(CURRENT_SIDE, side));

                    currentMarketPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.SPOT);
                    _printerService.PrintMessage(GETTING_PRICE);

                    var priceDiff = CalculatePercentageDifference(previousPrice, currentMarketPrice);

                    if (side == Side.SELL && priceDiff > maxPricePercentDiff && currentMarketPrice <= previousPrice && currentTimesWithoutTrade < timesWithouthTrade)
                    {
                        _printerService.PrintMessage(string.Format(PRICE_DIFF_TOO_LARGE, priceDiff));
                        _printerService.PrintMessage(string.Format(TIMES_WITHOUT_TRADE, ++currentTimesWithoutTrade));
                        Thread.Sleep(requestInterval);
                        continue;
                    }
                    else
                    {
                        currentTimesWithoutTrade = 0;
                        _printerService.PrintMessage(RESETING_TIMES_WITHOUT_TRADE);
                    }

                    if (side == Side.SELL)
                    {
                        var quantityToSell = previousOrderInfo != null ? previousOrderInfo.Quantity : $"{capital / currentMarketPrice}";

                        if (decimal.Parse(quantityToSell) < 1)
                        {
                            quantity = TrimSmallQuantity(quantityToSell);
                        }
                        else
                        {
                            quantity = quantityToSell;
                        }

                        var previousQuantity = previousOrderInfo != null ? previousOrderInfo.Quantity : "0";
                        _printerService.PrintMessage(string.Format(QUANTITY_TO_SELL, quantity, previousQuantity));
                    }
                    else
                    {
                        var quantityToBuy = capital / currentMarketPrice;

                        if (quantityToBuy < 1)
                        {
                            quantity = TrimSmallQuantity(quantityToBuy.ToString());
                        }
                        else
                        {
                            quantityToBuy = Math.Round(quantityToBuy, 2);
                            quantity = quantityToBuy.ToString();
                        }

                        var previousQuantity = previousOrderInfo != null ? previousOrderInfo.Quantity : "0";
                        _printerService.PrintMessage(string.Format(QUANTITY_TO_BUY, quantity, previousQuantity));
                    }

                    var placeOrderResult = await _orderService.PlaceOrderAsync(Category.SPOT, coin, side, OrderType.LIMIT, $"{quantity}", $"{currentMarketPrice}");

                    // if there was not a previous order - set the first price of selling / buying to be used for comparison in the pecent diff calculation
                    if (previousOrderInfo == null)
                        initialTradeOpenPrice = currentMarketPrice;

                    _printerService.PrintMessage(string.Format(PLACED_ORDER_RESULT, placeOrderResult.RetMsg));

                    if (placeOrderResult.RetMsg == "OK")
                    {
                        tradedVolume += capital;
                    }
                }
                else if (decimal.Parse(openOrdersResult.Result.List[0].Price) != currentMarketPrice)
                {
                    _printerService.PrintMessage(OPEN_ORDER_PRICE_DIFF);
                    // pak vzimame  segashnata cena i izchislqvame quantitito          

                    var previousOrderInfo = await _orderService.GetLastOrderInfoAsync(coin);
                    var previousOrderPrice = previousOrderInfo != null ? decimal.Parse(previousOrderInfo.Price) : initialTradeOpenPrice;
                    var openOrderPrice = decimal.Parse(openOrdersResult.Result.List[0].Price);

                    currentMarketPrice = await _coinDataService.GetCurrentPriceAsync(coin, Category.SPOT);
                    _printerService.PrintMessage(GETTING_PRICE);

                    var pricePercentDiff = CalculatePercentageDifference(previousOrderPrice, currentMarketPrice);

                    if (side == Side.SELL 
                        && pricePercentDiff > maxPricePercentDiff 
                        && currentMarketPrice < previousOrderPrice 
                        && currentMarketPrice <= openOrderPrice
                        && currentTimesWithoutTrade <= timesWithouthTrade)
                    {
                        _printerService.PrintMessage(string.Format(PRICE_DIFF_TOO_SMALL, pricePercentDiff));
                        _printerService.PrintMessage(string.Format(TIMES_WITHOUT_TRADE, ++currentTimesWithoutTrade));
                        Thread.Sleep(requestInterval);
                        continue;
                    }
                    else
                    {
                        currentTimesWithoutTrade = 0;
                        _printerService.PrintMessage(RESETING_TIMES_WITHOUT_TRADE);
                    }

                    // updeitvame ordera sus segashnata cena i podhodqshtoto quantity
                    var amendOrderResult = await _orderService.AmendOrderAsync(Category.SPOT,
                        coin,
                        openOrdersResult.Result.List[0].OrderId,
                        $"{currentMarketPrice}");

                    _printerService.PrintMessage(string.Format(AMEND_ORDER_RESULT, amendOrderResult.RetMsg));
                }

                _printerService.PrintMessage(string.Format(WAITING_SECONDS, requestInterval / 1000));
                Thread.Sleep(requestInterval);
                _printerService.PrintMessage(string.Format(WAITED_SECONDS, requestInterval / 1000));
                _printerService.PrintMessage(string.Format(ACCUMULATED_VOLUME, tradedVolume, requiredVolume));
            }

            _printerService.PrintMessage(string.Format(SUCCESSFULY_ACCUMULATED_VOLUME, tradedVolume, DateTime.UtcNow.TimeOfDay - timeStarted.TimeOfDay));
            var lastOrder = await _orderService.GetLastOrderInfoAsync(coin);

            if (lastOrder.Side == Side.BUY)
            {
                _printerService.PrintMessage(LAST_ORDER_BUY_SIDE);
                var orderUSDValude = decimal.Parse(lastOrder.Price) * decimal.Parse(lastOrder.Quantity);
                await BuySellSpotCoinFirstAsync(coin, orderUSDValude, Side.SELL);
            }
        }

        public async Task BuySellSpotCoinFirstAsync(string symbol, decimal capital, Side side)
        {
            List<CoinShortInfo> fittingCoins = await GetSpotCoinsAsync(symbol);

            while (fittingCoins.Count == 0)
            {
                _printerService.PrintMessage(COIN_NOT_LISTED);
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
                    _printerService.PrintMessage(CANT_SELL);
                    return;
                }

                quantity = decimal.Parse(previousOrderInfo.Quantity);
            }
            else
            {
                quantity = Math.Round(capital / currentPrice, 2);
            }

            var placeOrderResult = await _orderService.PlaceOrderAsync(Category.SPOT, coin.Symbol, side, OrderType.LIMIT, quantity.ToString(), currentPrice.ToString());
            _printerService.PrintMessage(string.Format(PLACED_ORDER_RESULT, placeOrderResult.RetMsg));

            while (placeOrderResult.RetMsg != "OK")
            {
                placeOrderResult = await _orderService.PlaceOrderAsync(Category.SPOT, coin.Symbol, side, OrderType.LIMIT, quantity.ToString(), currentPrice.ToString());
                _printerService.PrintMessage(string.Format(PLACED_ORDER_RESULT, placeOrderResult.RetMsg));
            }

            var openOrdersResult = await _orderService.GetOpenOrdersAsync(coin.Symbol, Category.SPOT);

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

                _printerService.PrintMessage(string.Format(AMEND_ORDER_RESULT, amendOrderResult.RetMsg));

                openOrdersResult = await _orderService.GetOpenOrdersAsync(coin.Symbol, Category.SPOT);
            }

            _printerService.PrintMessage(string.Format(SUCCESSFULY_TRADED_COIN, quantity, coin.Symbol));
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
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_RETRIVE_MARKET_INFO);
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

        private string TrimSmallQuantity(string quantity)
        {
            List<char> trimmedZerosNumber = new List<char>();

            for (int i = quantity.Length - 1; i >= 0; i--)
            {
                if (quantity[i] == '0' && trimmedZerosNumber.Count == 0)
                {
                    continue;
                }

                trimmedZerosNumber.Add(quantity[i]);
            }

            trimmedZerosNumber.Reverse();

            // Trim quantity to be with no more than 4 decimals - 0.0000
            return string.Join("", trimmedZerosNumber.Take(6));
        }
    }
}