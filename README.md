DotaHostManager
===============

###Requirnments for building###
 - Visual Studio 2013 (2012 should also work)
 - Alchemy Websockets
  - Using Visual Studio's package manager, NuGet: `Install-Package Alchemy`

###Source1 Server Installer###
 - This will download dota 2 (source1)
 - It will download and install metamod
 - It will download and install d2fixups
 - It will patch the gameinfo.txt to load metamod
 - It will patch all the large maps so they are loadable.
 - It will install SRCDS
 - It can be called again to update the server directly

###Source2 Server Installer###
 - This will attempt to download dota 2 (source2)
 - Having issues with depot downloader where it will corrupt a ton of files
 - Not recommended to install until issues are solved

###Custom Server Scripts###
 - Lobbies, options, team allocation and server closing is handled by Lua
 - The following scripts will automatically be installed onto all dedicated servers
  - https://github.com/ash47/Dota2ServerLoaderScripts
  - These scripts aren't currently installed / tested

###Todo###
 - Everything XD
