using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DotaHostBoxManager
{
    class Program
    {

        #region Constants

        // The path to steamcmd
        private const string SteamcmdPath = @"steamcmd\";

        // The steam cmd file to run steam commands with
        private const string Steamcmd = SteamcmdPath + "steamcmd.exe";

        // The base path to our files
        private const string DownloadPathBase = "http://dotahost.net/files/";

        // The path to download steamcmd from
        private const string DownloadPathSteamcmd = DownloadPathBase + "steamcmd.zip";

        // URL to download SRCDS from
        private const string DownloadPathSrcds = DownloadPathBase + "srcds.zip";

        // URL to download metamod from
        private const string DownloadPathMetamod = DownloadPathBase + "mmsource.zip";

        // URL to download d2fixups from
        private const string DownloadPathD2Fixups = DownloadPathBase + "d2fixups.zip";

        // The path to the source servers
        private const string SourcePath = @"dota\";

        // The username to download files with (Username and password should probably be exported somewhere)
        private const string SteamUsername = "dotahost_net";

        // The password to download files with
        private const string SteamPassword = "***REMOVED***";

        // The minidump error
        private const string MinidumpError = "Setting breakpad minidump AppID = 580";

        #endregion

        #region Static Readonlys

        // Network card set up for network monitoring
        private static readonly string[] NetworkCards = GetNetworkCards();

        // The command to update the servers
        private const string SteamcmdUpdateServers = "+login " + SteamUsername + " " + SteamPassword + " +runscript install.txt";

        // The update file for source1
        // Need to add Environment.NewLine + "app_update 316570", in order to install/update source2
        private static readonly string SteamcmdUpdatefile = "@ShutdownOnFailedCommand 0" + Environment.NewLine + "force_install_dir " + Global.BasePath + SourcePath + Environment.NewLine + "app_update 570" + Environment.NewLine + "quit";

        #endregion

        #region Private variables

        // Performance monitoring
        private static readonly PerformanceCounter CpuCounter = new PerformanceCounter();
        private static readonly PerformanceCounter RamCounter = new PerformanceCounter("Memory", "Available MBytes");
        private static readonly List<PerformanceCounter> DataSentCounter = new List<PerformanceCounter>();
        private static readonly List<PerformanceCounter> DataReceivedCounter = new List<PerformanceCounter>();

        // Used for downloading files
        private static readonly DownloadManager DlManager = new DownloadManager();

        // Web socket client
        private static readonly WebSocketClient WsClient = new WebSocketClient("ws://" + Runabove.ServerManagerIp + ":" + Runabove.ServerManagerPort + "/");

        // Unique websocket client ID
        public static string WebsocketUserId { get; set; }

        // List of game server running on the box
        private static readonly GameServers GameServers = new GameServers();

        // This box manager
        private static BoxManager _boxManager = new BoxManager();

        // Box Server status
        private static byte _status;

        private static bool connectedToServerManager = false;

        #endregion

        // The main entry point into the program
        private static void Main()
        {
            // Ensure temp dir exists
            Directory.CreateDirectory(Global.Temp);

            // Delete the old log file
            File.Delete(Global.BasePath + "log.txt");



            /*/ Compile our test settings
            AddonCompiler.compileAddons(l, Global.BASE_PATH + @"addons\", true);
            return;//*/

            // Update addons (and serverinit)
            UpdateAddons(true);

            // Set box status to idle.
            _status = Runabove.BoxIdle;

            // Prepare to calculate + send system diagnostics.
            SetupSystemDiagnostics();

            // Hook the websocket events/
            HookWSocketEvents();

            // Start the websocket client
            WsClient.Start();





            // Update the dota install
            //updateServers();
        }

        // Updates serverinit.zip
        private static void UpdateServerInit()
        {
            try
            {
                // Attempt to install serverinit (server scripts)
                DlManager.DownloadSync(string.Format(Global.DownloadPathAddonInfo, "serverinit"), Global.Temp + "serverinit.txt");

                // Read the file to a variable
                string[] crcCommitId = File.ReadAllLines(Global.Temp + "serverinit.txt");

                // Delete the serverinit.txt
                File.Delete(Global.Temp + "serverinit.txt");

                // Download the serverinit.zip
                DlManager.DownloadSync(Global.ServerinitDownload, Global.Temp + "serverinit.zip");

                // Calculate the CRC of the downloaded zip file.
                string downloadedCrc = Helpers.CalculateCrc(Global.Temp + "serverinit.zip");

                // Check if the downloaded zip file CRC matches the CRC found in the serverinit.txt file
                if (downloadedCrc == crcCommitId[0])
                {
                    // They match
                    Helpers.Log("[ServerInit] Latest version aquired, installing...");

                    // Delete old folder if still here for some reason
                    Helpers.DeleteFolder(Global.Temp + "serverinit", true);

                    // Extract the new serverinit.zip to serverinit
                    ZipFile.ExtractToDirectory(Global.Temp + "serverinit.zip", Global.Temp + "serverinit");

                    // Delete the downloaded serverinit.zip
                    File.Delete(Global.Temp + "serverinit.zip");

                    // Zip up the second level folder in the extracted zip, to serverinit.zip so it matches the compileAddons function.
                    ZipFile.CreateFromDirectory(Global.Temp + @"serverinit\Jexah-DotaHostServerInit-" + crcCommitId[1], Global.Temp + "serverinit.zip");

                    // If the addon install location exists, delete serverinit.zip from it.
                    if (Directory.Exists(string.Format(Global.ClientAddonInstallLocation, Global.BasePath)))
                    {
                        File.Delete(string.Format(Global.ClientAddonInstallLocation, Global.BasePath) + "serverinit.zip");
                    }

                    // Delete the serverinit.zip
                    File.Delete(AddonDownloader.GetAddonInstallLocation() + "serverinit.zip");

                    // Move the zip into the addon install location
                    File.Move(Global.Temp + "serverinit.zip", AddonDownloader.GetAddonInstallLocation() + "serverinit.zip");

                    // Delete the temp serverinit folder
                    Helpers.DeleteFolder(Global.Temp + "serverinit", true);

                    Helpers.Log("[ServerInit] Successfully updated!");

                    // Successfully updated
                }
                else
                {
                    Helpers.Log(string.Format("[ServerInit] Download failed, CRC mismatch: {0} : {1}", downloadedCrc, crcCommitId[0]));

                    // Failed to update
                }
            }
            catch
            {
                Helpers.Log("[ServerInit] Update failed, unknown error");

                // Failed to update
            }
        }

        // Updates all addons in DotaHost library
        private static void UpdateAddons(bool serverinit)
        {
            Helpers.Log("[Addons] Starting update of all addons.");

            // Cleanup addons folder
            Helpers.DeleteFolder(Global.BasePath + "addons\\", true);

            // Download list of addons.
            DlManager.DownloadSync(Global.Root + "addons/addons.txt", "addons.txt");

            // Store list in array
            string[] addons = File.ReadAllLines(Global.BasePath + "addons.txt");

            // For each addon
            for (byte i = 0; i < addons.Length; ++i)
            {
                // Attempt to install addon
                AddonDownloader.UpdateAddon(addons[i], (addonId, success) =>
                {
                    // Check if it worked!
                    if (success)
                    {
                        Helpers.Log(addonId + " was successfully installed!");
                    }
                    else
                    {
                        Helpers.Log(addonId + " failed to install!");
                    }
                });
            }

            // If serverinit should be updated too...
            if (serverinit)
            {
                // Update it.
                UpdateServerInit();
            }

            Helpers.Log("[Addons] Update complete.");
        }

        // Iterates through the network cards, adding them to dataSendCounter and dataReceivedCounter
        private static void SetupNetworkCards()
        {
            // For each network card
            for (byte i = 0; i < NetworkCards.Length; ++i)
            {
                // Add a performance counter to dataSent
                DataSentCounter.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", NetworkCards[i]));

                // And received
                DataReceivedCounter.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", NetworkCards[i]));
            }
        }

        // ID of boxmanager instance
        public static string InstanceId { get; set; }

        // Hook websocket events
        private static void HookWSocketEvents()
        {
            // Log everything that is sent, for debugging
            WsClient.AddHook(WebSocketClient.TypeSend, BoxManagerHook);

            // Log everything that is received, for debugging
            WsClient.AddHook(WebSocketClient.TypeReceive, ReceiveHook);

            // When connected to the ServerManager, send alert and request information and instructions
            WsClient.AddHook(WebSocketClient.TypeConnected, ConnectedHook);

            // When connected to the ServerManager, send alert and request information and instructions
            WsClient.AddHook(WebSocketClient.TypeDisconnected, DisconnectedHook);

            // Begin server reboot
            WsClient.AddHook("reboot", RebootHook);

            // Server destroy (soft/hard)
            WsClient.AddHook("destroy", DestroyHook);

            // Receive subid from server manager
            WsClient.AddHook("instanceid", InstanceidHook);

            // Get unique socket identifier from server
            WsClient.AddHook("id", IdHook);

            // Get unique socket identifier from server
            WsClient.AddHook("box", BoxHook);

            // Get status overview
            WsClient.AddHook("system", SystemHook);

            // Create game server function
            WsClient.AddHook("create", CreateHook);

            // Updates server
            WsClient.AddHook("updateServer", UpdateServerHook);
        }

        private static void BoxManagerHook(UserContext c)
        {
            Helpers.Log("SENT SOMETHING");
        }

        private static void ReceiveHook(UserContext c)
        {
            Helpers.Log("RECEIVE: " + c.DataFrame);
        }

        private static void ConnectedHook(UserContext c)
        {
            c.Send("box");
            connectedToServerManager = true;
        }

        private static void DisconnectedHook(UserContext c)
        {
            connectedToServerManager = false;
            AttemptReconnect();

            Helpers.Log("Disconnected, starting timeout");
        }

        private static void AttemptReconnect()
        {
            Timers.SetTimeout(1, Timers.Seconds, () =>
            {
                if (connectedToServerManager) return;

                Helpers.Log("Attempting connect");

                WsClient.Start();
                AttemptReconnect();
            });
        }

        private static void RebootHook(UserContext c, string[] x)
        {
            // Set status to deactivated
            _status = Runabove.BoxDeactivated;

            // Begin reboot loop.
            RebootLoop();
        }

        private static void InstanceidHook(UserContext c, string[] x)
        {

            // Update instanceID
            InstanceId = x[1];
        }


        private static void DestroyHook(UserContext c, string[] x)
        {

            // If requested hard destroy
            if (x.Length > 1 && x[1] == "hard")
            {
                // Hard destroy, no waiting, no timeout
                Destroy();
            }
            else
            {
                // Soft destroy, waits for games to finish, polls every minute
                _status = Runabove.BoxDeactivated;
                DestroyLoop();
            }
        }

        private static void IdHook(UserContext c, string[] x)
        {

            // Update ip
            _boxManager.Ip = x[1];
        }

        private static void BoxHook(UserContext c, string[] x)
        {

            _boxManager = new BoxManager(Kv.Parse(x[1]));
            c.Send(Helpers.PackArguments("system", _boxManager.ToString()));
        }

        private static void SystemHook(UserContext c, string[] x)
        {
            // Update system diagnostics
            RefreshSystemDiagnostics();

            // If this server isn't deactivated
            if (_status == Runabove.BoxDeactivated) return;

            // If there are no game servers running
            if (GameServers.GetKeys() != null && GameServers.GetKeys().Count == 0)
            {
                // Set box status to idle.
                _status = Runabove.BoxIdle;
            }
            else
            {
                // There are game servers running, we are active.
                _status = Runabove.BoxActive;
            }

            // Send updated box manager info to server manager.
            c.Send(Helpers.PackArguments("system", _boxManager.ToString()));
        }

        private static void CreateHook(UserContext c, string[] x)
        {

            // Create server object to handle game server info
            var gameServer = new GameServer(Kv.Parse(x[1]));

            // Adds game server to game server list.
            GameServers.AddGameServer(gameServer);

            // Launch the server using the string options
            LaunchGameServer(gameServer);
        }

        private static void UpdateServerHook(UserContext c, string[] x)
        {

            // Updates server
            UpdateServers();
        }

        // Starts a specific game server with a specific set of arguments
        private static void LaunchGameServer(GameServer gameServer)
        {
            // NOTE: WE NEED TO ENSURE ONLY ONE SERVER IS BOOTING AT A TIME, OR WE WILL HAVE A SHIT STORM!

            // ASSUMPTION: THE SERVERS ARE FULLY INSTALLED AND ADDONS ARE GOOD TO LOAD

            // We probably want to report that the server failed, if it did infact fail

            Helpers.Log("Here we go...");

            // Update addons
            UpdateAddons(true);


            // BEGIN OPTIONS: SHOULD AUTO FILL THESE

            // Should we use source1?
            bool source1 = true;

            // Should adjust this where nessessary
            int maxPlayers = gameServer.Lobby.MaxPlayers;

            // Port to open on, perhaps we keep track of what ports are in use? or test? idk
            int port = gameServer.Port;

            // The map to load up
            const string map = "dota";

            // The path to the addons we need to mount (this will be generated by addon compiler)
            // Once the server has closed, the mount path will need to be deleted as well!
            // NOTE: This function takes another argument which will be the gameServerArgs -- this still needs to be built
            var mountPath = AddonCompiler.CompileAddons(gameServer.Lobby, AddonDownloader.GetAddonInstallLocation(), Global.BasePath + @"addons_dotahost\", true);
            mountPath = mountPath.Substring(0, mountPath.Length - 1);

            // END OPTIONS



            // The path to SRCDS
            string path = Global.BasePath + SourcePath;

            // The application to launch
            string app;

            // The arguments to launch with
            string args;

            // Build arguments based on server version required
            if (source1)
            {
                // Source1 Settings
                app = "srcds.exe";
                args = "+meta list -console -game dota +maxplayers " + maxPlayers + " -port " + port + " +dota_force_gamemode 15 +dota_local_addon_enable 1 +map " + map;
                //srcds  -console -game dota +maxplayers 24 +hostport 27016 -condebug -dev +dota_force_gamemode 15 +dota_local_addon_enable 1 +map dota
                // -strictportbind doesn't appear to work, doh!

                // Patch gameinfo.txt
                Source1GameInfoPatch(mountPath);
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
            var proc = new ProcessStartInfo
            {
                WorkingDirectory = path,
                FileName = path + app,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            // Attempt to launch the server
            //try
            //{
            // Start the process
            var process = Process.Start(proc);

            // Set to true once STDOUT has been fully read.
            var canStop = false;

            // Create diconary to check who has played
            var connections = new Dictionary<string, int>();

            // Default everyone to not connected yet
            gameServer.Lobby.ForEachPlayer((player) => connections[player.SteamId] = Global.PlayerStatusNotConnected);

            // Did the server even activate?
            bool activated = false;

            // Error manager
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                if (process != null)
                {
                    // Wait for an error (if process exits, this will be null)
                    var stderrx = process.StandardError.ReadLine();

                    // Check for minidump error
                    if (stderrx == MinidumpError) stderrx = null;

                    // Ensure the process is dead
                    if (!process.HasExited) process.Kill();

                    // Wait for SRDOUT to be done
                    while (!canStop) System.Threading.Thread.Sleep(100);

                    // DEBUG: Print who has connected and who hasn't
                    foreach (var pair in connections)
                    {
                        Helpers.Log(port + ": " + pair.Key + " - " + pair.Value);
                    }

                    // DEBUG: Log if something went REALLY wrong
                    if (!activated)
                    {
                        Helpers.Log(port + ": The server didnt even activate, we have a SERIOUS problem!");
                    }

                    // Check if we got an error
                    if (stderrx == null)
                    {
                        // Log the error
                        Helpers.Log(port + ": Game server exited normally");

                        // No error, server exited, report to master server
                        WsClient.Send(Helpers.PackArguments("gameServerExit", "good", gameServer.ToString()));
                    }
                    else
                    {
                        // Log the error
                        Helpers.Log(port + ": SRCDS Error: " + stderrx);

                        // Report error to master server
                        WsClient.Send(Helpers.PackArguments("gameServerExit", "error", gameServer.ToString(), stderrx));
                    }
                }

                GameServers.RemoveGameServer(gameServer);

                // Cleanup the addon folder
                Helpers.DeleteFolder(mountPath, true);
            }, null);

            // STDOUT watch dog
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                while (true)
                {
                    if (process == null) continue;

                    // Read a line and check if it;s the end of our input
                    var line = process.StandardOutput.ReadLine();
                    if (line == null) break;

                    // Check for Lua data
                    var message = line.Split('\u0007');

                    if (message.Length <= 1) continue;

                    switch (message[0])
                    {
                        // The server activated successfully
                        case "activate":
                            // Log it
                            Helpers.Log(port + ": Activated successfully!");
                            activated = true;

                            // Report to the master server
                            WsClient.Send(Helpers.PackArguments("gameServerInfo", "success", gameServer.ToString()));
                            break;

                        // Output from a mod, lets just log for now
                        case "print":
                            Helpers.Log(port + ": " + message[1]);
                            break;

                        // A user connects successfully
                        case "connect":
                            Helpers.Log(port + ": " + message[1] + " connected.");
                            connections[message[1]] = Global.PlayerStatusConnected;
                            break;

                        // A user disconnected
                        case "disconnect":
                            Helpers.Log(port + ": " + message[1] + " disconnected.");
                            connections[message[1]] = Global.PlayerStatusDisconnected;
                            break;

                        // Unknown message, doh!
                        default:
                            Helpers.Log(port + ": " + message[0] + " = " + message[1]);
                            break;
                    }
                }

                // STDOUT is done
                canStop = true;
            }, null);

            // Woot, success
            Helpers.Log(port + ": Launched successfully!");

            // We probably want to store a reference to the process so we can see if it dies
            //}
            //catch (Exception e)
            //{

            //    wsClient.send(Helpers.packArguments("gameServerInfo", "failed", gameServer.toString()));
            //    Helpers.log("Failed to launch the server!");
            //}
        }

        // Reboot check loop
        private static void RebootLoop()
        {
            if (GameServers.GetKeys().Count > 0)
            {
                // Game server still running, check again in 60 seconds
                Timers.SetTimeout(60, Timers.Seconds, RebootLoop);
            }
            else
            {
                // Reboot server
                Process.Start("shutdown.exe", "-r -t 0");
            }
        }

        // Destroy check loop
        private static void DestroyLoop()
        {
            if (GameServers.GetKeys().Count > 0)
            {
                // Game servers still running, check again in 60 seconds
                Timers.SetTimeout(60, Timers.Seconds, DestroyLoop);
            }
            else
            {
                // Destroy server
                //Runabove.destroyServer(instanceID);
            }
        }

        // Destroy server instantly
        private static void Destroy()
        {
            //Runabove.destroyServer(instanceID);
        }

        // Set up system diagnostics
        private static void SetupSystemDiagnostics()
        {
            SetupNetworkCards();
            CpuCounter.CategoryName = "Processor";
            CpuCounter.CounterName = "% Processor Time";
            CpuCounter.InstanceName = "_Total";
        }

        // Get system diagnostics such as CPU % usage, RAM used and RAM remaining
        private static void RefreshSystemDiagnostics()
        {
            _boxManager.GameServers = GameServers;
            _boxManager.Cpu = GetCurrentCpuUsage();
            _boxManager.RamAvailable = GetAvailableRam();
            _boxManager.RamTotal = GetTotalRam();
            _boxManager.Upload = GetUploadSpeed();
            _boxManager.Download = GetDownloadSpeed();
        }

        // This function ensures steamcmd is available
        private static void InstallServerFile(string localFileCheck, string downloadUrl, string extractTo, string friendlyName, Action callback)
        {
            // Check if steamcmd exists
            if (!File.Exists(localFileCheck))
            {
                // Debug log
                Helpers.Log(friendlyName + " not found, downloading...");

                // Name of the zip to use
                string zipName = friendlyName + ".zip";

                // If there is an old version of steamcmd.zip, delete it
                File.Delete(Global.BasePath + zipName);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                DlManager.Download(downloadUrl, zipName, (e) => { }, (e) =>
                {
                    // Extract the archive
                    ZipFile.ExtractToDirectory(zipName, extractTo);

                    // Delete the zip
                    File.Delete(Global.BasePath + zipName);

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
        private static void UpdateServers()
        {
            // Debug log
            Helpers.Log("Updating dota 2 (source2)...");

            // Ensure steamcmd exists
            InstallServerFile(Global.BasePath + Steamcmd, DownloadPathSteamcmd, Global.BasePath + SteamcmdPath, "steamCMD", () =>
            {
                // Ensure the directory exists
                Directory.CreateDirectory(SourcePath);

                // Create the path command file
                File.WriteAllText(Global.BasePath + SteamcmdPath + "install.txt", SteamcmdUpdatefile);

                // Build the update commmand
                var proc = new ProcessStartInfo
                {
                    WorkingDirectory = Global.BasePath,
                    FileName = Steamcmd,
                    Arguments = SteamcmdUpdateServers
                };

                // Attempt to run the update
                try
                {
                    // Start the process
                    var process = Process.Start(proc);

                    // Wait for it to end
                    if (process != null) process.WaitForExit();
                }
                catch
                {
                    Helpers.Log("Failed to update!");
                    return;
                }

                // Install SRCDS
                InstallServerFile(Global.BasePath + SourcePath + "srcds.exe", DownloadPathSrcds, Global.BasePath + SourcePath, "srcds", () =>
                {
                    // Install Metamod
                    InstallServerFile(Global.BasePath + SourcePath + @"dota\addons\metamod.vdf", DownloadPathMetamod, Global.BasePath + SourcePath + @"dota\", "metamod", () =>
                    {
                        // Install d2fixups
                        InstallServerFile(Global.BasePath + SourcePath + @"dota\addons\metamod\d2fixups.vdf", DownloadPathD2Fixups, Global.BasePath + SourcePath + @"dota\", "d2fixups", () =>
                        {
                            // Patch the gameinfo file
                            Source1GameInfoPatch();

                            // Patch the maps
                            PatchSource1Maps();

                            // Finished installing
                            Helpers.Log("Finished installing servers!");
                        });
                    });
                });
            });
        }

        // Patches gameinfo.txt for source1
        // additonalMount is an extra location to mount into the server
        private static void Source1GameInfoPatch(string additonalMount = null)
        {
            // Ensure additonalMount is valid
            if (string.IsNullOrEmpty(additonalMount))
            {
                additonalMount = "";
            }
            else
            {
                additonalMount = "Game " + '"' + additonalMount + '"' + Environment.NewLine;
            }

            // Gameinfo to load metamod
            var gameinfo =
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
            File.WriteAllText(Global.BasePath + SourcePath + @"dota\gameinfo.txt", gameinfo);
        }

        // This function patches source1 maps
        // This function could could issues with data[i++] if the map is made to fail on purpose
        // This function won't fail on standard source maps, since we have control over this,
        // Everything is good in the hood
        private static void PatchSource1Maps()
        {
            // List of maps to patch
            string[] maps = {
				"dota",
				"dota_681",
				"dota_autumn",
				"dota_diretide_12",
				"dota_winter"
			};

            // Path to the map folder
            var mapPath = Global.BasePath + SourcePath + @"dota\maps\";

            // Loop over each map
            foreach (var map in maps)
            {
                // Debug log
                Helpers.Log("Attempting to patch " + map);

                // Make a copy of the map
                File.Copy(mapPath + map + ".bsp", mapPath + map + ".bsp.tmp", true);

                // Delete the original map
                File.Delete(mapPath + map + ".bsp");

                // Load up the map
                using (var fs = File.Open(mapPath + map + ".bsp.tmp", FileMode.Open))
                {
                    // Read in the map data
                    var data = new BinaryReader(fs).ReadBytes((int)fs.Length);

                    // Search over data
                    for (int i = 0; i < data.Length - 10; )
                    {
                        // Searching for `world_maxs` basically
                        if (data[i++] != 'w' || data[i++] != 'o' || data[i++] != 'r' || data[i++] != 'l' ||
                            data[i++] != 'd' || data[i++] != '_' || data[i++] != 'm' || data[i++] != 'a' ||
                            data[i++] != 'x' || data[i++] != 's') continue;


                        // Matched, lets find where the numbers begin
                        while (i < data.Length && data[i++] != '"') ;
                        while (i < data.Length && data[i++] != '"') ;

                        // i is now positioned at the numbers
                        data[i++] = (byte)'8';
                        data[i++] = (byte)'3';
                        data[i++] = (byte)'2';
                        data[i++] = (byte)'0';

                        // Store that we patched successfully
                        Helpers.Log(map + " was patched successfully!");
                    }

                    // Write the new map
                    using (var writer = new BinaryWriter(File.Open(mapPath + map + ".bsp", FileMode.Create)))
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
        private static byte GetCurrentCpuUsage()
        {
            // Call is required twice because Windows
            CpuCounter.NextValue();

            // Sleep is required to delay next call because Windows
            System.Threading.Thread.Sleep(1000);
            return (byte)Math.Round(CpuCounter.NextValue());
        }

        // Gets the available RAM in the system in megabytes
        private static ushort GetAvailableRam()
        {
            return (ushort)Math.Round(RamCounter.NextValue());
        }

        // Gets the total RAM available in the system in megabytes
        private static ushort GetTotalRam()
        {
            return (ushort)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1000000);
        }

        // Gets the current upload speed in bytes/second
        private static uint GetUploadSpeed()
        {
            return DataSentCounter.Aggregate<PerformanceCounter, uint>(0, (current, t) => current + (uint)(Math.Round(t.NextValue())));
        }

        // Gets the current download speed in bytes/second
        private static uint GetDownloadSpeed()
        {
            uint download = 0;
            for (int i = 0; i < DataSentCounter.Count; ++i)
            {
                // Iterates through all adapters and sums them
                download += (uint)(Math.Round(DataReceivedCounter[i].NextValue()));
            }
            return download;
        }

        // Returns a list of all network interface card reference names
        private static string[] GetNetworkCards()
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var instancename = category.GetInstanceNames();
            return instancename;
        }

    }
}