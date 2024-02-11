using bybit.net.api.ApiServiceImp;
using ByBitBots.DTOs;
using ByBItBots.Services.Interfaces;
using Newtonsoft.Json;

namespace ByBItBots.Services.Implementations
{
    public class BybitTimeService : IBybitTimeService
    {
        private readonly BybitMarketDataService _marketService;
        public BybitTimeService(BybitMarketDataService marketService)
        {
            _marketService = marketService;
        }

        public async Task<DateTime> GetCurrentBybitTimeAsync()
        {
            var bybitTimeInfo = await _marketService.CheckServerTime();
            var bybitTimeObject = JsonConvert.DeserializeObject<ApiResponseResult<TimeResponse>>(bybitTimeInfo);

            if (bybitTimeObject == null)
            {
                throw new InvalidOperationException("Could not retrieve bybit time.");
            }

            return ReadBybitTime(bybitTimeObject.Result.TimeSecond);
        }

        public DateTime ReadBybitTime(int bybitTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(bybitTime);
        }
    }
}
