using bybit.net.api.Models;
using bybit.net.api.Models.Trade;
using ByBitBots.DTOs;
using ByBItBots.Configs;
using ByBItBots.DTOs.Menus;
using ByBItBots.Enums;
using ByBItBots.Helpers.Interfaces;
using ByBItBots.Services.Interfaces;
using static ByBItBots.Constants.InterfaceCommunicationMessages;

namespace ByBItBots.Helpers.Implementations
{
    public class BotConsoleInterfaceHost : IBotInterfaceHost
    {
        private readonly IPrinterService _printerService;
        private readonly ISpotTradingService _spotTradingService;
        private readonly IDerivativesTradingService _derivativesTradingService;
        private readonly IBybitTimeService _bybitTimeService;
        private readonly IOrderService _orderService;

        public BotConsoleInterfaceHost(IPrinterService printerService
            , ISpotTradingService spotTradingService
            , IDerivativesTradingService fundingTradingService
            , IBybitTimeService bybitTimeService
            , IOrderService orderService)
        {
            _printerService = printerService;
            _spotTradingService = spotTradingService;
            _derivativesTradingService = fundingTradingService;
            _bybitTimeService = bybitTimeService;
            _orderService = orderService;
        }

        public async Task StartBot(IConfig config)
        {
            await ChoseAnOption(config);
        }

        public void StopBot()
        {
            Environment.Exit(0);
        }

        #region Private methods
        private async Task ChoseAnOption(IConfig config)
        {
            bool shouldExit = false;
            while (!shouldExit)
            {
                _printerService.PrintMenu(new MainMenu());

                var userChoice = Console.ReadKey(true);
                var isOptionVlid = int.TryParse(userChoice.KeyChar.ToString(), out int keyAsInt);

                if (!isOptionVlid)
                {
                    Console.Clear();
                    _printerService.PrintMessage(PRESS_SPECIFIED_BUTTON);
                    continue;
                }

                var canParse = Enum.TryParse(keyAsInt.ToString(), out MainMenuOptions menuOption);

                if (!canParse)
                {
                    Console.Clear();
                    _printerService.PrintMessage(PRESS_SPECIFIED_BUTTON);
                    continue;
                }

                switch (menuOption)
                {
                    case MainMenuOptions.FARM_SPOT_VOLUME:
                        await ExecuteFunction(menuOption, () => ExecuteFarmSpotVolume());
                        break;
                    case MainMenuOptions.BUY_SPOT_COIN_FIRST:
                        await ExecuteFunction(menuOption, () => ExecuteBuySpotCoinFirst());
                        break;
                    case MainMenuOptions.GET_SPOT_COINS_INFO:
                        await ExecuteFunction(menuOption, () => ExecuteGetSpotCoinsAsync());
                        break;
                    case MainMenuOptions.GET_DERIVATIVES_COINS_INFO:
                        await ExecuteFunction(menuOption, () => ExecuteGetDerivativesCoinsAsync());
                        break;
                    case MainMenuOptions.GET_COINS_FOR_FUNDING_TRADING:

                        if (config.NetURL == ConfigConstants.TestNetURL)
                        {
                            _printerService.PrintMessage(SERVICE_UNAVAILABLE);
                            break;
                        }

                        await ExecuteFunction(menuOption, () => ExecuteGetCoinsForFundingTradingAsync());
                        break;
                    case MainMenuOptions.GET_OPEN_ORDERS:
                        await ExecuteFunction(menuOption, () => ExecuteGetOpenOrdersAsync());
                        break;
                    case MainMenuOptions.GET_BYBIT_SERVER_TIME:
                        await ExecuteFunction(menuOption, () => ExecuteGetBybitServerTime());
                        break;
                    case MainMenuOptions.SCALP_VOLATILE_MOVEMENTS:
                        await ExecuteFunction(menuOption, () => ExecuteScalpVolatileMovements());
                        break;
                    case MainMenuOptions.EXIT:
                        shouldExit = true;
                        break;
                    default:
                        Console.Clear();
                        _printerService.PrintMessage(PRESS_SPECIFIED_BUTTON);
                        continue;
                }
            }

            StopBot();
        }

      

        private async Task ExecuteFunction(MainMenuOptions menuOption, Func<Task> function)
        {
            Console.Clear();
            _printerService.PrintMessage(string.Format(OPTION_SELECTED, menuOption.ToString().Replace("_", " ")));
            await function.Invoke();
        }

        private async Task ExecuteGetOpenOrdersAsync()
        {
            _printerService.PrintMessage(ENTER_COIN_OPEN_TRADES);

            var coin = await EnterCoinAsync(true, false);

            var openOrdersResult = await _orderService.GetOpenOrdersAsync(coin, Category.SPOT);

            if (openOrdersResult.Result.List.Count == 0)
            {
                _printerService.PrintMessage(NO_OPEN_ORDERS);
            }
            else
            {
                for (int i = 0; i < openOrdersResult.Result.List.Count; i++)
                {
                    var order = openOrdersResult.Result.List[i];
                    _printerService.PrintMessage(string.Format(ORDER_FORMAT,i + 1, order));
                }
            }
        }

        private async Task ExecuteGetBybitServerTime()
        {
            var bybitTime = await _bybitTimeService.GetCurrentBybitTimeAsync();
            _printerService.PrintMessage(string.Format(BYBIT_TIME, bybitTime));
        }

        private async Task ExecuteGetCoinsForFundingTradingAsync()
        {
            var coins = await _derivativesTradingService.GetCoinsForFundingTradingAsync();

            _printerService.PrintCoinInfo(coins, Category.LINEAR);
        }

        private async Task ExecuteGetSpotCoinsAsync()
        {
            var printAction = (List<CoinShortInfo> coins) => _printerService.PrintCoinInfo(coins, Category.SPOT);
            var getCoinsFunc = (string coin) => _spotTradingService.GetSpotCoinsAsync(coin);

            await GetMarketCoins(printAction, getCoinsFunc);
        }

        private async Task ExecuteGetDerivativesCoinsAsync()
        {
            var printAction = (List<CoinShortInfo> coins) => _printerService.PrintCoinInfo(coins, Category.LINEAR);
            var getCoinsFunc = (string coin) => _derivativesTradingService.GetDerivativesCoinsAsync(coin);

            await GetMarketCoins(printAction, getCoinsFunc);
        }

        private async Task ExecuteFarmSpotVolume()
        {
            var coin = await EnterCoinAsync();

            _printerService.PrintMessage(ENTER_CAPITAL);
            decimal capital = EnterValue<decimal>(ENTER_VALID_CAPITAL, CAPITAL_SET);
            _printerService.PrintMessage(string.Format(CAPITAL_TO_TRADE, capital) + Environment.NewLine);

            _printerService.PrintMessage(ENTER_VOLUME);
            decimal requiredVolume = EnterValue<decimal>(ENTER_VALID_VOLUME, VOLUME_SET);
            _printerService.PrintMessage(string.Format(VOLUME_TO_TRADE, requiredVolume) + Environment.NewLine);

            _printerService.PrintMessage(ENTER_INTERVAL);
            int requestInterval = EnterValue<int>(ENTER_VALID_INTERVAL, INTERVAL_SET);
            _printerService.PrintMessage(string.Format(INTERVAL_TO_WAIT, requestInterval) + Environment.NewLine);

            _printerService.PrintMessage(ENTER_MAX_PRICE_DIFF);
            _printerService.PrintMessage(CAPITAL_LOSE_INFO);
            _printerService.PrintMessage(LONGER_TIME_INFO);

            decimal maxPricePercentDiff = EnterValue<decimal>(ENTER_VALID_DIFF, DIFF_SET);
            _printerService.PrintMessage(string.Format(MAX_DIFF_PERCENT, maxPricePercentDiff) + Environment.NewLine);
            maxPricePercentDiff /= 100;

            _printerService.PrintMessage(ENTER_MINUTES);
            _printerService.PrintMessage(BAD_TRADES_INFO);

            int minutesWithoutTrade = EnterValue<int>(ENTER_VALID_MINUTES, MINUTES_SET, true);
            _printerService.PrintMessage(string.Format(MINUTES_TO_WAIT, minutesWithoutTrade) + Environment.NewLine);

            Console.Clear();
            _printerService.PrintMessage(START_FARMING);

            await _spotTradingService.FarmSpotVolumeAsync(coin, capital, requiredVolume, requestInterval, maxPricePercentDiff, minutesWithoutTrade);
        }

        private async Task ExecuteBuySpotCoinFirst()
        {
            string coin = await EnterCoinAsync();

            _printerService.PrintMessage(ENTER_CAPITAL);
            decimal capital = EnterValue<decimal>(ENTER_VALID_CAPITAL, CAPITAL_SET);
            _printerService.PrintMessage(string.Format(CAPITAL_TO_TRADE, capital) + Environment.NewLine);

            _printerService.PrintMessage(CHOOSE_SIDE);
            _printerService.PrintMessage(string.Format(BUY_SIDE, 1));
            _printerService.PrintMessage(string.Format(SELL_SIDE, 2));
            Side side;
            bool choiceValid = false;

            while (!choiceValid)
            {
                var userChoice = Console.ReadKey(true);

                if (userChoice.Key == ConsoleKey.D1)
                {
                    choiceValid = true;
                    side = Side.BUY;
                }
                else if (userChoice.Key == ConsoleKey.D2)
                {
                    choiceValid = true;
                    side = Side.SELL;
                }
                else
                {
                    _printerService.PrintMessage(PRESS_SPECIFIED_BUTTON);
                }
            }

            _printerService.PrintMessage(string.Format(TRADING_SIDE, side) + Environment.NewLine);
            _printerService.PrintMessage(OPENING_TRADE);

            await _spotTradingService.BuySellSpotCoinFirstAsync(coin, capital, side);
        }

        private async Task<string> EnterCoinAsync(bool allowEmpty = false, bool printCoin = true)
        {
            _printerService.PrintMessage(ENTER_COIN);
            string coin = Console.ReadLine();

            if (!allowEmpty || coin != string.Empty)
                coin += "USDT";

            bool isCoinValid = false;

            while (!isCoinValid)
            {
                try
                {
                    var coinsInfo = await _spotTradingService.GetSpotCoinsAsync(coin);

                    if(printCoin)
                    _printerService.PrintCoinInfo(coinsInfo, Category.SPOT);

                    isCoinValid = true;
                }
                catch (Exception)
                {
                    _printerService.PrintMessage(ENTER_VALID_COIN);
                    coin = Console.ReadLine();

                    if (!allowEmpty)
                        coin += "USDT";
                }
            }

            return coin;
        }

        private T EnterValue<T>(string correctionMessage, string successMessage, bool allowDefaultValue = false)
        {
            T value;

            try
            {
                var input = Console.ReadLine();
                value = (T)Convert.ChangeType(input, typeof(T));

                if (Comparer<T>.Default.Compare(value, default) < 0 || (Comparer<T>.Default.Compare(value, default) == 0 && !allowDefaultValue))
                {
                    _printerService.PrintMessage(correctionMessage);
                    return EnterValue<T>(correctionMessage, successMessage, allowDefaultValue);
                }
                else
                {
                    _printerService.PrintMessage(successMessage);
                    return value;
                }
            }
            catch (Exception)
            {
                _printerService.PrintMessage(correctionMessage);
                return EnterValue<T>(correctionMessage, successMessage, allowDefaultValue);
            }
        }

        private async Task GetMarketCoins(Action<List<CoinShortInfo>> printAction, Func<string, Task<List<CoinShortInfo>>> getCoinsFunc)
        {
            _printerService.PrintMessage(ENTER_COIN_INFORMATION);

            bool isCoinValid = false;
            string coin = Console.ReadLine();

            if (coin != string.Empty)
                coin += "USDT";

            while (!isCoinValid)
            {
                try
                {
                    var coinsInfo = await getCoinsFunc.Invoke(coin);
                    printAction.Invoke(coinsInfo);
                    isCoinValid = true;
                }
                catch (Exception)
                {
                    _printerService.PrintMessage(ENTER_VALID_COIN);
                    coin = Console.ReadLine();

                    if (coin != string.Empty)
                        coin += "USDT";
                }
            }
        }

        private async Task ExecuteScalpVolatileMovements()
        {
            Console.WriteLine("Enter coin");
            string coin = Console.ReadLine();
            Console.WriteLine("Enter capital");
            decimal capital = decimal.Parse(Console.ReadLine());
            Console.WriteLine("Enter move start %");
            decimal moveStartPercent = decimal.Parse(Console.ReadLine());
            Console.WriteLine("Enter whole move %");
            decimal wholeMovePercent = decimal.Parse(Console.ReadLine());
            Console.WriteLine("Enter seconds between updates");
            int secontsBetweenUpdate = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter trade leverage");
            int leverage = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter preset bottom - -1 for none");
            decimal presetBottom = decimal.Parse(Console.ReadLine());
            Console.WriteLine("Enter decimal points for price formating - 0/1/2/3..");
            int decimals = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter required price multiplication: 0 for none, otherwise 10/100");
            int multiple = int.Parse(Console.ReadLine());

            await _derivativesTradingService.ScalpLongsAsync(coin, capital, moveStartPercent, wholeMovePercent, secontsBetweenUpdate, leverage, decimals, multiple, presetBottom, true);
        }
        #endregion Private methods
    }
}
