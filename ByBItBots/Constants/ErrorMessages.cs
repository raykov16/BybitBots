using static System.Net.Mime.MediaTypeNames;

namespace ByBItBots.Constants
{
    public static class ErrorMessages
    {
        public const string COULD_NOT_RETRIVE_BYBIT_TIME = "Could not retrieve bybit time";
        public const string COULD_NOT_RETRIVE_COIN_INFO = "Could not retrieve coin info";
        public const string COULD_NOT_RETRIVE_DERIVATIVES_COINS = "Could not retrieve derivatives coins";
        public const string COULD_NOT_RETRIVE_MARKET_INFO = "Could not retrieve market info";
        public const string COULD_NOT_RETRIVE_OPEN_ORDERS = "Open orders could not be retrieved";
        public const string COULD_NOT_OPEN_ORDER = "Order could not be placed!";
        public const string COULD_NOT_AMEND_ORDER = "Could not amend order!";
        public const string COULD_NOT_RETRIVE_ORDER_HISTORY = "Order history could not be retrieved";
        public const string TEXT_LENGTH_EXCEEDS_BODY_LENGTH = "The text length ({0}) should not exceed the row's body length ({1})";
    }
}
