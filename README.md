DotaHostManager
===============

###Notes
 - LoD requires reserve of 350mb RAM and 15% CPU

###Requirnments for building###
 - Visual Studio 2013 (2012 should also work)
 - Alchemy Websockets
  - Using Visual Studio's package manager, NuGet: `Install-Package Alchemy`

###Server Installer###
 - This will download dota 2 (source1 & source2)
 - It will download and install metamod
 - It will download and install d2fixups
 - It will patch the gameinfo.txt to load metamod
 - It will patch all the large maps so they are loadable.
 - It will install SRCDS
 - It can be called again to update the server directly

The Server Installer needs further testing. D2fixups (among other things) NEED to be deployed to our own HTTP server ASAP, it has already caused issues by not being able to download it due to the download moving.

###Custom Server Scripts###
 - Lobbies, options, team allocation and server closing is handled by Lua
 - The following scripts will automatically be installed onto all dedicated servers
  - https://github.com/ash47/Dota2ServerLoaderScripts
  - These scripts aren't currently installed / tested

###Todo###
 - Everything XD
