using ByBitBots.Moi;
using Newtonsoft.Json;

namespace ByBItBots.Results
{
    public class ResultOpenOrders
    {
        [JsonProperty("list")]
        public List<Order> List { get; set; }

        [JsonProperty("nextPageCursor")]
        public string NextPageCursor { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }
}
