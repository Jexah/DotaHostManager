/*
 * NOTES
 * All the verify*() commands could be merged into one, since their code is basically exactly the same, just different URLs
 * 
 * */


using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace DotaHostBoxManager
{
    class Program
    {

        #region Constants

        // The path to steamcmd
        private const string STEAMCMD_PATH = @"steamcmd\";

        // The steam cmd file to run steam commands with
        private const string STEAMCMD = STEAMCMD_PATH + "steamcmd.exe";

        // The base path to our files
        private const string DOWNLOAD_PATH_BASE = "http://dotahost.net/files/";

        // The path to download steamcmd from
        private const string DOWNLOAD_PATH_STEAMCMD = DOWNLOAD_PATH_BASE + "steamcmd.zip";

        // URL to download SRCDS from
        private const string DOWNLOAD_PATH_SRCDS = DOWNLOAD_PATH_BASE + "srcds.zip";

        // URL to download metamod from
        private const string DOWNLOAD_PATH_METAMOD = DOWNLOAD_PATH_BASE + "mmsource.zip";

        // URL to download d2fixups from
        private const string DOWNLOAD_PATH_D2FIXUPS = DOWNLOAD_PATH_BASE + "d2fixups.zip";

        // The path to the source servers
        private const string SOURCE_PATH = @"dota\";

        // The username to download files with (Username and password should probably be exported somewhere)
        private const string STEAM_USERNAME = "dotahost_net";

        // The password to download files with
        private const string STEAM_PASSWORD = "***REMOVED***";

        #endregion

        #region Static Readonlys

        // Network card set up for network monitoring
        private static readonly string[] NETWORK_CARDS = getNetworkCards();

        // The command to update the servers
        private static readonly string STEAMCMD_UPDATE_SERVERS = "+login " + STEAM_USERNAME + " " + STEAM_PASSWORD + " +runscript install.txt";

        // The update file for source1
        private static readonly string STEAMCMD_UPDATEFILE = "@ShutdownOnFailedCommand 0" + Environment.NewLine + "force_install_dir " + Global.BASE_PATH + SOURCE_PATH + Environment.NewLine + "app_update 316570" + Environment.NewLine + "app_update 570" + Environment.NewLine + "quit";
        
        #endregion

        #region Private variables

        // Performance monitoring
        private static PerformanceCounter cpuCounter = new PerformanceCounter();
        private static PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        private static List<PerformanceCounter> dataSentCounter = new List<PerformanceCounter>();
        private static List<PerformanceCounter> dataReceivedCounter = new List<PerformanceCounter>();

        // Used for downloading files
        private static DownloadManager dlManager = new DownloadManager();
        
        // Web socket client
        private static WebSocketClient wsClient = new WebSocketClient("ws://" + Vultr.SERVER_MANAGER_IP + ":" + Vultr.SERVER_MANAGER_PORT + "/");
           
        // Unique websocket client ID
        private static string websocketUserID;

        // List of game server running on the box
        private static List<GameServer> gameServers = new List<GameServer>();

        // Box Server status
        private static byte status;

        // Box Server subID
        private static int subID;

        #endregion

        // The main entry point into the program
        private static void Main(string[] args)
        {

            // Delete the old log file
            File.Delete(Global.BASE_PATH + "log.txt");

            // Cleanup addons folder
            Helpers.deleteFolder(Global.BASE_PATH + "addons\\", true);

            //updateServers();
            //launchGameServer(null, null);

            // Create addon list
            /*List<Addon> addons = new List<Addon>();

            // Add LoD
            Dictionary<string, string> addonProperties = new Dictionary<string, string>();
            addons.Add(new Addon("lod", addonProperties));

            // Add warchasers
            addonProperties = new Dictionary<string, string>();
            addons.Add(new Addon("warchasers", addonProperties));

            // Compile
            AddonCompiler.compileAddons(addons, Global.BASE_PATH + "addonZips\\", Global.BASE_PATH + "addons\\", true);

            return;*/

            status = BoxManager.IDLE;

            setupSystemDiagnostics();

            hookWSocketEvents();


            wsClient.start();

            // Update the dota install
            //updateDotaSource1();
        }

        // Iterates through the network cards, adding them to the static readonlys dataSendCounter, and dataReceivedCounter
        private static void setupNetworkCards()
        {
            for (int i = 0; i < NETWORK_CARDS.Length; ++i)
            {
                dataSentCounter.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", NETWORK_CARDS[i]));
                dataReceivedCounter.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", NETWORK_CARDS[i]));
            }
        }
        
        // Hook websocket events
        private static void hookWSocketEvents()
        {
            // Log everything that is received, for debugging
            #region wsClient.addHook(WebSocketClient.RECEIVE);
            wsClient.addHook(WebSocketClient.RECEIVE, (c) =>
            {
                Helpers.log(c.DataFrame.ToString());
            });
            #endregion

            // When connected to the ServerManager, send alert and request information and instructions
            #region wsClient.addHook(WebSocketClient.CONNECTED);
            wsClient.addHook(WebSocketClient.CONNECTED, (c) =>
            {
                c.Send("box");
            });
            #endregion

            // Begin server reboot
            #region wsClient.addHook("reboot");
            wsClient.addHook("reboot", (c, x) =>
            {
                status = BoxManager.DEACTIVATED;
                rebootLoop();
            });
            #endregion

            // Server destroy (soft/hard)
            #region wsClient.addHook("destroy");
            wsClient.addHook("destroy", (c, x) =>
            {
                if (x.Length > 1 && x[1] == "hard")
                {
                    // Hard destroy, no waiting, no timeout
                    destroy();
                }
                else
                {
                    // Soft destroy, waits for games to finish, polls every minute
                    status = BoxManager.DEACTIVATED;
                    destroyLoop();
                }
            });
            #endregion

            // Receive subid from server manager
            #region wsClient.addHook("subid");
            wsClient.addHook("subid", (c, x) =>
            {
                subID = Convert.ToInt32(x[1]);
                Helpers.log(x[1]);
            });
            #endregion

            // Get unique socket identifier from server
            #region wsClient.addHook("id");
            wsClient.addHook("id", (c, x) =>
            {
                websocketUserID = x[1];
            });
            #endregion

            // Get status overview
            #region wsClient.addHook("system");
            wsClient.addHook("system", (c, x) =>
            {
                int[] args = getSystemDiagnostics();
                if (status != BoxManager.DEACTIVATED)
                {
                    if (gameServers.Count == 0)
                    {
                        status = BoxManager.IDLE;
                    }
                    else
                    {
                        status = BoxManager.ACTIVE;
                    }

                    // func;status;cpu;ramAvailable;ramTotal;upload;download
                    c.Send("system;" + status + ";" + String.Join(";", args));
                }
                
            });
            #endregion

            // Create game server function
            #region wsClient.addHook("create");
            wsClient.addHook("create", (c, x) =>
            {
                // Socket msg: "create;addon0=lod;addon0options=maxBans-20|mode-ap;addon1=csp;addon1options=multiplier-2;team0=0-Jexah-STEAM1:0_38397532|1-Ash-STEAM_0:1:343492;team1="
                // Lobby args: "addon0=lod;addon0options=maxBans-20|mode-ap;addon1=csp;addon1options=multiplier-2;team0=0-Jexah-STEAM1:0_38397532|1-Ash-STEAM_0:1:343492;team1="

                // Create server object to handle game server info
                GameServer gameServer = new GameServer();

                // Remove the first element of the array (function name ("create"))
                string[] gameServerArgs = Helpers.RemoveIndex(x, 0);

                // Re-add the seperators
                string gameServerArgsStr = String.Join(";", gameServerArgs);

                // Set up the properties for the lobby in case we want to retrieve them later
                Dictionary<string, string> lobbyArgs = Lobby.getLobbyArgsObj(gameServerArgs);

                // Set the the options to the game server
                gameServer.Options = lobbyArgs;
             
                // Read teams from input arguments
                List<List<Player>> teams = Lobby.getPlayersObj(lobbyArgs);

                // Set the teams on the game server
                gameServer.Players = teams;

                // Read addons from input arguments
                List<Addon> addons = Lobby.getAddonsObj(lobbyArgs);

                // Set the addons on the game server
                gameServer.Addons = addons;

                // Add game server to list
                gameServers.Add(gameServer);

                // Launch the server using the string options
                //launchGameServer(gameServer, gameServerArgsStr);
                Helpers.log(gameServerArgsStr);

            });
            #endregion

            // Return game server status to the ServerManager [WIP]
            #region wsClient.addHook("gameServers");
            wsClient.addHook("gameServers", (c, x) =>
            {
                string gameServersString = "";
                for (int i = 0; i < gameServers.Count; ++i)
                {
                    gameServersString += gameServers[i].Addons[0].Id;
                }
                c.Send("gameServers;" + gameServersString);
            });
            #endregion

        }

        // Starts a specific game server with a specific set of arguments
        private static void launchGameServer(GameServer gameServer, string gameServerArgs)
        {
            // NOTE: WE NEED TO ENSURE ONLY ONE SERVER IS BOOTING AT A TIME, OR WE WILL HAVE A SHIT STORM!

            // ASSUMPTION: THE SERVERS ARE FULLY INSTALLED AND ADDONS ARE GOOD TO LOAD

            // We probably want to report that the server failed, if it did infact fail

            
            // BEGIN OPTIONS: SHOULD AUTO FILL THESE

            // Should we use source1?
            bool source1 = true;

            // Should adjust this where nessessary
            int maxPlayers = 10;

            // Port to open on, perhaps we keep track of what ports are in use? or test? idk
            int port = 27017;

            // The map to load up
            string map = "dota";

            // The path to the addons we need to mount (this will be generated by addon compiler)
            // Once the server has closed, the mount path will need to be deleted as well
            string mountPath = Global.BASE_PATH + @"addons\LegendsOfDota\lod\";

            // END OPTIONS



            // The path to SRCDS
            string path = Global.BASE_PATH + SOURCE_PATH;

            // The application to launch
            string app;

            // The arguments to launch with
            string args;
    
            // Build arguments based on server version required
            if(source1)
            {
                // Source1 Settings
                app = "srcds.exe";
                args = "-console -game dota +maxplayers " + maxPlayers + " -port " + port + " +dota_force_gamemode 15 +dota_local_addon_enable 1 +map " + map;

                // -strictportbind doesn't appear to work, doh!

                // Patch gameinfo.txt
                source1GameInfoPatch(mountPath);
            }
            else
            {
                // Source2 Settings
                app = @"dota_ugc\game\bin\win64\dota2.exe";
                args = "-dedicated +maxplayers " + maxPlayers + " -port " + port + " -addon_path \"" + mountPath + "\" -dota \"+map " + map + " gamemode=15 customgamemode=lod nomapvalidation=1\"";

                // NOTE: -console IS FOR DEBUG, PLEASE REMOVE THIS ONCE SERVERS ARE GOOD!
                // NOTE: Closing the console wont kill the server, you NEED to type `quit` into the console to close the dedicated server!
                // NOTE: customgamemode=<something> -- <something> is meant to be the ID of the gamemode to load, if you don't have it, it will spit errors, I don't think it matters since we are mounting via addon_path
            }


            // Build the update commmand
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.WorkingDirectory = path;
            proc.FileName = path + app;
            proc.Arguments = args;

            // Attempt to launch the server
            try
            {
                // Start the process
                Process process = Process.Start(proc);

                // Woot, success
                Helpers.log("Server was launched successfully!");

                // We probably want to store a reference to the process so we can see if it dies
            }
            catch
            {
                Helpers.log("Failed to launch the server!");
            }
        }

        // Reboot check loop
        private static void rebootLoop()
        {
            if (gameServers.Count > 0)
            {
                // Game server still running, check again in 60 seconds
                Timers.setTimeout(60, Timers.SECONDS, () => { rebootLoop(); });
            }
            else
            {
                // Reboot server
                System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
            }
        }

        // Destroy check loop
        private static void destroyLoop()
        {
            if (gameServers.Count > 0)
            {
                // Game servers still running, check again in 60 seconds
                Timers.setTimeout(60, Timers.SECONDS, () => { destroyLoop(); });
            }
            else
            {
                // Destroy server
                Vultr.destroyServer(subID);
            }
        }

        // Destroy server instantly
        private static void destroy()
        {
            Vultr.destroyServer(subID);
        }

        // Set up system diagnostics
        private static void setupSystemDiagnostics()
        {
            setupNetworkCards();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
        }

        // Get system diagnostics such as CPU % usage, RAM used and RAM remaining
        private static int[] getSystemDiagnostics()
        {
            return new int[] { getCurrentCpuUsage(), getAvailableRAM(), getTotalRAM(), getUploadSpeed(), getDownloadSpeed() };
        }

        // This function ensures steamcmd is available
        private static void installServerFile(string localFileCheck, string downloadURL, string extractTo, string friendlyName, Action callback)
        {
            // Check if steamcmd exists
            if (!File.Exists("localFileCheck"))
            {
                // Debug log
                Helpers.log(friendlyName+" not found, downloading...");

                // Name of the zip to use
                string zipName = friendlyName+".zip";

                // If there is an old version of steamcmd.zip, delete it
                File.Delete(Global.BASE_PATH + zipName);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                dlManager.download(downloadURL, zipName, (e) => { }, (e) =>
                {
                    // Extract the archive
                    ZipFile.ExtractToDirectory(zipName, extractTo);

                    // Delete the zip
                    File.Delete(Global.BASE_PATH + zipName);

                    // Run the callback
                    callback();
                });

                // Wait for download to finish
                return;
            }

            // Run the callback
            callback();
        }

        // This function updates both servers
        // If server aren't installed, this function will install them from scratch
        private static void updateServers()
        {
            // Debug log
            Helpers.log("Updating dota 2 (source2)...");

            // Ensure steamcmd exists
            installServerFile(Global.BASE_PATH + STEAMCMD, DOWNLOAD_PATH_STEAMCMD, Global.BASE_PATH + STEAMCMD_PATH, "steamCMD", () =>
            {
                // Ensure the directory exists
                Directory.CreateDirectory(SOURCE_PATH);

                // Create the path command file
                File.WriteAllText(Global.BASE_PATH + STEAMCMD_PATH + "install.txt", STEAMCMD_UPDATEFILE);

                // Build the update commmand
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.WorkingDirectory = Global.BASE_PATH;
                proc.FileName = STEAMCMD;
                proc.Arguments = STEAMCMD_UPDATE_SERVERS;

                // Attempt to run the update
                try
                {
                    // Start the process
                    Process process = Process.Start(proc);

                    // Wait for it to end
                    process.WaitForExit();
                }
                catch
                {
                    Helpers.log("Failed to update!");
                    return;
                }

                // Install SRCDS
                installServerFile(Global.BASE_PATH + SOURCE_PATH + "srcds.exe", DOWNLOAD_PATH_SRCDS, Global.BASE_PATH + SOURCE_PATH, "srcds", () =>
                {
                    // Install Metamod
                    installServerFile(Global.BASE_PATH + SOURCE_PATH + @"dota\addons\metamod.vdf", DOWNLOAD_PATH_METAMOD, Global.BASE_PATH + SOURCE_PATH + @"dota\", "metamod", () =>
                    {
                        // Install d2fixups
                        installServerFile(Global.BASE_PATH + SOURCE_PATH + @"dota\addons\metamod\d2fixups.vdf", DOWNLOAD_PATH_D2FIXUPS, Global.BASE_PATH + SOURCE_PATH + @"dota\", "d2fixups", () =>
                        {
                            // Patch the gameinfo file
                            source1GameInfoPatch();

                            // Patch the maps
                            patchSource1Maps();
                            
                            // Finished installing
                            Helpers.log("Finished installing servers!");
                        });
                    });
                });
            });
        }

        // Patches gameinfo.txt for source1
        // additonalMount is an extra location to mount into the server
        private static void source1GameInfoPatch(string additonalMount=null)
        {
            // Ensure additonalMount is valid
            if (additonalMount == null || additonalMount == "")
            {
                additonalMount = "";
            }
            else
            {
                additonalMount = "Game " + additonalMount + Environment.NewLine;
            }

            // Gameinfo to load metamod
            string gameinfo = 
                "\"GameInfo\"" + Environment.NewLine + 
                    "{" + Environment.NewLine +
                        "game \"DOTA 2\"" + Environment.NewLine+
                        "gamelogo 1"+ Environment.NewLine +
                        "type multiplayer_only"+ Environment.NewLine +
                        "nomodels 1"+ Environment.NewLine +
                        "nohimodel 1"+ Environment.NewLine +
                        "nocrosshair 0" + Environment.NewLine + "GameData \"dota.fgd\"" + Environment.NewLine +
                        "SupportsDX8 0" + Environment.NewLine +
                        "FileSystem" + Environment.NewLine +
                        "{" + Environment.NewLine +
                            "SteamAppId 816"+ Environment.NewLine +
                            "ToolsAppId 211" + Environment.NewLine +
                            "SearchPaths" + Environment.NewLine +
                            "{" + Environment.NewLine +
                                "GameBin |gameinfo_path|addons/metamod/bin" + Environment.NewLine +
                                "Game |gameinfo_path|." + Environment.NewLine +
                                "Game platform" + Environment.NewLine +
                                additonalMount +
                            "}" + Environment.NewLine +
                        "}" + Environment.NewLine +
                    "}";

            // May need to add addon mounting here eventually
            
            // Write the metamod loader
            File.WriteAllText(Global.BASE_PATH + SOURCE_PATH + @"dota\gameinfo.txt", gameinfo);
        }

        // This function patches source1 maps
        // This function could could issues with data[i++] if the map is made to fail on purpose
        // This function won't fail on standard source maps, since we have control over this,
        // Everything is good in the hood
        private static void patchSource1Maps()
        {
            // List of maps to patch
            string[] maps = new string[]
            {
                "dota",
                "dota_681",
                "dota_autumn",
                "dota_diretide_12",
                "dota_winter"
            };

            // Path to the map folder
            string mapPath = Global.BASE_PATH + SOURCE_PATH + @"dota\maps\";

            // Loop over each map
            foreach(string map in maps)
            {
                // Debug log
                Helpers.log("Attempting to patch " + map);

                // Make a copy of the map
                System.IO.File.Copy(mapPath + map + ".bsp", mapPath + map + ".bsp.tmp", true);

                // Delete the original map
                File.Delete(mapPath + map + ".bsp");

                // Load up the map
                using (FileStream fs = File.Open(mapPath + map + ".bsp.tmp", FileMode.Open))
                {
                    // Read in the map data
                    byte[] data = new BinaryReader(fs).ReadBytes((int)fs.Length);

                    // Search over data
                    for (int i = 0; i < data.Length-10; )
                    {
                        // Searching for `world_maxs` basically
                        if (data[i++] == 'w' &&
                            data[i++] == 'o' &&
                            data[i++] == 'r' &&
                            data[i++] == 'l' &&
                            data[i++] == 'd' &&
                            data[i++] == '_' &&
                            data[i++] == 'm' &&
                            data[i++] == 'a' &&
                            data[i++] == 'x' &&
                            data[i++] == 's')
                        {
                            // Matched, lets find where the numbers begin
                            while (i < data.Length && data[i++] != '"') ;
                            while (i < data.Length && data[i++] != '"') ;

                            // i is now positioned at the numbers
                            data[i++] = (byte)'8';
                            data[i++] = (byte)'3';
                            data[i++] = (byte)'2';
                            data[i++] = (byte)'0';

                            // Store that we patched successfully
                            Helpers.log(map + " was patched successfully!");
                        }
                    }

                    // Write the new map
                    using (BinaryWriter writer = new BinaryWriter(File.Open(mapPath + map + ".bsp", FileMode.Create)))
                    {
                        // Write the data
                        writer.Write(data);
                    }
                }

                // Delete the copy of the map
                File.Delete(mapPath + map + ".bsp.tmp");
            }
        }

        private static string getGameServersAsString()
        {
            string ret = "";
            for (byte i = 0; i < gameServers.Count; ++i)
            {
                ret += gameServers[i].Name + ";" + gameServers[i].Players.Count + ";";
            }
            if (ret.Length > 0)
            {
                ret.Remove(ret.Length - 1, 1);
            }
            return ret;
        }

        // Gets the current CPU usage in percent
        private static int getCurrentCpuUsage()
        {
            // Call is required twice because Windows
            cpuCounter.NextValue();
            // Sleep is required to delay next call because Windows
            System.Threading.Thread.Sleep(1000);
            return (int)Math.Round(cpuCounter.NextValue());
        }
       
        // Gets the available RAM in the system in megabytes
        private static int getAvailableRAM()
        {
            return Convert.ToInt32(Math.Round(ramCounter.NextValue()));
        }

        // Gets the total RAM available in the system in megabytes
        private static int getTotalRAM()
        {
            return  Convert.ToInt32(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1000000);
        }

        // Gets the current upload speed in bytes/second
        private static int getUploadSpeed()
        {
            int upload = 0;
            for (int i = 0; i < dataSentCounter.Count; ++i)
            {
                // Iterates through all adapters and sums them
                upload += Convert.ToInt32(Math.Round(dataSentCounter[i].NextValue()));
            }
            return upload;
        }

        // Gets the current download speed in bytes/second
        private static int getDownloadSpeed()
        {
            int download = 0;
            for (int i = 0; i < dataSentCounter.Count; ++i)
            {
                // Iterates through all adapters and sums them
                download += Convert.ToInt32(Math.Round(dataReceivedCounter[i].NextValue()));
            }
            return download;
        }

        // Returns a list of all network interface card reference names
        private static string[] getNetworkCards()
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");
            string[] instancename = category.GetInstanceNames();
            return instancename;
        }

    }
}
