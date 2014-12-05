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
        private static GameServers gameServers = new GameServers();

        // This box manager
        private static BoxManager boxManager = new BoxManager();

        // Box Server status
        private static byte status;

        // Box Server subID
        private static int subID;

        #endregion

        // The main entry point into the program
        private static void Main(string[] args)
        {
            // Ensure temp dir exists
            Directory.CreateDirectory(Global.TEMP);

            // Delete the old log file
            File.Delete(Global.BASE_PATH + "log.txt");

            GameServer gs = new GameServer();
            gs.Ip = "yolo";
            gs.Port = 27015;
            Lobby l = new Lobby();
            Addons ads = new Addons();
            Addon ad = new Addon();
            ad.Id = "lod";
            ad.Options = new Options();
            ad.Options.setOption("pickingMode", "All Pick");
            ads.addAddon(ad);
            l.Addons = ads;
            l.CurrentPlayers = 3;
            l.MaxPlayers = 5;
            l.Name = "trolol";
            Teams ts = new Teams();

            // First team, with us on it
            Team t = new Team();
            t.MaxPlayers = 5;
            Players ps = new Players();
            Player p = new Player();
            p.Avatar = "avatar URL here";
            p.PersonaName = "some personan name";
            p.ProfileURL = "http://steamcommunity.com/jexah";
            p.SteamID = "45686503";
            //p.SteamID = "41686503";
            ps.addPlayer(p);
            Player p2 = new Player();
            p2.Avatar = "avatar URL here";
            p2.PersonaName = "some personan name";
            p2.ProfileURL = "http://steamcommunity.com/jexah";
            //p.SteamID = "45686503";
            p2.SteamID = "28090256";
            ps.addPlayer(p2);
            t.Players = ps;
            t.TeamName = "teamMeowingtons";

            // Second team, dummy player
            Team t2 = new Team();
            t2.MaxPlayers = 5;
            t2.TeamName = "teamMeowingtons";
            Players ps2 = new Players();
            Player p3 = new Player();
            p3.Avatar = "avatar URL here";
            p3.PersonaName = "some personan name";
            p3.ProfileURL = "http://steamcommunity.com/jexah";
            p3.SteamID = "28123256";
            ps2.addPlayer(p3);
            t2.Players = ps2;

            // Add second team first
            ts.addTeam(t2);
            ts.addTeam(t);
            l.Teams = ts;
            gs.Lobby = l;
            gameServers.addGameServer(gs);

            Helpers.log(ts.toJSON());

            /*/ Compile our test settings
            AddonCompiler.compileAddons(l, Global.BASE_PATH + @"addons\", true);
            return;//*/

            // Cleanup addons folder
            Helpers.deleteFolder(Global.BASE_PATH + "addons\\", true);

            // Attempt to install Legends of Dota
            AddonDownloader.updateAddon("lod", (addonID, success) =>
            {
                // Check if it worked!
                if (success)
                {
                    Helpers.log(addonID + " was successfully installed!");
                }
                else
                {
                    Helpers.log(addonID + " failed to install!");
                }
            });

            // Attempt to install serverinit (server scripts)
            AddonDownloader.updateAddon("serverinit", (addonID, success) =>
            {
                // Check if it worked!
                if (success)
                {
                    Helpers.log(addonID + " was successfully installed!");
                }
                else
                {
                    Helpers.log(addonID + " failed to install!");
                }
            });

            status = Vultr.BOX_IDLE;

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
            // Log everything that is sent, for debugging
            #region wsClient.addHook(WebSocketClient.RECEIVE);
            wsClient.addHook(WebSocketClient.SEND, (c) =>
            {
                Helpers.log("SENT SOMETHING");
            });
            #endregion

            // Log everything that is received, for debugging
            #region wsClient.addHook(WebSocketClient.RECEIVE);
            wsClient.addHook(WebSocketClient.RECEIVE, (c) =>
            {
                Helpers.log("RECEIVE: " + c.DataFrame.ToString());
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
                status = Vultr.BOX_DEACTIVATED;
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
                    status = Vultr.BOX_DEACTIVATED;
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
                boxManager.Ip = x[1];
            });
            #endregion

            // Get unique socket identifier from server
            #region wsClient.addHook("box");
            wsClient.addHook("box", (c, x) =>
            {
                boxManager = new BoxManager(KV.parse(x[1]).getKV("box"));
                c.Send("system;" + boxManager.toString("box"));
            });
            #endregion

            // Get status overview
            #region wsClient.addHook("system");
            wsClient.addHook("system", (c, x) =>
            {
                refreshSystemDiagnostics();
                if (status != Vultr.BOX_DEACTIVATED)
                {
                    if (gameServers.getKeys() != null && gameServers.getKeys().Count == 0)
                    {
                        status = Vultr.BOX_IDLE;
                    }
                    else
                    {
                        status = Vultr.BOX_ACTIVE;
                    }
                    c.Send("system;" + boxManager.toString("box"));
                }

            });
            #endregion

            // Create game server function
            #region wsClient.addHook("create");
            wsClient.addHook("create", (c, x) =>
            {

                // Create server object to handle game server info
                GameServer gameServer = new GameServer(KV.parse(x[1]));

                gameServers.addGameServer(gameServer);

                // Launch the server using the string options
                launchGameServer(gameServer);

            });
            #endregion

            #region wsClient.addHook("updateServer");
            wsClient.addHook("updateServer", (c, x) =>
            {
                updateServers();
            });
            #endregion


        }

        // Starts a specific game server with a specific set of arguments
        private static void launchGameServer(GameServer gameServer)
        {
            // NOTE: WE NEED TO ENSURE ONLY ONE SERVER IS BOOTING AT A TIME, OR WE WILL HAVE A SHIT STORM!

            // ASSUMPTION: THE SERVERS ARE FULLY INSTALLED AND ADDONS ARE GOOD TO LOAD

            // We probably want to report that the server failed, if it did infact fail


            // BEGIN OPTIONS: SHOULD AUTO FILL THESE

            // Should we use source1?
            bool source1 = true;

            // Should adjust this where nessessary
            int maxPlayers = gameServer.Lobby.MaxPlayers;

            // Port to open on, perhaps we keep track of what ports are in use? or test? idk
            int port = gameServer.Port;

            // The map to load up
            string map = "dota";

            // The path to the addons we need to mount (this will be generated by addon compiler)
            // Once the server has closed, the mount path will need to be deleted as well!
            // NOTE: This function takes another argument which will be the gameServerArgs -- this still needs to be built
            string mountPath = AddonCompiler.compileAddons(gameServer.Lobby, Global.BASE_PATH + @"addons\", true);

            // END OPTIONS



            // The path to SRCDS
            string path = Global.BASE_PATH + SOURCE_PATH;

            // The application to launch
            string app;

            // The arguments to launch with
            string args;

            // Build arguments based on server version required
            if (source1)
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

                wsClient.send("gameServerInfo;success;" + gameServer.toString());

                // We probably want to store a reference to the process so we can see if it dies
            }
            catch
            {

                wsClient.send("gameServerInfo;" + gameServer.Lobby.Name + ";failed");
                Helpers.log("Failed to launch the server!");
            }
        }

        // Reboot check loop
        private static void rebootLoop()
        {
            if (gameServers.getKeys().Count > 0)
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
            if (gameServers.getKeys().Count > 0)
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
        private static void refreshSystemDiagnostics()
        {
            boxManager.GameServers = gameServers;
            boxManager.Cpu = getCurrentCpuUsage();
            boxManager.RamAvailable = getAvailableRAM();
            boxManager.RamTotal = getTotalRAM();
            boxManager.Upload = getUploadSpeed();
            boxManager.Download = getDownloadSpeed();
        }

        // This function ensures steamcmd is available
        private static void installServerFile(string localFileCheck, string downloadURL, string extractTo, string friendlyName, Action callback)
        {
            // Check if steamcmd exists
            if (!File.Exists(localFileCheck))
            {
                // Debug log
                Helpers.log(friendlyName + " not found, downloading...");

                // Name of the zip to use
                string zipName = friendlyName + ".zip";

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
        private static void source1GameInfoPatch(string additonalMount = null)
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
                        "game \"DOTA 2\"" + Environment.NewLine +
                        "gamelogo 1" + Environment.NewLine +
                        "type multiplayer_only" + Environment.NewLine +
                        "nomodels 1" + Environment.NewLine +
                        "nohimodel 1" + Environment.NewLine +
                        "nocrosshair 0" + Environment.NewLine + "GameData \"dota.fgd\"" + Environment.NewLine +
                        "SupportsDX8 0" + Environment.NewLine +
                        "FileSystem" + Environment.NewLine +
                        "{" + Environment.NewLine +
                            "SteamAppId 816" + Environment.NewLine +
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
            foreach (string map in maps)
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
                    for (int i = 0; i < data.Length - 10; )
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

        // Gets the current CPU usage in percent
        private static byte getCurrentCpuUsage()
        {
            // Call is required twice because Windows
            cpuCounter.NextValue();

            // Sleep is required to delay next call because Windows
            System.Threading.Thread.Sleep(1000);
            return (byte)Math.Round(cpuCounter.NextValue());
        }

        // Gets the available RAM in the system in megabytes
        private static ushort getAvailableRAM()
        {
            return (ushort)Math.Round(ramCounter.NextValue());
        }

        // Gets the total RAM available in the system in megabytes
        private static ushort getTotalRAM()
        {
            return (ushort)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1000000);
        }

        // Gets the current upload speed in bytes/second
        private static uint getUploadSpeed()
        {
            uint upload = 0;
            for (int i = 0; i < dataSentCounter.Count; ++i)
            {
                // Iterates through all adapters and sums them
                upload += (uint)(Math.Round(dataSentCounter[i].NextValue()));
            }
            return upload;
        }

        // Gets the current download speed in bytes/second
        private static uint getDownloadSpeed()
        {
            uint download = 0;
            for (int i = 0; i < dataSentCounter.Count; ++i)
            {
                // Iterates through all adapters and sums them
                download += (uint)(Math.Round(dataReceivedCounter[i].NextValue()));
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
