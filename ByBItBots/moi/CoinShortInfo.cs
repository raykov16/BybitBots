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
    }
}
