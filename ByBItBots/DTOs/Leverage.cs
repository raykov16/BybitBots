using Newtonsoft.Json;

namespace ByBitBots.DTOs
{
    public class Leverage
    {
        [JsonProperty("maxLeverage")]
        public string MaxLeverage { get; set; }
    }
}
