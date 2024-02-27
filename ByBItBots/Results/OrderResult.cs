using Newtonsoft.Json;

namespace ByBItBots.Results
{
    public class OrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("orderLinkId")]
        public string OrderLinkId { get; set; }
    }
}
