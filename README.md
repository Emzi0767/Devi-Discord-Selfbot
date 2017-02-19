#Dynamic Evaluation Implement by Emzi0767

##ABOUT

A Discord selfbot built on top of [Discord.NET library](https://github.com/RogueException/Discord.Net). It's designed to provide a C# evaluator, quoting, and some other functionality.

##BUILDING

You need .NET Core 1.1 SDK to build the project, and .NET Core 1.1 runtime to run it.

1. In order to build this project, you will need to add the following package sources to your NuGet:
   * `https://www.myget.org/F/discord-net/api/v3/index.json`
   * `https://dotnet.myget.org/F/roslyn/api/v3/index.json`
2. Next, you must restore all NuGet packages.
3. Then build the code in Release mode.
4. Finally publish the bot.

##SETUP

In order for bot to run, you will need to set up your environment. 

1. Create a directory for the bot.
2. Copy `devi.json`, `emoji.json`, and `donger.json` from project source to bot's directory.
3. Edit `devi.json` and put your user's access token in the file.
4. Copy bot's Publish results to the bot directory.

##RUNNING

DO `dotnet Emzi0767.Devi.exe`.