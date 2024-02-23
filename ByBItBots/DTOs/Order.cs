using Newtonsoft.Json;
using System.Text.Json;

namespace ByBitBots.DTOs
{
    public class Order
    {
        [JsonProperty("orderId")]
        public string? OrderId { get; set; }

        [JsonProperty("orderLinkId")]
        public string? OrderLinkId { get; set; }

        [JsonProperty("blockTradeId")]
        public string? BlockTradeId { get; set; }

        [JsonProperty("symbol")]
        public string? Symbol { get; set; }

        [JsonProperty("price")]
        public string? Price { get; set; }

        [JsonProperty("qty")]
        public string? Qty { get; set; }

        [JsonProperty("side")]
        public string? Side { get; set; }

        [JsonProperty("isLeverage")]
        public string? IsLeverage { get; set; }

        [JsonProperty("positionIdx")]
        public int? PositionIdx { get; set; }

        [JsonProperty("orderStatus")]
        public string? OrderStatus { get; set; }

        [JsonProperty("cancelType")]
        public string? CancelType { get; set; }

        [JsonProperty("rejectReason")]
        public string? RejectReason { get; set; }

        [JsonProperty("avgPrice")]
        public string? AvgPrice { get; set; }

        [JsonProperty("leavesQty")]
        public string? LeavesQty { get; set; }

        [JsonProperty("leavesValue")]
        public string? LeavesValue { get; set; }

        [JsonProperty("cumExecQty")]
        public string? CumExecQty { get; set; }

        [JsonProperty("cumExecValue")]
        public string? CumExecValue { get; set; }

        [JsonProperty("cumExecFee")]
        public string? CumExecFee { get; set; }

        [JsonProperty("timeInForce")]
        public string? TimeInForce { get; set; }

        [JsonProperty("orderType")]
        public string? OrderType { get; set; }

        [JsonProperty("stopOrderType")]
        public string? StopOrderType { get; set; }

        [JsonProperty("orderIv")]
        public string? OrderIv { get; set; }

        [JsonProperty("triggerPrice")]
        public string? TriggerPrice { get; set; }

        [JsonProperty("takeProfit")]
        public string? TakeProfit { get; set; }

        [JsonProperty("stopLoss")]
        public string? StopLoss { get; set; }

        [JsonProperty("tpTriggerBy")]
        public string? TpTriggerBy { get; set; }

        [JsonProperty("slTriggerBy")]
        public string? SlTriggerBy { get; set; }

        [JsonProperty("triggerDirection")]
        public int? TriggerDirection { get; set; }

        [JsonProperty("triggerBy")]
        public string? TriggerBy { get; set; }

        [JsonProperty("lastPriceOnCreated")]
        public string? LastPriceOnCreated { get; set; }

        [JsonProperty("reduceOnly")]
        public bool? ReduceOnly { get; set; }

        [JsonProperty("closeOnTrigger")]
        public bool? CloseOnTrigger { get; set; }

        [JsonProperty("smpType")]
        public string? SmpType { get; set; }

        [JsonProperty("smpGroup")]
        public int? SmpGroup { get; set; }

        [JsonProperty("smpOrderId")]
        public string? SmpOrderId { get; set; }

        [JsonProperty("tpslMode")]
        public string? TpslMode { get; set; }

        [JsonProperty("tpLimitPrice")]
        public string? TpLimitPrice { get; set; }

        [JsonProperty("slLimitPrice")]
        public string? SlLimitPrice { get; set; }

        [JsonProperty("placeType")]
        public string? PlaceType { get; set; }

        [JsonProperty("createdTime")]
        public string? CreatedTime { get; set; }

        [JsonProperty("updatedTime")]
        public string? UpdatedTime { get; set; }

        public override string ToString()
        {
            return $"OrderId: {OrderId}, Coin: {Symbol}, Side: {Side}, Price: {Price}, Quantity: {Qty}, USDT amount: {Math.Round(decimal.Parse(Price) * decimal.Parse(Qty), 2)}";
        }
    }
}
