using ByBItBots.Configs;

namespace ByBItBots.Helpers.Interfaces
{
    public interface IBotInterfaceHost
    {
        Task StartBot(IConfig config);
        void StopBot();
    }
}
