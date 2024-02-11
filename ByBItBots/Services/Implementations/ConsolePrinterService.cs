using bybit.net.api.Models;
using ByBitBots.DTOs;
using ByBItBots.DTOs.Menus;
using ByBItBots.Services.Interfaces;

namespace ByBItBots.Services.Implementations
{
    public class ConsolePrinterService : IPrinterService
    {
        public void PrintMenu(MenuModel menu)
        {
            printRow(menu.RowLength, menu.RowBody, menu.HeaderEdges);
            printEmptyColumn(menu.RowLength, menu.ColumnEdge, menu.MarginColumns);
            printColumnWithText(menu.Title, menu.RowBodyLength, 1, menu.ColumnEdge);
            printEmptyColumn(menu.RowLength, menu.ColumnEdge);

            for (int i = 0; i < menu.Options.Count; i++)
            {
                var option = $"[{i + 1}] {menu.Options[i]}";
                printColumnWithText(option, menu.RowBodyLength, 1, menu.ColumnEdge);
                printEmptyColumn(menu.RowLength, menu.ColumnEdge);
            }

            printEmptyColumn(menu.RowLength, menu.ColumnEdge, menu.MarginColumns);
            printRow(menu.RowLength, menu.RowBody, menu.ColumnEdge);
        }

        public void PrintMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void PrintCoinInfo(List<CoinShortInfo> fittingCoin, Category category)
        {
            foreach (var c in fittingCoin)
            {
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine($"Coin: {c.Symbol}");
                Console.WriteLine($"Price: {c.Price}");
                if(category == Category.LINEAR)
                    Console.WriteLine($"Funding rate: {c.FundingRate}");
                Console.WriteLine("-------------------------------------------");
            }
        }
    
        private void printRow(int rowLength, char rowBody = ' ', string rowEdges = "")
        {
            string mainRow = rowEdges;
            mainRow += new string(rowBody, rowLength - rowEdges.Length * 2);
            mainRow += rowEdges;

            Console.WriteLine(mainRow);
        }

        private void printColumnWithText(string text, int rowBodyLength, int times = 1, string columnEdge = "")
        {
            if (text.Length > rowBodyLength)
            {
                throw new InvalidOperationException($"The text length ({text.Length}) should not exceed the row's body length ({rowBodyLength})");
            }

            var emptySpaces = rowBodyLength - text.Length;
            var leftSideEmptySpaces = 0;
            var rightSideEmptySpaces = 0;

            if (emptySpaces % 2 == 0)
            {
                leftSideEmptySpaces = emptySpaces / 2;
                rightSideEmptySpaces = emptySpaces / 2;
            }
            else
            {
                leftSideEmptySpaces = emptySpaces / 2 + 1;
                rightSideEmptySpaces = emptySpaces / 2;
            }

            var column = $"{columnEdge}{new string(' ', leftSideEmptySpaces)}{text}{new string(' ', rightSideEmptySpaces)}{columnEdge}";

            for (var i = 0; i < times; i++)
            {
                Console.WriteLine(column);
            }
        }


        private void printEmptyColumn(int rowLength, string columnEdges = "", int times = 1)
        {
            string column = columnEdges;
            column += new string(' ', rowLength - columnEdges.Length * 2);
            column += columnEdges;

            for (int i = 0; i < times; i++)
            {
                Console.WriteLine(column);
            }
        }

    }
}
