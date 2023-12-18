using Newtonsoft.Json;

namespace ByBItBots.moi
{
    public class TimeResponse
    {
        [JsonProperty("timeSecond")]
        public int TimeSecond { get; set; }

        [JsonProperty("timeNano")]
        public string TimeNano { get; set; }
    }
}
