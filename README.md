# Crypto Trading Bot

This is my crypto trading bot designed using the Bybit API. It can be used to trade or get market information.
I use the bot on a daily basis and work on improving the current functionalities and developing new ones.
Some functionalities might be broken on the TestNet accounts (the problem comes from Bybit's side)

## Getting Started
To start using the bot you will need to create 3 configuration files in the Configs folder.
Create a folder that has a ConfigConstants class that holds both the Testnet and Mainnet configurations.
Create MainnetConfig and TestnetConfig files implementing the IConfig interface and set up your configurations there.

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (.Net 6)

Packages used:
- [Bybit API Package](https://github.com/wuhewuhe/bybit.net.api)
- Microsoft Dependency Injection
