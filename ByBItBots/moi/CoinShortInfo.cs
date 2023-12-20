namespace ByBitBots.Moi
{
    public class CoinShortInfo
    {
        public string? Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal FundingRate { get; set; }
        public long NextFunding { get; set; }
        public decimal Leverage { get; set; }
        public decimal Profits => Math.Abs(FundingRate * Leverage) - (0.12m * Leverage);
        public DateTime NextFundingHour => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(NextFunding / 1000);
    }
}
