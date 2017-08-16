# Dynamic Evaluation Implement by Emzi0767

[![Emzi's Central Dispatch](https://discordapp.com/api/guilds/207879549394878464/widget.png)](https://discord.gg/rGKrJDR)

## ABOUT

A Discord selfbot built on top of [DSharpPlus library](https://github.com/NaamloosDT/DSharpPlus). It's designed to provide a C# evaluator, quoting, and some other functionality.

More information is available on [its GitHub page](https://emzi0767.github.io/discord/devi/).

## BUILDING

You need .NET Core SDK 2.0 to build the project, and .NET Core 2.0.0 runtime to run it. Both are available [here](https://www.microsoft.com/net/download/core ".NET Core download page").

1. In order to build this project, you will need to add the following package sources to your NuGet:
   * `https://www.myget.org/F/discord-net/api/v3/index.json`
   * `https://dotnet.myget.org/F/roslyn/api/v3/index.json`
2. Next, you must restore all NuGet packages (`dotnet restore`).
3. Then build the code in Release mode (`dotnet build -c Release`).
4. Finally publish the bot (`dotnet publish -c Release`).
   * You can optionally package it as a self-contained application by specifying target RID such as `linux-x64` or `linux-arm` (`dotnet publish -c Release -r linux-x64`).

## SETUP

In order for bot to run, you will need to set up your environment. 

### POSTGRESQL DATABASE

1. If you haven't done so already, install PostgreSQL server (version 9.6 or better).
2. Create a database for bot's data.
3. Create a user for the database.
4. Execute the attached `schema.sql` script as the created user.

### THE BOT ITSELF

1. Create a directory for the bot.
2. Copy bot's Publish results to the bot directory.
3. Copy `devi.json`, `emoji.json`, and `donger.json` from project source to bot's directory.
4. Edit `devi.json` and put your user's access token in the file.

## RUNNING

Execute `dotnet Emzi0767.Devi.dll` in your command line.

If you packaged the bot as a self-contained app, you will need to run the bot's executable. That is `Emzi0767.Devi.exe` for Windows, or `./Emzi0767.Devi` for GNU/Linux.

It is recommended you run the bot in a terminal multiplexer, such as `screen` or `tmux` when running on GNU/Linux.

## SUPPORT ME

If you feel like supporting me by providing me with currency that I can exchange for goods and services, you can do so on [my Patreon](https://www.patreon.com/emzi0767).

## ADDITIONAL HELP

Should you still have any questions regarding the bot, feel free to join my server. I'll try to answer an questions:

[![Emzi's Central Dispatch](https://discordapp.com/api/guilds/207879549394878464/embed.png?style=banner1)](https://discord.gg/rGKrJDR)