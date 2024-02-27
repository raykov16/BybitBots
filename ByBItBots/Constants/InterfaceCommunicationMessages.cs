namespace ByBItBots.Constants
{
    public static class InterfaceCommunicationMessages
    {
        public const string USING_TEST_NET = "[USING TESTNET]";
        public const string USING_MAIN_NET = "[USING MAINNET]";

        public const string PRESS_SPECIFIED_BUTTON = "Please press one of the specified buttons";

        public const string BYBIT_TIME = "Bybit time: {0}";

        public const string SERVICE_UNAVAILABLE = "This service is currently unavailable on Testnet";

        public const string OPTION_SELECTED = "[{0} SELECTED]";

        public const string ENTER_COIN = "Enter coin for farming (Example: BTC):";
        public const string ENTER_VALID_COIN = "This coin does not exist. Enter valid coin (Example: BTC):";
        public const string ENTER_COIN_INFORMATION = "Enter a coin to get information (Example: BTC), if you want to see all coins just press Enter (This option is currently unavailable on TestNet):";
        public const string ENTER_COIN_OPEN_TRADES = "Enter a coin to get open trades (Example: BTC), if you want to see all open trades press enter: ";

        public const string NO_OPEN_ORDERS = "No open orders";
        public const string ORDER_FORMAT = "{0}. {1}";

        public const string ENTER_CAPITAL = "Enter your capital (Examples: 100, 200.25):";
        public const string ENTER_VALID_CAPITAL = "Enter valid capital > 0";
        public const string CAPITAL_SET = "Capital set";
        public const string CAPITAL_TO_TRADE = "Capital to trade with: {0}";

        public const string ENTER_VOLUME = "Enter required volume to farm (Examples: 1000, 1000.25):";
        public const string ENTER_VALID_VOLUME = "Enter valid volume to farm (Examples: 1000, 1000.25):";
        public const string VOLUME_SET = "Volume set";
        public const string VOLUME_TO_TRADE = "Required volume: {0}";

        public const string ENTER_INTERVAL = "Enter interval in seconds to wait between requests(Examples: 1, 5):";
        public const string ENTER_VALID_INTERVAL = "Enter valid interval (Examples: 1, 5):";
        public const string INTERVAL_SET = "Interval set";
        public const string INTERVAL_TO_WAIT = "Interval to wait: {0} seconds";

        public const string ENTER_MAX_PRICE_DIFF = "Enter max price percent difference between orders. (Examples 0.1, 1):";
        public const string CAPITAL_LOSE_INFO = "Enter max price percent difference between orders. (Examples 0.1, 1):";
        public const string LONGER_TIME_INFO = "INFO: The lower max price percent difference you set, the longer it will take to executes trades";
        public const string ENTER_VALID_DIFF = "Enter valid price percent difference (Examples: 0.1, 1):";
        public const string DIFF_SET = "Percent difference set";
        public const string MAX_DIFF_PERCENT = "Max price percent difference: {0}%";

        public const string ENTER_MINUTES = "Enter how many minutes are you willing to go without a trade (Examples: 0, 5):";
        public const string BAD_TRADES_INFO = "INFO: higher the minutes, the lower your chances are of getting a bad trade";
        public const string ENTER_VALID_MINUTES = "Enter valid minutes to go without a trade (Examples: 0, 5):";
        public const string MINUTES_SET = "Minutes set";
        public const string MINUTES_TO_WAIT = "Minutes without trade: {0} minutes";

        public const string START_FARMING = "Farming starting...";

        public const string CHOOSE_SIDE = "Choose to buy or sell the coin:";
        public const string BUY_SIDE = "[{0}] Buy";
        public const string SELL_SIDE = "[{0}] Sell";
        public const string TRADING_SIDE = "Trading side: {0}";
        public const string OPENING_TRADE = "Opening a trade...";
    }
}
