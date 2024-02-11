namespace ByBItBots.Services.Interfaces
{
    public interface IBybitTimeService
    {
        /// <summary>
        /// Gets the current time on the Bybit Server
        /// </summary>
        /// <returns></returns>
        Task<DateTime> GetCurrentBybitTimeAsync();
        DateTime ReadBybitTime(int bybitTime);
    }
}
