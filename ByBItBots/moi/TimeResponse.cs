using Newtonsoft.Json;

namespace ByBItBots.moi
{
    public class TimeResponse
    {
        [JsonProperty("timeSecond")]
        public string TimeSecond { get; set; }

        [JsonProperty("timeNano")]
        public string TimeNano { get; set; }
    }
}
