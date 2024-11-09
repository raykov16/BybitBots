namespace ByBItBots.Configs
{
    public class SmurfMainnetConfig : IConfig
    {
        public SmurfMainnetConfig()
        {
            ApiKey = ConfigConstants.SmurfApiKey;
            ApiSecret = ConfigConstants.SmurfApiSecret;
            NetURL = ConfigConstants.MainNetURL;
            RecvWindow = ConfigConstants.MainNetRecvWindow;
        }

        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string NetURL { get; set; }
        public string RecvWindow { get; set; }
    }
}
