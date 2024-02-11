namespace ByBItBots.Configs
{
    public interface IConfig
    {
        public string ApiKey { get; }
        public string ApiSecret { get; }
        public string NetURL { get; }
        public string RecvWindow { get; }
    }
}
