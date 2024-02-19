# Crypto Trading Bot

This is my crypto trading bot designed using the Bybit API. It can be used to trade or get market information.
I use the bot on a daily basis and work on improving the current functionalities and developing new ones.
Some functionalities might be broken on the TestNet accounts (the problem comes from Bybit's side)

## Getting Started
To start using the bot you will need to create 3 configuration files in the Configs folder.
Create a folder that has a ConfigConstants class that holds both the Testnet and Mainnet configurations.
Create MainnetConfig and TestnetConfig files implementing the IConfig interface and set up your configurations there.

## How to configurate for personal use:
I am using 1 main file where i hold my config settings and then i have 2 other configs for Main and Test nets. 
If you follow my approach the bot will run without any changes to the code.
Just keep the files in the Configs folder and use the same namings.

This is the main configuration file that holds the config information: 
![MainConfigFile](https://github.com/raykov16/BybitBots/assets/79668458/db4f9106-4fcd-4bdf-9f07-5fa098eacbc7)
I recommend keeping the RecvWindow to 500 000 000 as me in order to not have any problems with the requests.

Then use the created constants in the Main and Test net config classes. MainnetConfig class:
![image](https://github.com/raykov16/BybitBots/assets/79668458/daea3837-be26-48cd-90ad-76f88e919beb)

TestnetConfig class: 
![image](https://github.com/raykov16/BybitBots/assets/79668458/0a3490c3-e865-40e2-a9d4-f3623f08b620)


### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (.Net 6)

Packages used:
- [Bybit API Package](https://github.com/wuhewuhe/bybit.net.api)
- Microsoft Dependency Injection
