using Newtonsoft.Json;

namespace ByBitBots.DTOs
{
    public class TimeResponse
    {
        [JsonProperty("timeSecond")]
        public int TimeSecond { get; set; }

        [JsonProperty("timeNano")]
        public string TimeNano { get; set; }
    }
}
