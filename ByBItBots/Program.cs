using bybit.net.api.ApiServiceImp;
using bybit.net.api.Models;
using bybit.net.api.Models.Trade;
using ByBitBots.Moi;
using ByBItBots.moi;
using ByBItBots.Results;
using Newtonsoft.Json;
using static ByBItBots.Config;

string mainnetUrl = "https://api.bybit.com";
string testnetUrl = "https://api-testnet.bybit.com";

string testNetApiKey = TestNetApiKey;
string testNetApiSecret = TestNetApiSecret;

string mainNetApiKey = MainNetApiKey;
string mainNetApiSecret = MainNetApiSecret;

string recvWindow = "500000000";

////////////////////////////////////////////////////////////////

string usedUrl = testnetUrl;
string usedApiKey = testNetApiKey;
string usedApiSecret = testNetApiSecret;

BybitMarketDataService market = new(url: usedUrl, recvWindow: recvWindow);

BybitTradeService trade = new(usedApiKey, usedApiSecret, recvWindow: recvWindow, url: usedUrl);
BybitPositionService position = new(usedApiKey, usedApiSecret, recvWindow: recvWindow, url: usedUrl);

await BuyCloseOrder(market, position, trade);


return;
List<CoinShortInfo> fittingCoins = await GetCoinsForFundingTradingAsync(market);

// print the information
PrintCoinShortInfo(fittingCoins);


//await FarmVolumeAsync(fittingCoins[0], 100, 49200 , 5, market, trade);
//await BuySpotCoinFirstAsync(fittingCoins[0].Symbol, 100, Side.SELL, market, trade);

//// make order for every coin
//List<OrderRequest> request = await CreateOrders(positionTest, fittingCoins);

////place positions
//var openOrderInfoString = await tradeTest.PlaceBatchOrder(Category.LINEAR, request);

//// wait 1 second then close all positions
//Thread.Sleep(1000);

//// close all positions
//var closeOrderInfoString = await tradeTest.CancelAllOrder(Category.LINEAR, baseCoin: "USDT");

//Console.WriteLine("OpenOrderInfoString : " + openOrderInfoString);
//Console.WriteLine();
//Console.WriteLine("CloseOrderInfoString : " + closeOrderInfoString);

//var riskLimit = await market.GetMarketRiskLimit(Category.LINEAR, "BTCUSDT");
//Console.WriteLine($"RiskLimit: {riskLimit}");

//var leverageResponse = await positionTest.SetPositionLeverage(Category.LINEAR, "BTCUSDT", "100", "100");
//var order = await tradeTest.PlaceOrder(Category.LINEAR, "BTCUSDT", Side.BUY, OrderType.MARKET, qty: "0.076", stopLoss: "36790", takeProfit: "37850"); // setva leverega takuv kakuvto e v saita

static async Task BuySpotCoinFirstAsync(string symbol, decimal capital, Side side, BybitMarketDataService market, BybitTradeService trade)
{
    List<CoinShortInfo> fittingCoins = await GetSpotCoinsAsync(market, symbol);

    while (fittingCoins.Count == 0)
    {
        Console.WriteLine("Coin not listed yet!");
        fittingCoins = await GetSpotCoinsAsync(market, symbol);
    }

    var coin = fittingCoins[0];

    var currentPrice = await GetCurrentPriceAsync(symbol, market, Category.SPOT);

    decimal quantity = 0;
    var previousOrderInfo = await GetLastOrderInfoAsync(trade, coin.Symbol);

    if (side == Side.SELL)
    {
        quantity = decimal.Parse(previousOrderInfo.Quantity);
    }
    else
    {
        quantity = Math.Round(capital / currentPrice, 2);
    }

    var placeOrderResult = await PlaceOrderAsync(trade, Category.SPOT, coin.Symbol, side, OrderType.LIMIT, quantity.ToString(), currentPrice.ToString());
    Console.WriteLine($"Placed order result: {placeOrderResult.RetMsg}");

    while (placeOrderResult.RetMsg != "OK")
    {
        placeOrderResult = await PlaceOrderAsync(trade, Category.SPOT, coin.Symbol, side, OrderType.LIMIT, quantity.ToString(), currentPrice.ToString());
        Console.WriteLine($"Placed order result: {placeOrderResult.RetMsg}");
    }

    var openOrdersResult = await GetOpenOrdersAsync(trade, coin.Symbol);

    while (openOrdersResult.Result.List.Count > 0)
    {
        currentPrice = await GetCurrentPriceAsync(coin.Symbol, market, Category.SPOT);

        if (side == Side.SELL)
        {
            quantity = decimal.Parse(previousOrderInfo.Quantity);
        }
        else
        {
            quantity = Math.Round(capital / currentPrice, 2);
        }

        var amendOrderResult = await trade.AmendOrder(Category.SPOT,
                coin.Symbol,
                orderId: openOrdersResult.Result.List[0].OrderId,
            qty: $"{quantity}",
                price: $"{currentPrice}");

        Console.WriteLine("Ammended order result: " + amendOrderResult);

        openOrdersResult = await GetOpenOrdersAsync(trade, coin.Symbol);
    }

    Console.WriteLine("GG bratan, glei parite v bybita luud");
}

static async Task FarmVolumeAsync(CoinShortInfo coin, decimal capital, decimal requiredVolume, int requestInterval, BybitMarketDataService market, BybitTradeService trade)
{
    decimal tradedVolume = 0m;
    bool shouldBuy = true;
    decimal quantity;
    decimal maxPriceDiff = 0.01m;
    var timesWithouthTrade = 300 / requestInterval; // Minute in seconds / interval = interval * timesWithoutTrade = minutes AKA how many minutes without a trade
    var actualTimesWithoutTrade = 0;
    requestInterval *= 1000; // transforming seconds to MS

    var timeStarted = DateTime.UtcNow;

    while (tradedVolume < requiredVolume)
    {
        // check for open orders
        var openOrdersResult = await GetOpenOrdersAsync(trade, coin.Symbol);
        Console.WriteLine("Got open orders");

        if (openOrdersResult.RetMsg != "OK")
        {
            Console.WriteLine(openOrdersResult.RetMsg);
            break;
        }

        if (openOrdersResult.Result.List.Count > 0)
            Console.WriteLine("Order price " + openOrdersResult.Result.List[0].Price);

        var currentPrice = await GetCurrentPriceAsync(coin.Symbol, market, Category.SPOT);
        Console.WriteLine("Got price");
        Console.WriteLine($"market price {currentPrice}");

        //ako ima otvoren order i cenata na toq order se razminava ot segashnata cena - vlizame
        if (openOrdersResult.Result.List.Count == 0)
        {
            Console.WriteLine("No orders found, opening an order!");

            if (tradedVolume >= requiredVolume)
                break;

            var previousOrderInfo = await GetLastOrderInfoAsync(trade, coin.Symbol);

            var previousSide = previousOrderInfo.Side;
            var previousPrice = decimal.Parse(previousOrderInfo.Price);

            if (previousSide == Side.BUY)
            {
                shouldBuy = false;
            }
            else
            {
                shouldBuy = true;
            }

            var side = shouldBuy ? Side.BUY : Side.SELL;

            Console.WriteLine("Previous side: " + previousSide);
            Console.WriteLine("Current side: " + side);

            currentPrice = await GetCurrentPriceAsync(coin.Symbol, market, Category.SPOT);
            Console.WriteLine("getting price");

            var priceDiff = CalculatePercentageDifference(previousPrice, currentPrice);

            if (previousSide == Side.BUY && priceDiff > maxPriceDiff && currentPrice <= previousPrice && actualTimesWithoutTrade < timesWithouthTrade)
            {
                Console.WriteLine($"Price diff: {priceDiff}, sell order will not be placed!");
                Console.WriteLine($"Times without trade: {++actualTimesWithoutTrade}");
                Thread.Sleep(requestInterval);
                continue;
            }
            else
            {
                actualTimesWithoutTrade = 0;
                Console.WriteLine($"Reseting times without trade to: {actualTimesWithoutTrade}");
            }

            if (previousSide == Side.BUY)
            {
                quantity = Math.Round(decimal.Parse(previousOrderInfo.Quantity), 2);
            }
            else
            {
                quantity = Math.Round(capital / currentPrice, 2);
            }

            var placeOrder = await trade.PlaceOrder(Category.SPOT, coin.Symbol, side, OrderType.LIMIT, $"{quantity}", $"{currentPrice}");
            var placeOrderResult = JsonConvert.DeserializeObject<ApiResponseResult<EmptyResult>>(placeOrder);

            Console.WriteLine("Placed order result: " + placeOrderResult.RetMsg);

            if (placeOrderResult.RetMsg == "OK")
            {
                tradedVolume += capital;
            }
        }
        else if (decimal.Parse(openOrdersResult.Result.List[0].Price) != currentPrice)
        {
            Console.WriteLine("Open order price difference, changing price!");
            // pak vzimame  segashnata cena i izchislqvame quantitito          

            var previousOrderInfo = await GetLastOrderInfoAsync(trade, coin.Symbol);

            var previousSide = previousOrderInfo.Side;
            var previousPrice = decimal.Parse(previousOrderInfo.Price);

            currentPrice = await GetCurrentPriceAsync(coin.Symbol, market, Category.SPOT);
            Console.WriteLine("getting price");

            var priceDiff = CalculatePercentageDifference(previousPrice, currentPrice);

            if (previousSide == Side.BUY && priceDiff > maxPriceDiff && currentPrice < previousPrice && actualTimesWithoutTrade <= timesWithouthTrade)
            {
                Console.WriteLine($"Price diff: {priceDiff}, existing order price will not be changed!");
                Console.WriteLine($"Times without trade: {++actualTimesWithoutTrade}");
                Thread.Sleep(requestInterval);
                continue;
            }
            else
            {
                actualTimesWithoutTrade = 0;
                Console.WriteLine($"Reseting times without trade to: {actualTimesWithoutTrade}");
            }

            // updeitvame ordera sus segashnata cena i podhodqshtoto quantity
            var amendOrderResult = await trade.AmendOrder(Category.SPOT,
                coin.Symbol,
                orderId: openOrdersResult.Result.List[0].OrderId,
                price: $"{currentPrice}");

            Console.WriteLine("Ammended order result: " + amendOrderResult);
        }

        Console.WriteLine($"waiting {requestInterval / 1000} sec");
        Thread.Sleep(requestInterval);
        Console.WriteLine($"waited {requestInterval / 1000} sec, starting again");
        Console.WriteLine($"Accumulated volume : {tradedVolume} / {requiredVolume}");
    }

    Console.WriteLine($"Succesfuly acumulated {tradedVolume} volume for {DateTime.UtcNow.TimeOfDay - timeStarted.TimeOfDay}! PICHAGAAAAAA");
}

static async Task<List<CoinShortInfo>> GetCoinsForFundingTradingAsync(BybitMarketDataService market)
{
    List<CoinShortInfo> fittingCoins = await GetProfitableFundingsAsync(market);

    // calculate bybit funding rate and leverage + set how much profits the coin will make
    await SetLeverageAndFundingRate(market, fittingCoins);

    return fittingCoins.OrderByDescending(c => c.Profits).ToList();
}

static async Task<decimal> GetCurrentPriceAsync(string symbol, BybitMarketDataService market, Category category)
{
    var marketTickers = await market.GetMarketTickers(category, symbol);
    ApiResponseResult<ResultCoinInfo> info = JsonConvert.DeserializeObject<ApiResponseResult<ResultCoinInfo>>(marketTickers);

    CoinShortInfo coin = null;

    if (category == Category.SPOT)
    {
        coin = info.Result.List
        .Where(c => c.Symbol.Contains("USDT"))
        .Select(c => new CoinShortInfo
        {
            Symbol = c.Symbol,
            Price = decimal.Parse(c.LastPrice)
        })
        .FirstOrDefault();
    }
    else
    {
        coin = info.Result.List
      .Where(c => c.Symbol.Contains("USDT"))
      .Select(c => new CoinShortInfo
      {
          FundingRate = decimal.Parse(c.FundingRate),
          NextFunding = long.Parse(c.NextFundingTime),
          Symbol = c.Symbol,
          Price = decimal.Parse(c.LastPrice)
      })
      .FirstOrDefault();
    }

    return coin.Price;
}

static void PrintCoinShortInfo(List<CoinShortInfo> fittingCoin)
{
    foreach (var c in fittingCoin)
    {
        Console.WriteLine("-----------");
        Console.WriteLine($"Coin: {c.Symbol}");
        Console.WriteLine($"Funding rate: {c.FundingRate:f4}");
        Console.WriteLine($"Leverage: {c.Leverage}");
        Console.WriteLine($"Price: {c.Price}");
        Console.WriteLine($"SetProfitsResult {c.Profits:f2}");
    }
}

static async Task<List<OrderRequest>> CreateOrdersAsync(BybitPositionService positionTest, List<CoinShortInfo> coins)
{
    List<OrderRequest> request = new List<OrderRequest>();

    foreach (var c in coins.OrderByDescending(c => c.Profits))
    {
        var coin = c;

        if (request.Count < 10)
        {
            var leverageResponse = await positionTest.SetPositionLeverage(Category.LINEAR, coin.Symbol, $"{coin.Leverage}", $"{coin.Leverage}");
            Console.WriteLine(leverageResponse);

            var order = new OrderRequest
            {
                Symbol = coin.Symbol,
                OrderType = "Market",
                Side = coin.FundingRate > 0 ? "Sell" : "Buy",
                Qty = "1",
                //StopLoss = "",
            };

            request.Add(order);
        }
    }

    return request;
}

static async Task<List<CoinShortInfo>> GetProfitableFundingsAsync(BybitMarketDataService market)
{
    var derivativeCoins = await GetDerivativesCoinsAsync(market);

    return derivativeCoins.Where(c => c.FundingRate > 0.0012m || c.FundingRate < -0.0012m).ToList();
}

static async Task<List<CoinShortInfo>> GetSpotCoinsAsync(BybitMarketDataService market, string symbol = null)
{
    var marketTickers = await market.GetMarketTickers(Category.SPOT, symbol);
    ApiResponseResult<ResultCoinInfo> info = JsonConvert.DeserializeObject<ApiResponseResult<ResultCoinInfo>>(marketTickers);

    return info.Result.List
        .Where(c => c.Symbol.Contains("USDT"))
        .Select(c => new CoinShortInfo
        {
            Symbol = c.Symbol,
            Price = decimal.Parse(c.LastPrice)
        })
        .OrderBy(c => c.Symbol)
        .ToList();
}

static async Task<List<CoinShortInfo>> GetDerivativesCoinsAsync(BybitMarketDataService market, string symbol = null)
{
    var marketTickers = await market.GetMarketTickers(Category.LINEAR, symbol);
    ApiResponseResult<ResultCoinInfo> info = JsonConvert.DeserializeObject<ApiResponseResult<ResultCoinInfo>>(marketTickers);

    return info.Result.List
       .Where(c => c.Symbol.Contains("USDT"))
       .Select(c => new CoinShortInfo
       {
           FundingRate = decimal.Parse(c.FundingRate),
           NextFunding = long.Parse(c.NextFundingTime),
           Symbol = c.Symbol,
           Price = decimal.Parse(c.LastPrice)
       })
       .ToList();
}

static async Task SetLeverageAndFundingRate(BybitMarketDataService market, List<CoinShortInfo> coins)
{
    foreach (var c in coins)
    {
        c.Leverage = await GetLeverageAsync(market, c.Symbol);

        c.FundingRate *= 100;
    }
}

static async Task<ApiResponseResult<ResultOpenOrders>> GetOpenOrdersAsync(BybitTradeService tradeTest, string symbol)
{
    var openOrders = await tradeTest.GetOpenOrders(Category.SPOT, symbol: symbol);
    var openOrdersResult = JsonConvert.DeserializeObject<ApiResponseResult<ResultOpenOrders>>(openOrders);
    return openOrdersResult;
}

static async Task<PreviousOrderInfo> GetLastOrderInfoAsync(BybitTradeService trade, string symbol)
{
    var orderHistory = await trade.GetOrdersHistory(Category.SPOT, symbol, limit: 1);
    var orderHistoryResult = JsonConvert.DeserializeObject<ApiResponseResult<ResultOpenOrders>>(orderHistory);

    var previousOrderInfo = new PreviousOrderInfo
    {
        Side = orderHistoryResult.Result.List[0].Side,
        Price = orderHistoryResult.Result.List[0].AvgPrice,
        Quantity = orderHistoryResult.Result.List[0].Qty
    };

    return previousOrderInfo;
}
static decimal CalculatePercentageDifference(decimal num1, decimal num2)
{
    decimal absoluteDifference = Math.Abs(num1 - num2);
    decimal average = (num1 + num2) / 2;

    decimal percentageDifference = (absoluteDifference / average) * 100;

    return Math.Round(percentageDifference, 2);
}

static decimal CalculateTPSLPrice(decimal percentageLose, decimal leverage, decimal coinPrice, bool isTakeProfit)
{
    decimal coinPercentPriceDrop = percentageLose / leverage / 100;

    decimal coinCashPriceDrop = coinPrice * coinPercentPriceDrop;

    if (isTakeProfit)
    {
        decimal takeProfit = coinPrice + coinCashPriceDrop;

        return takeProfit;
    }

    decimal stopLoss = coinPrice - coinCashPriceDrop;

    return stopLoss;
}

static async Task<ApiResponseResult<EmptyResult>> PlaceOrderAsync(BybitTradeService trade, Category category, string coinSymbol, Side side, OrderType orderType, string quantity, string currentPrice)
{
    var placeOrder = await trade.PlaceOrder(category, coinSymbol, side, orderType, quantity, currentPrice);
    return JsonConvert.DeserializeObject<ApiResponseResult<EmptyResult>>(placeOrder);
}

static async Task<decimal> GetLeverageAsync(BybitMarketDataService market, string symbol)
{
    var instrumentInfo = await market.GetInstrumentInfo(Category.LINEAR, symbol);

    var arr = new string[100];

    arr = instrumentInfo.Split(":").ToArray();

    return decimal.Parse(arr[17].Split("\",")[0].Replace("\"", ""));
}

static async Task BuyCloseOrder(BybitMarketDataService market, BybitPositionService position, BybitTradeService trade)
{
    var coin = (await GetDerivativesCoinsAsync(market, "BTCUSDT"))[0];
    var capital = 100m;

    coin.Leverage = await GetLeverageAsync(market, coin.Symbol);
    var leverageResponse = await position.SetPositionLeverage(Category.LINEAR, coin.Symbol, $"{coin.Leverage}", $"{coin.Leverage}");

    var currentPrice = await GetCurrentPriceAsync(coin.Symbol, market, Category.LINEAR);

    var quantityRaw = capital / currentPrice;
    var quantity = Math.Round(quantityRaw, 3);

    var tpPrice = CalculateTPSLPrice(50, coin.Leverage, currentPrice, true);
    var slPrice = CalculateTPSLPrice(50, coin.Leverage, currentPrice, false);

    var orderInfo = await trade.PlaceOrder(Category.LINEAR,
        coin.Symbol,
        Side.BUY
        , OrderType.MARKET
        , qty: quantity.ToString()
        , takeProfit: tpPrice.ToString()
        , stopLoss: slPrice.ToString());

    Console.WriteLine(orderInfo);

    Thread.Sleep(5000);

    var placeOrderResult = JsonConvert.DeserializeObject<ApiResponseResult<PlacedOrderResult>>(orderInfo);

    var cancleOrder = await trade.PlaceOrder(category: Category.LINEAR
        ,symbol: coin.Symbol
        ,side: Side.SELL
        ,orderType: OrderType.MARKET
        , reduceOnly: true
        , qty: quantity.ToString(),
        price: "0");

    Console.WriteLine(cancleOrder);
}