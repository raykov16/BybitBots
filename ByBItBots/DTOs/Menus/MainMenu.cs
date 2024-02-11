using ByBItBots.Enums;

namespace ByBItBots.DTOs.Menus
{
    public class MainMenu : MenuModel
    {
        public MainMenu()
        {
            this.Title = "SELECT A FUNCTION";
            this.RowBody = '_';
            this.RowLength = 50;
            this.ColumnEdge = "|";
            this.MarginColumns = 3;
            this.HeaderEdges = " ";
            this.Options = new List<string>
            {
                MainMenuOptions.FARM_SPOT_VOLUME.ToString().Replace("_", " "),
                MainMenuOptions.BUY_SPOT_COIN_FIRST.ToString().Replace("_", " "),
                MainMenuOptions.GET_SPOT_COINS_INFO.ToString().Replace("_", " "),
                MainMenuOptions.GET_DERIVATIVES_COINS_INFO.ToString().Replace("_", " "),
                MainMenuOptions.GET_COINS_FOR_FUNDING_TRADING.ToString().Replace("_", " "),
                MainMenuOptions.GET_OPEN_ORDERS.ToString().Replace("_", " "),
                MainMenuOptions.GET_BYBIT_SERVER_TIME.ToString().Replace("_", " "),
                MainMenuOptions.EXIT.ToString().Replace("_", " ")
            };
        }
    }
}
