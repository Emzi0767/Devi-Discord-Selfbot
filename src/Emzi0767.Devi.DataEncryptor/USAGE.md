# Devi Data Encryption Tool - usage

This tool encrypts the message contents in your database, in order to be compliant with Discord's new developer ToS.

## BUILDING

You need .NET Core SDK 2.0 to build the project, and .NET Core 2.0.0 runtime to run it. Both are available [here](https://www.microsoft.com/net/download/core ".NET Core download page").

1. In order to build this project, you must restore all NuGet packages (`dotnet restore`).
2. Then build the code in Release mode (`dotnet build -c Release`).
3. Finally publish the tool (`dotnet publish -c Release`).
   * You can optionally package it as a self-contained application by specifying target RID such as `linux-x64` or `linux-arm` (`dotnet publish -c Release -r linux-x64`).

## SETUP

In order for the encryptor to run, you need to set up your environment.

### POSTGRESQL BACKUP

**This is important.** Do not do any large operations on your database without backing it up first.

1. Run `pgdump -U postgres -d devi_database_name > backup.sql`.
   * You can optionally compress the backup afterwards, by doing `xz -z9evv backup.sql`.
2. Wait for the backup to complete, this might take a long moment.

### THE TOOL ITSELF

1. Copy `devi.json` from your Devi instance's directory.
2. Remove all contents except for the database configuration.
3. Rename the file to `devidb.json`.

## RUNNING

Execute `dotnet Emzi0767.Devi.DataEncryptor.dll` in your command line.

If you packaged the tool as a self-contained app, you will need to run the tool's executable. That is `Emzi0767.Devi.DataEncryptor.exe` for Windows, or `./Emzi0767.Devi.DataEncryptor` for GNU/Linux.

Please note that the tool will take a longer while to finish its job.

It is recommended you run the tool in a terminal multiplexer, such as `screen` or `tmux` when running on GNU/Linux.

## AFTERMATH

The tool will generate a file called `keystore.json`. Copy it to your Devi instance's directory.