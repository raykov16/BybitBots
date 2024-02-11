using ByBitBots.DTOs;
using Newtonsoft.Json;

namespace ByBItBots.Results
{
    public class ResultCoinInfo
    {
        [JsonProperty("category")]
        public string? Category { get; set; }
        [JsonProperty("list")]
        public List<CoinInfo> List { get; set; }
    }
}
