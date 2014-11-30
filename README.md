DotaHostManager
===============

###Notes
 - LoD requires reserve of 350mb RAM and 15% CPU

###Requirnments for building###
 - Visual Studio 2013 (2012 should also work)
 - Alchemy Websockets
  - Using Visual Studio's package manager, NuGet: `Install-Package Alchemy`

###Addon Location###
 - This could change in the future
 - Addons will be housed on a central repo controlled by us [This one](https://github.com/ash47/DotaHostAddons)
 - We will create and update releases for plugins as needed
 - We should delete old releases once the new release has successfully rolled out

###Server Installer###
 - This will download dota 2 (source1 & source2)
 - It will download and install metamod
 - It will download and install d2fixups
 - It will patch the gameinfo.txt to load metamod
 - It will patch all the large maps so they are loadable.
 - It will install SRCDS
 - It can be called again to update the server directly

The Server Installer needs further testing. D2fixups (among other things) NEED to be deployed to our own HTTP server ASAP, it has already caused issues by not being able to download it due to the download moving.

###Server Launcher###
 - The server launcher can launch both source1 and source2 servers
 - Most server launcher arguments are HARD CODED
 - We need to find a way to add a `loading/waiting for players` screen to the start of the match like the old days

###Custom Server Scripts###
 - Lobbies, options, team allocation and server closing is handled by Lua
 - The following scripts will automatically be installed onto all dedicated servers
  - serverinit from [github.com/ash47/DotaHostAddons](https://github.com/ash47/DotaHostAddons)
  - These scripts aren't currently installed / tested
  - serverinit.zip NEEDS to be installed as an addon
  - The addon compiler will compile it in, which will deal with vscripts
  - DO NOT add serverinit to the list of addons to load -- it is built into the addonCompiler class, and will auto compile

###Passing settings to Lua###
 - Originally we were going to use +hostname <settings>
 - It has now changed to being compiled directly into the Lua
 - AddonCompiler.compileAddons() now has an optional serverSettings string, if set, that string will be stored into the Lua loading scripts
 - That string currently ISN'T processed, but it will be :)

###Webserver Layout###
 - wwwroot/
  - addons/
   - addonID.txt (there is one for each addon)
  - DotaHostManager.exe (this is the actual program)
  - DotaHostManagerUpdater.exe (this is the updater)
  - version.txt (this contains version info for the client program)

NOTE: If you want to change this layout, you NEED to change the layout in the DotaHostClientLibrary/Globals.cs

###Todo###
 - Everything XD
