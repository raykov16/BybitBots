using Newtonsoft.Json;

namespace ByBitBots.Moi
{
    public class Leverage
    {
        [JsonProperty("maxLeverage")]
        public string MaxLeverage { get; set; }
    }
}
