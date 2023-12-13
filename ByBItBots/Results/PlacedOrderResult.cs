using Newtonsoft.Json;

namespace ByBItBots.Results
{
    public class PlacedOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("orderLinkId")]
        public string OrderLinkId { get; set; }
    }
}
