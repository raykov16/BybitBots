using bybit.net.api.Models;
using ByBitBots.DTOs;
using ByBItBots.DTOs.Menus;

namespace ByBItBots.Services.Interfaces
{
    public interface IPrinterService
    {
        void PrintMenu(MenuModel menu);
        void PrintMessage(string message);
        void PrintCoinInfo(List<CoinShortInfo> fittingCoin, Category category);
    }
}
