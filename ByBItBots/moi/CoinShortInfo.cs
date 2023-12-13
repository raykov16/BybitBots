namespace ByBitBots.Moi
{
    public class CoinShortInfo
    {
        public string? Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal FundingRate { get; set; }
        public long NextFunding { get; set; }
        public decimal Leverage { get;set; }
        public decimal Profits { get; private set; }

        public void SetProfits()
        {
            if (FundingRate < 0)
            {
                Profits = Math.Abs((FundingRate * Leverage) + (0.12m * Leverage));
            }
            else
            {
                Profits = Math.Abs((FundingRate * Leverage) - (0.12m * Leverage));
            }
        }
    }
}
