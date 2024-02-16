namespace ByBItBots.Constants
{
    public static class InfoMessages
    {
        public const string USING_TEST_NET = "[USING TESTNET]";
        public const string USING_MAIN_NET = "[USING MAINNET]";

        public const string PRESS_SPECIFIED_BUTTON = "Please press one of the specified buttons";

        public const string GOT_OPEN_ORDERS = "Got open orders";
        public const string OPENING_ORDER = "No orders found, opening an order!";
        public const string PLACED_ORDER_RESULT = "Placed order result: {0}";
        public const string AMEND_ORDER_RESULT = "Ammended order result: {0}";
        public const string OPEN_ORDER_PRICE_DIFF = "Open order price difference, changing price!";
        public const string LAST_ORDER_BUY_SIDE = "The last order was on Buy Side. Selling left over quantity.";

        public const string GETTING_PRICE = "Getting price";
        public const string ORDER_PRICE = "Order price: {0}";
        public const string MARKET_PRICE = "Market price: {0}";
        public static string PRICE_DIFF_TOO_LARGE = "Price diff: {0}, sell order will not be placed!";
        public static string PRICE_DIFF_TOO_SMALL = "Price diff: {0}, existing order price will not be changed!";

        public const string PREVIOUS_SIDE = "Previous side: {0}";
        public const string CURRENT_SIDE = "Current side: {0}";

        public const string TIMES_WITHOUT_TRADE = "Times without trade: {0}";
        public const string RESETING_TIMES_WITHOUT_TRADE = "Reseting times without trade";

        public const string QUANTITY_TO_SELL = "Quantity to sell: {0}, Previous bought quantity: {1}";
        public const string QUANTITY_TO_BUY = "Quantity to buy: {0}, Previous sold quantity: {1}";

        public const string WAITING_SECONDS = "waiting {0} seconds";
        public const string WAITED_SECONDS = "waited {0} seconds, starting again";

        public const string ACCUMULATED_VOLUME = "Accumulated volume : {0} / {1}";

        public const string SUCCESSFULY_ACCUMULATED_VOLUME = "Successfuly accumulated {0} volume for {1}!";
        public const string SUCCESSFULY_TRADED_COIN = "Successfuly traded {0} {1}";

        public const string BYBIT_TIME = "Bybit time: {0}";

        public const string COIN_NOT_LISTED = "Coin not listed yet!";

        public const string CANT_SELL = "Can not sell something that you have not bought. Exiting...";
    }
}
