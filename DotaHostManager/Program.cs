using Alchemy.Classes;
using DotaHostClientLibrary;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows.Forms;

namespace DotaHostManager
{
    class Program
    {
        // Addon status consts
        private const byte ADDON_STATUS_ERROR = 0;
        private const byte ADDON_STATUS_MISSING = 1;
        private const byte ADDON_STATUS_UPDATE = 2;
        private const byte ADDON_STATUS_READY = 3;

        // CRC of this exe
        private static string CRC = "";

        // Keep-alive timer
        private static System.Timers.Timer keepAlive;

        // Keep-alive duration
        private const int KEEP_ALIVE_DURATION = 60000; // 60 seconds

        // Increments by 1 everytime an action is started, decrements every time an action is finished. The program will not close on timeout unless this is zero
        private static byte zeroCanClose = 0;

        // If this is true, the program requests close, but will not close until zeroCanClose is equal to zero
        private static bool requestClose = false;

        // This is our download manager.
        private static DownloadManager dlManager = new DownloadManager();

        // Path to dota, eg: C:\Program Files (x86)\Steam\steamapps\dota 2 beta\
        private static string dotaPath = "";

        // Prevent running exit code more than once
        private static bool exiting = false;

        // Our websocket server
        private static WebSocketServer wsServer = new WebSocketServer(2074);

        private static void Main(string[] i)
        {
            if (i.Length > 1 && i[0] == "crc")
            {
                Console.WriteLine(Helpers.calculateCRC(i[1]));
                Console.ReadLine();
                return;
            }

            // Reset log file
            File.Delete(Global.BASE_PATH + "log.txt");

            File.Delete(Global.BASE_PATH + "DotaHostManagerUpdater.exe");

            // Create temp directory if it doesn't exist
            Directory.CreateDirectory(Global.TEMP);

            if (Global.TEMP != Global.BASE_PATH)
            {
                if (!File.Exists(Global.TEMP + "DotaHostManager.exe"))
                {
                    copyAndDeleteSelf();
                }
                else
                {
                    File.Delete(Global.TEMP + "DotaHostManager.exe");
                    copyAndDeleteSelf();
                }
                return;
            }

            // Hook the dotaHostManager socket events
            hookWSocketEvents();

            // Download the version file from website
            downloadAppVersion();

            // Attempts to find the dota path, if it can't find it, sets it to 'unknown'
            checkDotaPath();

            // Try to patch gameinfo
            if (!Properties.Settings.Default.gameinfoPatched)
            {
                patchGameInfo();
            }

            // Start websocket server
            Timers.setTimeout(500, Timers.MILLISECONDS, wsServer.start);

            // If first-run or requested autorun, attempt to register the uri protocol
            Console.WriteLine(Properties.Settings.Default.autorun);
            if (Properties.Settings.Default.shouldRegister)
            {
                registerProtocol();
            }
            if (Properties.Settings.Default.shouldDeregister)
            {
                deregisterProtocol();
            }

            // Begin exit timer
            appKeepAlive();

            // Event loop to prevent program from exiting
            doEvents();
        }

        // Copies this application to temp, then deletes itself
        private static void copyAndDeleteSelf()
        {
            using (var inputFile = new FileStream(
                        Helpers.FULL_EXE_PATH,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite
                    ))
            {
                using (var outputFile = new FileStream(Global.TEMP + "DotaHostManager.exe", FileMode.Create))
                {
                    var buffer = new byte[0x10000];
                    int bytes;

                    while ((bytes = inputFile.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputFile.Write(buffer, 0, bytes);
                    }
                }
            };
            Process.Start(Global.TEMP + "DotaHostManager.exe");
            exit();
        }

        // Download the most up-to-date version file of the app
        private static void downloadAppVersion()
        {
            Helpers.log(string.Format(Global.DOWNLOAD_PATH_ADDON_INFO, "DotaHostManager"));
            Helpers.log(Global.TEMP + "DotaHostManager.txt");
            dlManager.download(string.Format(Global.DOWNLOAD_PATH_ADDON_INFO, "DotaHostManager"), Global.TEMP + "DotaHostManager.txt", (e) => { }, (e) =>
            {
                Helpers.log("[Update] Checking for updates...");
                //try
                //{
                Console.WriteLine("1");
                // Reads the version file from temp
                string[] managerVersionCRC = File.ReadAllLines(Global.TEMP + "DotaHostManager.txt");
                Console.WriteLine("2");
                // Clean up file
                File.Delete(Global.TEMP + "DotaHostManager.txt");

                Console.WriteLine("3");
                Console.WriteLine(getCRC());
                // Checks if the read version matches the const version
                if (managerVersionCRC[1] != getCRC())
                {
                    // They do not match, download new version
                    Helpers.log("[Update] New version detected!");

                    dlManager.downloadSync(string.Format(Global.DOWNLOAD_PATH_ADDON_INFO, "DotaHostManagerUpdater"), Global.TEMP + "DotaHostManagerUpdater.txt");

                    string[] updaterVersionCRC = File.ReadAllLines(Global.TEMP + "DotaHostManagerUpdater.txt");

                    Helpers.log("[Update] Downloading updater...");

                    dlManager.download(updaterVersionCRC[0], Global.TEMP + "DotaHostManagerUpdater.exe", (e2) =>
                    {
                        appUpdaterDownloadProgress(e2.ProgressPercentage);
                    }, (e2) =>
                    {
                        // Begin the updater
                        startUpdater();
                    });
                }
                else
                {
                    Helpers.log("[Update] DotaHost up-to-date!");
                }
                //}
                ///catch
                //{
                //    Helpers.log("[Update] Updating failed.");
                //}
            });
        }

        // Calculate CRC and store it in CRC variable, if already calculated, just return CRC variable
        private static string getCRC()
        {
            Console.WriteLine("getCRC 1");
            if (CRC == "")
            {
                Console.WriteLine("getCRC 2");
                CRC = Helpers.calculateCRC(Helpers.FULL_EXE_PATH);
            }
            Console.WriteLine("getCRC 3");
            return CRC;
        }

        // Called every time the app updater download progresses
        private static void appUpdaterDownloadProgress(int percentage)
        {
            wsServer.send(Helpers.packArguments("appUpdater", "percent", percentage.ToString()));
        }

        // Exits the program as soon as it is finished the current task
        private static void exit(object sender, ElapsedEventArgs e)
        {
            requestClose = true;
            Helpers.log("[Time Out] Exiting...");
        }

        // Starts the updater and closes this program
        private static void startUpdater()
        {
            Helpers.log("[Update] Starting...");
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Global.TEMP;
            proc.FileName = "DotaHostManagerUpdater.exe";
            try
            {
                Process.Start(proc);
                exit();
            }
            catch
            {

            }
        }

        // Generates a json structure of installed addon information, sends it to client
        private static void checkAddons(UserContext c)
        {
            if (!Directory.Exists(dotaPath + @"dota\addons_dotahost"))
            {
                Directory.CreateDirectory(dotaPath + @"dota\addons_dotahost");
            }
            dlManager.downloadSync(Global.ROOT + "addons/addons.txt", Global.TEMP + "addons.txt");
            string[] addonsList = File.ReadAllLines(Global.TEMP + "addons.txt");
            Helpers.deleteSafe(Global.TEMP + "addons.txt");
            string[] fileList = Directory.GetFiles(dotaPath + @"dota\addons_dotahost");
            for (int i = 0; i < addonsList.Length; ++i)
            {
                try
                {
                    string addonID = addonsList[i];
                    string downloadPath = string.Format(Global.DOWNLOAD_PATH_ADDON_INFO, addonID);
                    dlManager.downloadSync(downloadPath, Global.TEMP + addonID);
                    string[] info = File.ReadAllLines(Global.TEMP + addonID);
                    Helpers.deleteSafe(Global.TEMP + addonID);
                    if (info.Length != 2)
                    {
                        Helpers.log("ERROR: Infopacket for " + addonID + " is corrupted! Got " + info.Length + " lines instead of 2.");
                        c.Send(Helpers.packArguments("addonStatus", ADDON_STATUS_ERROR.ToString(), addonID));
                        continue;
                    }

                    string version = info[0];
                    string correctCRC = info[1];
                    string actualCRC = "";

                    // Check if the addon is already downloaded
                    if (File.Exists(AddonDownloader.getAddonInstallLocation() + addonID + ".zip"))
                    {
                        // Check the CRC
                        actualCRC = Helpers.calculateCRC(AddonDownloader.getAddonInstallLocation() + addonID + ".zip");

                        // If it matches, we're already upto date
                        if (actualCRC == correctCRC)
                        {
                            c.Send(Helpers.packArguments("addonStatus", ADDON_STATUS_READY.ToString(), addonID));
                            continue;
                        }
                        else
                        {
                            c.Send(Helpers.packArguments("addonStatus", ADDON_STATUS_UPDATE.ToString(), addonID));
                            continue;
                        }
                    }
                    else
                    {
                        c.Send(Helpers.packArguments("addonStatus", ADDON_STATUS_MISSING.ToString(), addonID));
                        continue;
                    }
                }
                catch { }
            }
        }

        // Attempts to find the dota path, returns false if not found
        private static bool checkDotaPath()
        {
            try
            {
                // Checks if the path is already set
                if (Properties.Settings.Default.dotaPath == String.Empty)
                {
                    // Gets dota path from registry
                    string path = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570", "InstallLocation", null).ToString();

                    // If path is found, update it in the program
                    if (path != String.Empty)
                    {
                        updateDotaPath(path + @"\");
                    }
                }
                else
                {
                    // Path has already been found, set it to the stored setting
                    dotaPath = Properties.Settings.Default.dotaPath;

                    // Update addon downloader
                    AddonDownloader.setAddonInstallLocation(string.Format(Global.CLIENT_ADDON_INSTALL_LOCATION, dotaPath));
                }
                Helpers.log("Found dota path: " + dotaPath);
                return true;
            }
            catch
            {
                Helpers.log("Could not find dota path. Enter dota path manually on website.");
                updateDotaPath("unknown");
                return false;
            }
        }

        // Updates the dota path
        private static void updateDotaPath(string newPath)
        {
            // Check if newPath is a valid directory
            if (!Directory.Exists(newPath))
            {
                Helpers.log("Directory does not exist: " + newPath);
            }
            else
            {
                try
                {
                    // Sets dotaPath and settings to the new path
                    dotaPath = newPath;
                    Properties.Settings.Default.dotaPath = dotaPath;
                    Properties.Settings.Default.Save();
                    Helpers.log("Updated dota path: " + dotaPath);

                    // Update the addon downloads
                    AddonDownloader.setAddonInstallLocation(string.Format(Global.CLIENT_ADDON_INSTALL_LOCATION, dotaPath));

                    wsServer.send(Helpers.packArguments("dotaPath", newPath));
                }
                catch
                {
                    // Whoops, something went wrong
                    Helpers.log("Failed to update path: Uncaught exception");
                }
            }
        }

        // Create and bind the functions for web socket events
        private static void hookWSocketEvents()
        {
            wsServer.addHook("setDotaPath", setDotaPathHook);

            wsServer.addHook("exit", exitHook);

            wsServer.addHook(WebSocketServer.CONNECTED, connectedHook);

            wsServer.addHook("autorun", autorunHook);

            wsServer.addHook("getAutorun", getAutorunHook);

            wsServer.addHook("getDotapath", getDotapathHook);

            wsServer.addHook("uninstall", uninstallHook);

            wsServer.addHook("update", updateHook);

            wsServer.addHook("getAddonStatus", getAddonStatusHook);

            wsServer.addHook(WebSocketServer.RECEIVE, receiveHook);

            wsServer.addHook("gameServerInfo", gameServerInfoHook);

            wsServer.addHook("getPatchGameInfo", getPatchGameInfoHook);

            wsServer.addHook("patchGameInfo", patchGameInfoHook);
        }

        private static void setDotaPathHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            updateDotaPath(x[1]);
        }

        private static void exitHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            requestClose = true;
        }

        private static void connectedHook(UserContext c)
        {

            if (!validateConnection(c)) { return; }
            c.Send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
            c.Send(Helpers.packArguments("dotaPath", Properties.Settings.Default.dotaPath));
        }

        private static void autorunHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            Console.WriteLine("autorun receive");
            Properties.Settings.Default.shouldRegister = true;
            Properties.Settings.Default.Save();
            registerProtocol();
        }

        private static void getAutorunHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            c.Send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
        }

        private static void getDotapathHook(UserContext c, string[] x)
        {
            if (!validateConnection(c)) { return; }
            c.Send(Helpers.packArguments("dotaPath", dotaPath));
        }

        private static void uninstallHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            deregisterProtocol();
            Helpers.log("Uninstall received");
        }

        private static void updateHook(UserContext c, string[] x)
        {
            if (!validateConnection(c)) { return; }
            c.Send("startInstall");
            AddonDownloader.updateAddon(x[1], (addonID, success) =>
            {
                // Tell the server what happened
                if (success)
                {
                    // Installation was successful, send formatted string to most recent connection
                    wsServer.send(Helpers.packArguments("installationComplete"));
                }
                else
                {
                    wsServer.send(Helpers.packArguments("installationFailed"));
                }
            }, (addonID, e) =>
            {
                // If a socket connection has previously been opened, send the progress percentage in a formatted string
                wsServer.send(Helpers.packArguments("addon", addonID, e.ProgressPercentage.ToString()));
            });
        }

        private static void getAddonStatusHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            checkAddons(c);
        }

        private static void receiveHook(UserContext c)
        {

            if (!validateConnection(c)) { return; }
            appKeepAlive();
        }

        private static void gameServerInfoHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            Lobby lobby = new Lobby(KV.parse(x[2], true));
            AddonCompiler.compileAddons(lobby, AddonDownloader.getAddonInstallLocation(), dotaPath + @"dota\addons_dotahost\active\");
            c.Send(Helpers.packArguments("connectToServer", x[1]));
        }

        private static void getPatchGameInfoHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            c.Send(Helpers.packArguments("patchGameInfo", Properties.Settings.Default.gameinfoPatched ? "1" : "0"));
        }

        private static void patchGameInfoHook(UserContext c, string[] x)
        {

            if (!validateConnection(c)) { return; }
            c.Send(Helpers.packArguments("tryPatchGameInfo", patchGameInfo() ? "1" : "0"));
        }


        private static bool validateConnection(UserContext c)
        {
            if (c == wsServer.getConnections()[wsServer.getConnectionsCount() - 1])
            {
                return true;
            }
            else
            {
                c.Send("invalid");
                return false;
            }
        }

        // Attempt to patch gameinfo.txt
        private static bool patchGameInfo()
        {
            Helpers.log("Patching gameinfo.txt...");
            if (!processIsRunning("dota"))
            {
                source1GameInfoPatch();
                Properties.Settings.Default.gameinfoPatched = true;
                Properties.Settings.Default.Save();
                wsServer.send(Helpers.packArguments("gameinfo", "1"));
                Helpers.log("Patching gameinfo.txt success!");
                return true;
            }
            Helpers.log("Patching gameinfo.txt failure: dota.exe running");
            return false;
        }

        // Check if a process name is running.
        private static bool processIsRunning(string process)
        {
            return (System.Diagnostics.Process.GetProcessesByName(process).Length != 0);
        }

        // Updates gameinfo.txt to match DotaHost
        private static bool source1GameInfoPatch()
        {
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
                                "Game |gameinfo_path|addons_dotahost/active" + Environment.NewLine +
                            "}" + Environment.NewLine +
                        "}" + Environment.NewLine +
                    "}";

            // May need to add addon mounting here eventually

            // Write the metamod loader
            try
            {
                File.WriteAllText(dotaPath + @"dota\gameinfo.txt", gameinfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Removes the old timer, and ccreates and binds another one
        private static void appKeepAlive()
        {
            if (keepAlive != null)
            {
                keepAlive.Dispose();
            }
            keepAlive = new System.Timers.Timer(KEEP_ALIVE_DURATION);
            keepAlive.Elapsed += exit;
            keepAlive.Start();
        }

        // Application.DoEvents() + check closeRequest and zeroCanClose
        private static void doEvents()
        {
            System.Windows.Forms.Application.DoEvents();
            if (requestClose && zeroCanClose == 0)
            {
                exit();
            }
            Timers.setTimeout(1, Timers.SECONDS, doEvents);
        }

        // Function to request registration of the dotahost uri protocol
        private static void registerProtocol()
        {

            // Begin task
            zeroCanClose++;

            Helpers.log("[Protocol] Begin register...");

            // Stores the full executable path of this application, file name included
            string applicationPath = Helpers.FULL_EXE_PATH;

            // Opens the key "dotahost" and stores it in key
            bool found = false;
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("dotahost");

            string[] subkeys = Registry.ClassesRoot.GetSubKeyNames();
            for (var i = 0; i < subkeys.Length; ++i)
            {
                if (subkeys[i] == "dotahost")
                {
                    found = true;
                    break;
                }
            }

            // If the protocol is not registered yet, we register it
            if (!found)
            {
                try
                {
                    Helpers.log("[Protocol] Key not found, attempting create...");

                    // Creates the subkey in the registry
                    key = Registry.ClassesRoot.CreateSubKey("dotahost");

                    // Register the URI protocol in registry
                    key.SetValue(string.Empty, "URL:DotaHost Protocol");
                    key.SetValue("URL Protocol", string.Empty);

                    // Set URI to launch the application with one given argument
                    key = key.CreateSubKey(@"shell\open\command");
                    key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                    Properties.Settings.Default.shouldRegister = false;
                    Properties.Settings.Default.autorun = true;
                    Properties.Settings.Default.Save();

                    wsServer.send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));

                    Helpers.log("[Protocol] Registry added.");
                }
                catch
                {
                    Helpers.log("[Protocol] Failed to add registry. Requesting launch as admin...");
                    if (MessageBox.Show("Would you like DotaHostManager to launch itself automatically when you visit the DotaHost.net website? (Requires Administrator Privillages)", "Enable Autorun", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
                    {
                        const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.

                        // Start a new instance of this executable as administrator
                        ProcessStartInfo info = new ProcessStartInfo(Helpers.FULL_EXE_PATH);
                        info.UseShellExecute = true;
                        info.Verb = "runas";
                        try
                        {
                            Properties.Settings.Default.shouldRegister = true;
                            Properties.Settings.Default.Save();
                            Process.Start(info);
                            exit();
                        }
                        catch (Win32Exception ex)
                        {
                            if (ex.NativeErrorCode == ERROR_CANCELLED)
                            {
                                Properties.Settings.Default.shouldRegister = false;
                                Properties.Settings.Default.Save();
                                Helpers.log("[Protocol] Admin request denied.");

                                wsServer.send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                            }
                        }
                    }
                    else
                    {
                        // Save user preference
                        Properties.Settings.Default.shouldRegister = false;
                        Properties.Settings.Default.Save();
                        Helpers.log("[Protocol] User declined autorun option.");

                        wsServer.send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                    }
                }
            }
            else
            {
                try
                {
                    key = key.CreateSubKey(@"shell\open\command");
                    key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");
                    Helpers.log("[Protocol] Registry updated.");
                }
                catch
                {
                    Helpers.log("[Protocol] No registry action taken: Not running as Admin.");
                }
            }

            // We're done with the registry, close the key
            if (key != null)
            {
                key.Close();
            }

            Helpers.log("[Protocol] Done.");

            // End task
            zeroCanClose--;
        }

        // Function to request registration of the dotahost uri protocol
        private static void deregisterProtocol()
        {

            // Begin task
            zeroCanClose++;

            bool found = false;
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("dotahost");
            string[] subkeys = Registry.ClassesRoot.GetSubKeyNames();
            for (var i = 0; i < subkeys.Length; ++i)
            {
                if (subkeys[i] == "dotahost")
                {
                    found = true;
                    break;
                }
            }
            if (found)
            {
                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree("dotahost");
                    Properties.Settings.Default.shouldDeregister = false;
                    Properties.Settings.Default.autorun = false;
                    Properties.Settings.Default.Save();



                    wsServer.send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                }
                catch
                {
                    if (MessageBox.Show("Would you like to remove Autorun functionality from the DotaHost ModManager? (Requires Administrator Privillages)", "Disable Autorun", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
                    {
                        Helpers.log("[Protocol] Failed to remove registry. Launching as admin...");
                        const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.

                        // Start a new instance of this executable as administrator
                        ProcessStartInfo info = new ProcessStartInfo(Helpers.FULL_EXE_PATH);
                        info.UseShellExecute = true;
                        info.Verb = "runas";
                        try
                        {
                            Properties.Settings.Default.shouldDeregister = true;
                            Properties.Settings.Default.Save();
                            Process.Start(info);
                            exit();
                        }
                        catch (Win32Exception ex)
                        {
                            if (ex.NativeErrorCode == ERROR_CANCELLED)
                            {
                                Properties.Settings.Default.shouldDeregister = false;
                                Properties.Settings.Default.Save();
                                Helpers.log("[Protocol] Admin request denied.");

                                wsServer.send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                            }
                        }
                    }
                    else
                    {
                        Helpers.log("[Protocol] User declined disable autorun option.");

                        Properties.Settings.Default.shouldDeregister = false;
                        Properties.Settings.Default.Save();

                        wsServer.send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                    }
                }
            }
            else
            {
                Helpers.log("[Protocol] Key not found.");

                Properties.Settings.Default.shouldDeregister = false;
                Properties.Settings.Default.Save();

                wsServer.send(Helpers.packArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
            }


            // End task
            zeroCanClose--;
        }

        // Deletes the exe if autorun is false
        private static void exit()
        {
            if (!exiting)
            {
                exiting = true;
                if (!Properties.Settings.Default.autorun)
                {
                    var info = new ProcessStartInfo("cmd.exe", "/C ping 1.1.1.1 -n 1 -w 1000 > Nul & Del \"" + Helpers.FULL_EXE_PATH + "\"");
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(info).Dispose();
                    File.Delete(Global.BASE_PATH + "log.txt");
                }
                Timers.setTimeout(500, Timers.MILLISECONDS, () => { Environment.Exit(0); });
            }
        }

    }

}
