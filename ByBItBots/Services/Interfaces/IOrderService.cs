using bybit.net.api.Models;
using bybit.net.api.Models.Trade;
using ByBitBots.DTOs;
using ByBItBots.Results;

namespace ByBItBots.Services.Interfaces
{
    public interface IOrderService
    {
        Task<(List<OrderRequest> OpenRequests, List<OrderRequest> CloseRequests)> CreateOrdersAsync(List<CoinShortInfo> coins, decimal capitalPerCoin);

        /// <summary>
        /// Gets all open orders for a specific coin. If a coin is not specified all open orders will be returned.
        /// </summary>
        /// <param name="symbol">The symbol of the coin. Example - BTCUSDT</param>
        /// <returns></returns>
        Task<ApiResponseResult<ResultOpenOrders>> GetOpenOrdersAsync(string symbol, Category category);

        Task<PreviousOrderInfo> GetLastOrderInfoAsync(string symbol);
        Task<ApiResponseResult<EmptyResult>> PlaceOrderAsync(Category category, string coinSymbol, Side side, OrderType orderType, string quantity, string currentPrice);
        Task<ApiResponseResult<EmptyResult>> PlaceOrderAsync(Category category, string coinSymbol, Side side, OrderType orderType, string quantity,
        string currentPrice, string takeProfit, string stopLoss);
        Task<ApiResponseResult<OrderResult>> AmendOrderAsync(Category category, string symbol, string orderId, string price);
        Task<ApiResponseResult<OrderResult>> AmendOrderAsync(Category category, string symbol, string orderId, string quantity, string price);
    }
}
