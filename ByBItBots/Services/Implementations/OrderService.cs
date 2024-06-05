using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models.Trade;
using bybit.net.api.Models;
using ByBItBots.Results;
using Newtonsoft.Json;
using ByBItBots.Services.Interfaces;
using ByBitBots.DTOs;
using ByBItBots.Constants;

namespace ByBItBots.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly BybitPositionService _positionService;
        private readonly BybitTradeService _tradeService;
        private readonly ICoinDataService _coinDataService;

        public OrderService(
            BybitPositionService positionService
            , BybitTradeService tradeService
            , ICoinDataService coinDataService)
        {
            _positionService = positionService;
            _tradeService = tradeService;
            _coinDataService = coinDataService;
        }

        public async Task<(List<OrderRequest> OpenRequests, List<OrderRequest> CloseRequests)> CreateOrdersAsync(List<CoinShortInfo> coins, decimal capitalPerCoin)
        {
            List<OrderRequest> openRequests = new List<OrderRequest>();
            List<OrderRequest> closeRequests = new List<OrderRequest>();

            foreach (var coin in coins.OrderByDescending(c => c.Profits))
            {
                var currentPrice = await _coinDataService.GetCurrentPriceAsync(coin.Symbol, Category.SPOT);
                var quantityRaw = capitalPerCoin / currentPrice;
                var quantity = Math.Round(quantityRaw, 3);

                if (openRequests.Count < 10)
                {
                    var leverageResponse = await _positionService.SetPositionLeverage(Category.LINEAR, coin.Symbol, $"{coin.Leverage}", $"{coin.Leverage}");

                    var tpPrice = CalculateTPSLPrice(2, coin.Leverage, currentPrice, true);
                    var slPrice = CalculateTPSLPrice(2, coin.Leverage, currentPrice, false);

                    var openOrder = new OrderRequest
                    {
                        Category = Category.LINEAR,
                        Symbol = coin.Symbol,
                        OrderType = OrderType.MARKET,
                        Side = coin.FundingRate > 0 ? Side.SELL : Side.BUY,
                        Qty = quantity.ToString(),
                        TakeProfit = tpPrice.ToString(),
                        StopLoss = slPrice.ToString()
                    };

                    openRequests.Add(openOrder);
                }

                if (closeRequests.Count < 10)
                {
                    var closeOrder = new OrderRequest
                    {
                        Category = Category.LINEAR,
                        Symbol = coin.Symbol,
                        OrderType = OrderType.MARKET,
                        Side = coin.FundingRate > 0 ? Side.BUY : Side.SELL,
                        Qty = quantity.ToString(),
                        Price = "0",
                        ReduceOnly = true
                    };

                    closeRequests.Add(closeOrder);
                }
                else
                {
                    break;
                }
            }

            return (OpenRequests: openRequests, CloseRequests: closeRequests);
        }

        public async Task<ApiResponseResult<ResultOpenOrders>> GetOpenOrdersAsync(string symbol, Category category)
        {
            var openOrders = await _tradeService.GetOpenOrders(category, symbol: symbol);
            var openOrdersResult = JsonConvert.DeserializeObject<ApiResponseResult<ResultOpenOrders>>(openOrders);

            if (openOrdersResult == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_RETRIVE_OPEN_ORDERS);
            }

            return openOrdersResult;
        }

        public async Task<PreviousOrderInfo> GetLastOrderInfoAsync(string symbol)
        {
            var orderHistory = await _tradeService.GetOrdersHistory(Category.SPOT, symbol, limit: 1);
            var orderHistoryResult = JsonConvert.DeserializeObject<ApiResponseResult<ResultOpenOrders>>(orderHistory);

            if (orderHistoryResult == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_RETRIVE_ORDER_HISTORY);
            }

            if (orderHistoryResult.Result.List.Count == 0)
            {
                return null;
            }

            var previousOrderInfo = new PreviousOrderInfo
            {
                Side = orderHistoryResult.Result.List[0].Side,
                Price = orderHistoryResult.Result.List[0].AvgPrice,
                Quantity = orderHistoryResult.Result.List[0].Qty
            };

            return previousOrderInfo;
        }

        public async Task<ApiResponseResult<EmptyResult>> PlaceOrderAsync(Category category, string coinSymbol, Side side, OrderType orderType, string quantity, string currentPrice)
        {
            var placeOrder = await _tradeService.PlaceOrder(category, coinSymbol, side, orderType, quantity, currentPrice);
            var result = JsonConvert.DeserializeObject<ApiResponseResult<EmptyResult>>(placeOrder);

            if (result == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_OPEN_ORDER);
            }

            return result;
        }

        public async Task<ApiResponseResult<EmptyResult>> PlaceOrderAsync(Category category, string coinSymbol, Side side, OrderType orderType,
                                                                         string quantity, string currentPrice, string takeProfit, string stopLoss)
        {
            var placeOrder = await _tradeService.PlaceOrder(category, coinSymbol, side, orderType, quantity, currentPrice, takeProfit: takeProfit, stopLoss: stopLoss);
            var result = JsonConvert.DeserializeObject<ApiResponseResult<EmptyResult>>(placeOrder);

            if (result == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_OPEN_ORDER);
            }

            return result;
        }

        public async Task<ApiResponseResult<OrderResult>> AmendOrderAsync(Category category, string symbol, string orderId, string price)
        {
            var amendOrder = await _tradeService.AmendOrder(category, symbol, orderId: orderId, price: price);
            var amendOrderResult = JsonConvert.DeserializeObject<ApiResponseResult<OrderResult>>(amendOrder);

            if (amendOrderResult == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_AMEND_ORDER);
            }

            return amendOrderResult;
        }

        public async Task<ApiResponseResult<OrderResult>> AmendOrderAsync(Category category, string symbol, string orderId, string quantity, string price)
        {
            var amendOrder = await _tradeService.AmendOrder(category, symbol, orderId: orderId, qty: quantity, price: price);
            var amendOrderResult = JsonConvert.DeserializeObject<ApiResponseResult<OrderResult>>(amendOrder);

            if (amendOrderResult == null)
            {
                throw new InvalidOperationException(ErrorMessages.COULD_NOT_AMEND_ORDER);
            }

            return amendOrderResult;
        }

        private decimal CalculateTPSLPrice(decimal percentageLose, decimal leverage, decimal coinPrice, bool isTakeProfit)
        {
            decimal coinPercentPriceDrop = percentageLose / leverage / 100;

            decimal coinCashPriceDrop = coinPrice * coinPercentPriceDrop;

            if (isTakeProfit)
            {
                decimal takeProfit = coinPrice + coinCashPriceDrop;

                return takeProfit;
            }

            decimal stopLoss = coinPrice - coinCashPriceDrop;

            return stopLoss;
        }
    }
}
