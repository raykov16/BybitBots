using bybit.net.api.Models;
using ByBitBots.DTOs;
using ByBItBots.Constants;
using ByBItBots.DTOs.Menus;
using ByBItBots.Services.Interfaces;

namespace ByBItBots.Services.Implementations
{
    public class ConsolePrinterService : IPrinterService
    {
        private const string DEFAULT_COLUMN_EDGE = "|";
        private const string DEFAULT_ROW_EDGE = " ";
        private const char DEFAULT_ROW_BODY = '_';
        private const int DEFAULT_ROW_BODY_LENGTH = 48;
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
                printRow(DEFAULT_ROW_BODY_LENGTH, DEFAULT_ROW_BODY); // 50
                printEmptyColumn(DEFAULT_ROW_BODY_LENGTH, DEFAULT_COLUMN_EDGE); // 50
                printColumnWithText($"Coin: {c.Symbol}", DEFAULT_ROW_BODY_LENGTH - DEFAULT_COLUMN_EDGE.Length * 2, 1, DEFAULT_COLUMN_EDGE);
                printColumnWithText($"Price: {c.Price}", DEFAULT_ROW_BODY_LENGTH - DEFAULT_COLUMN_EDGE.Length * 2, 1, DEFAULT_COLUMN_EDGE);

                if (category == Category.LINEAR)
                    printColumnWithText($"Funding rate: {c.FundingRate}", DEFAULT_ROW_BODY_LENGTH - DEFAULT_COLUMN_EDGE.Length * 2, 1, DEFAULT_COLUMN_EDGE);

                printRow(DEFAULT_ROW_BODY_LENGTH, DEFAULT_ROW_BODY, DEFAULT_COLUMN_EDGE);
            }
        }
    
        private void printRow(int rowLength, char rowBody = DEFAULT_ROW_BODY, string rowEdges = DEFAULT_ROW_EDGE)
        {
            string mainRow = rowEdges;
            mainRow += new string(rowBody, rowLength - rowEdges.Length * 2);
            mainRow += rowEdges;

            Console.WriteLine(mainRow);
        }

        private void printColumnWithText(string text, int contentSpace, int times = 1, string columnEdge = DEFAULT_COLUMN_EDGE)
        {
            if (text.Length > contentSpace)
            {
                throw new InvalidOperationException(string.Format(ErrorMessages.TEXT_LENGTH_EXCEEDS_BODY_LENGTH, text.Length, contentSpace));
            }

            var emptySpaces = contentSpace - text.Length;
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


        private void printEmptyColumn(int rowLength, string columnEdges = DEFAULT_COLUMN_EDGE, int times = 1)
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
