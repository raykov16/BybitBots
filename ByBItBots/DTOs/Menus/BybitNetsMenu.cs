using ByBItBots.Enums;

namespace ByBItBots.DTOs.Menus
{
    public class BybitNetsMenu : MenuModel
    {
        public BybitNetsMenu()
        {
            this.Title = "USE TESTNET / MAINNET";
            this.RowBody = '_';
            this.RowLength = 50;
            this.ColumnEdge = "|";
            this.MarginColumns = 3;
            this.HeaderEdges = " ";
            this.Options = new List<string>
            {
                BybitNets.TESTNET.ToString().Replace("_", " "),
                BybitNets.MAINNET.ToString().Replace("_", " ")
            };
        }
    }
}
