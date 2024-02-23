using bybit.net.api.Models;
using bybit.net.api.Models.Trade;
using ByBitBots.DTOs;
using ByBItBots.Configs;
using ByBItBots.DTOs.Menus;
using ByBItBots.Enums;
using ByBItBots.Helpers.Interfaces;
using ByBItBots.Services.Interfaces;

namespace ByBItBots.Helpers.Implementations
{
    public class BotConsoleInterfaceHost : IBotInterfaceHost
    {
        private readonly IPrinterService _printerService;
        private readonly ISpotTradingService _spotTradingService;
        private readonly IFundingTradingService _fundingTradingService;
        private readonly IBybitTimeService _bybitTimeService;
        private readonly IOrderService _orderService;

        public BotConsoleInterfaceHost(IPrinterService printerService
            , ISpotTradingService spotTradingService
            , IFundingTradingService fundingTradingService
            , IBybitTimeService bybitTimeService
            , IOrderService orderService)
        {
            _printerService = printerService;
            _spotTradingService = spotTradingService;
            _fundingTradingService = fundingTradingService;
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
                    _printerService.PrintMessage("Please press one of the specified keys");
                    continue;
                }

                var canParse = Enum.TryParse(keyAsInt.ToString(), out MainMenuOptions menuOption);

                if (!canParse)
                {
                    Console.Clear();
                    _printerService.PrintMessage("Please press one of the specified keys");
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
                            _printerService.PrintMessage("This service is currently unavailable on Testnet");
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
                    case MainMenuOptions.EXIT:
                        shouldExit = true;
                        break;
                    default:
                        Console.Clear();
                        _printerService.PrintMessage("Please press one of the specified keys");
                        continue;
                }
            }

            StopBot();
        }

        private async Task ExecuteFunction(MainMenuOptions menuOption, Func<Task> function)
        {
            Console.Clear();
            _printerService.PrintMessage($"[{menuOption.ToString().Replace("_", " ")} SELECTED]");
            await function.Invoke();
        }

        private async Task ExecuteGetOpenOrdersAsync()
        {
            _printerService.PrintMessage("Enter a coin to get open trades (Example: BTCUSDT), if you want to see all open trades press enter: ");

            var coin = Console.ReadLine();
            var openOrdersResult = await _orderService.GetOpenOrdersAsync(coin);

            if (openOrdersResult.Result.List.Count == 0)
            {
                _printerService.PrintMessage("No open orders");
            }
            else
            {
                for (int i = 0; i < openOrdersResult.Result.List.Count; i++)
                {
                    var order = openOrdersResult.Result.List[i];
                    _printerService.PrintMessage($"{i + 1}. {order}");
                }
            }
        }

        private async Task ExecuteGetBybitServerTime()
        {
            var bybitTime = await _bybitTimeService.GetCurrentBybitTimeAsync();
            _printerService.PrintMessage($"Server time: {bybitTime}");
        }

        private async Task ExecuteGetCoinsForFundingTradingAsync()
        {
            var coins = await _fundingTradingService.GetCoinsForFundingTradingAsync();

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
            var getCoinsFunc = (string coin) => _fundingTradingService.GetDerivativesCoinsAsync(coin);

            await GetMarketCoins(printAction, getCoinsFunc);
        }

        private async Task ExecuteFarmSpotVolume()
        {
            var coin = await EnterCoin();

            _printerService.PrintMessage("Enter your capital (Examples: 100, 200.25):");
            decimal capital = EnterValue<decimal>("Enter valid capital > 0", "Capital set");
            _printerService.PrintMessage($"Capital to trade with: {capital}" + Environment.NewLine);

            _printerService.PrintMessage("Enter required volume to farm (Examples: 1000, 1000.25):");
            decimal requiredVolume = EnterValue<decimal>("Enter valid volume to farm (Examples: 1000, 1000.25):", "Volume set");
            _printerService.PrintMessage($"Required volume: {requiredVolume}" + Environment.NewLine);

            _printerService.PrintMessage("Enter interval in seconds to wait between requests(Examples: 1, 5):");
            int requestInterval = EnterValue<int>("Enter valid interval (Examples: 1, 5):", "Interval set");
            _printerService.PrintMessage($"Interval to wait: {requestInterval} seconds" + Environment.NewLine);

            _printerService.PrintMessage("Enter max price percent difference between orders. (Examples 0.1, 1):");
            _printerService.PrintMessage("INFO: This will determine what % of your capital you lose on bad trades");
            _printerService.PrintMessage("INFO: The lower max price percent difference you set, the longer it will take to executes trades");

            decimal maxPricePercentDiff = EnterValue<decimal>("Enter valid price percent difference (Examples: 0.1, 1):", "Percent difference set");
            _printerService.PrintMessage($"Max price percent difference: {maxPricePercentDiff}%" + Environment.NewLine);
            maxPricePercentDiff /= 100;

            _printerService.PrintMessage("Enter how many minutes are you willing to go without a trade (Examples: 0, 5):");
            _printerService.PrintMessage("INFO: higher the minutes, the lower your chances are of getting a bad trade");

            int minutesWithoutTrade = EnterValue<int>("Enter valid minutes to go without a trade (Examples: 0, 5):", "Minutes set", true);
            _printerService.PrintMessage($"Minutes without trade: {minutesWithoutTrade} minutes" + Environment.NewLine);

            Console.Clear();
            _printerService.PrintMessage("Farming starting...");

            await _spotTradingService.FarmSpotVolumeAsync(coin, capital, requiredVolume, requestInterval, maxPricePercentDiff, minutesWithoutTrade);
        }

        private async Task ExecuteBuySpotCoinFirst()
        {
            string coin = await EnterCoin();

            _printerService.PrintMessage("Enter your capital (Examples: 100, 200.25):");
            decimal capital = EnterValue<decimal>("Enter valid capital > 0", "Capital set");
            _printerService.PrintMessage($"Capital to trade with: {capital}" + Environment.NewLine);

            _printerService.PrintMessage("Choose to buy or sell the coin:");
            _printerService.PrintMessage("[1] Buy");
            _printerService.PrintMessage("[2] Sell");
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
                    _printerService.PrintMessage("Press one of the specified buttons");
                }
            }

            _printerService.PrintMessage($"Trading side: {side}" + Environment.NewLine);
            _printerService.PrintMessage($"Opening a trade...");

            await _spotTradingService.BuySellSpotCoinFirstAsync(coin, capital, side);
        }

        private async Task<string> EnterCoin()
        {
            _printerService.PrintMessage("Enter coin for farming (Example: BTCUSDT):");
            string coin = Console.ReadLine();

            bool isCoinValid = false;

            while (!isCoinValid)
            {
                try
                {
                    var coinsInfo = await _spotTradingService.GetSpotCoinsAsync(coin);
                    _printerService.PrintCoinInfo(coinsInfo, Category.SPOT);
                    isCoinValid = true;
                }
                catch (Exception)
                {
                    _printerService.PrintMessage("This coin does not exist. Enter new coin (Example: BTCUSDT):");
                    coin = Console.ReadLine();
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
            _printerService.PrintMessage("Enter a coin to get information (Example: BTCUSDT), if you want to see all coins just press Enter (This option is currently unavailable on TestNet):");

            bool isCoinValid = false;
            string coin = Console.ReadLine();

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
                    _printerService.PrintMessage("This coin does not exist. Enter new coin (Example: BTCUSDT):");
                    coin = Console.ReadLine();
                }
            }
        }
        #endregion Private methods
    }
}
