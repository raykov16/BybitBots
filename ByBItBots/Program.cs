using bybit.net.api.ApiServiceImp;
using ByBItBots.Configs;
using ByBItBots.Constants;
using ByBItBots.DTOs.Menus;
using ByBItBots.Enums;
using ByBItBots.Helpers.Implementations;
using ByBItBots.Helpers.Interfaces;
using ByBItBots.Services.Implementations;
using ByBItBots.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;


var printer = new ConsolePrinterService();

printer.PrintMenu(new BybitNetsMenu());
var userChoice = ChooseNet();
Console.Clear();

IConfig config;

if (userChoice == BybitNets.TESTNET)
{
    config = new TestnetConfig();
    printer.PrintMessage(InterfaceCommunicationMessages.USING_TEST_NET);
}
else if (userChoice == BybitNets.MAINNET)
{
    config = new MainnetConfig();
    printer.PrintMessage(InterfaceCommunicationMessages.USING_MAIN_NET);
}
else 
{
    config = new SmurfMainnetConfig();
    printer.PrintMessage(InterfaceCommunicationMessages.USING_MAIN_NET);
}


var serviceProvider = new ServiceCollection()
    .AddScoped<BybitMarketDataService>(provider =>
    {
        return new(config.NetURL, config.RecvWindow);

    })
    .AddScoped<BybitTradeService>(provider =>
    {
        return new(config.ApiKey, config.ApiSecret, config.NetURL, config.RecvWindow);
    })
    .AddScoped<BybitPositionService>(provider =>
    {
        return new(config.ApiKey, config.ApiSecret, config.NetURL, config.RecvWindow);
    })
    .AddScoped<IBybitTimeService, BybitTimeService>()
    .AddScoped<IConfig>(cfg => config)
    .AddScoped<ICoinDataService, CoinDataService>()
    .AddScoped<IPrinterService, ConsolePrinterService>()
    .AddScoped<IDerivativesTradingService, DerivativesTradingService>()
    .AddScoped<IOrderService, OrderService>()
    .AddScoped<ISpotTradingService, SpotTradingService>()
    .AddScoped<IBotInterfaceHost, BotConsoleInterfaceHost>()
    .BuildServiceProvider();

var bot = serviceProvider.GetRequiredService<IBotInterfaceHost>();
await bot.StartBot(config);


#region Private methods
BybitNets ChooseNet()
{
    Console.CursorVisible = false;

    var userChoice = Console.ReadKey(true);

    if (userChoice.Key == ConsoleKey.D1)
    {
        return BybitNets.TESTNET;
    }
    else if (userChoice.Key == ConsoleKey.D2)
    {
        return BybitNets.MAINNET;
    }
    else
    {
        printer.PrintMessage(InterfaceCommunicationMessages.PRESS_SPECIFIED_BUTTON);
        return ChooseNet();
    }
}
#endregion  Private methods