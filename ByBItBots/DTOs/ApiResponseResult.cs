using Newtonsoft.Json;

namespace ByBitBots.DTOs
{
    public class ApiResponseResult<T>
    {
        [JsonProperty("retCode")]
        public string? RetCode { get; set; }
        [JsonProperty("retMsg")]
        public string? RetMsg { get; set; }
        [JsonProperty("result")]
        public T Result { get; set; }
        [JsonProperty("retExtInfo")]
        public RetExtInfo RetExtInfo { get; set; }
        [JsonProperty("time")]
        public string? Time { get; set; }
    }
}
