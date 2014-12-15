using Alchemy.Classes;
using DotaHostClientLibrary;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Timers;
using System.Windows.Forms;

namespace DotaHostManager
{
    class Program
    {
        // Program version
        private const string VERSION = "0.1.1";

        // Addon status consts
        private const byte ADDON_STATUS_ERROR = 0;
        private const byte ADDON_STATUS_MISSING = 1;
        private const byte ADDON_STATUS_UPDATE = 2;
        private const byte ADDON_STATUS_READY = 3;


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
        private static string dotaPath;

        // Prevent running exit code more than once
        private static bool exiting = false;

        // Our websocket server
        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Any, 2074);

        private static void Main(string[] i)
        {
            // Reset log file
            File.Delete(Global.BASE_PATH + "log.txt");

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

            Helpers.log("[DotaHost] Version " + VERSION);

            // Sets up uri protocol args if launched from browser
            if (i.Length > 0) { Helpers.log("Requested: " + i[0]); }
            string[] args = new string[0];
            if (i.Length > 0)
            {
                args = i[0].Split('/');
                args = Helpers.RemoveIndex(args, 0);
                args = Helpers.RemoveIndex(args, 0);
                args = Helpers.RemoveIndex(args, args.Length - 1);
            }

            // Hook the dotaHostManager socket events
            hookWSocketEvents();

            // If first-run or requested autorun, attempt to register the uri protocol
            if (Properties.Settings.Default.autorun)
            {
                registerProtocol();
            }

            // Download the version file from website
            downloadAppVersion();

            // Attempts to find the dota path, if it can't find it, it exits the program
            if (!checkDotaPath())
            {
                Helpers.log("[DotaHost] Dota path could not be found. Exiting...");
                exit();
            }
            // Start websocket server
            Timers.setTimeout(500, Timers.MILLISECONDS, wsServer.start);

            // Begin exit timer
            appKeepAlive();

            // Event loop to prevent program from exiting
            doEvents();
        }

        // Copies this application to temp, then deletes itself
        private static void copyAndDeleteSelf()
        {
            using (var inputFile = new FileStream(
                        Global.BASE_PATH + "DotaHostManager.exe",
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
            dlManager.download(Global.DOWNLOAD_PATH_VERSION, Global.TEMP + "version", (e) => { }, (e) =>
            {
                Helpers.log("[Update] Checking for updates...");
                string version;
                try
                {
                    // Reads the version file from temp
                    version = File.ReadAllText(Global.TEMP + "version");
                    File.Delete(Global.TEMP + "version");

                    // Checks if the read version matches the const version
                    if (version != VERSION)
                    {
                        // They do not match, download new version
                        Helpers.log("[Update] New version detected!");

                        // If the downloader does not exist, download it
                        if (!File.Exists(Global.TEMP + "DotaHostManagerUpdater.exe"))
                        {
                            Helpers.log("[Update] Downloading updater...");

                            dlManager.download(Global.DOWNLOAD_PATH_UPDATER, Global.TEMP + "DotaHostManagerUpdater.exe", (e2) =>
                            {
                                appUpdaterDownloadProgress(e2.ProgressPercentage);
                            }, (e2) =>
                            {
                                // Begin the updater
                                startUpdater(version);
                            });
                        }
                        else
                        {
                            // Begin the updater
                            startUpdater(version);
                        }
                    }
                    else
                    {
                        Helpers.log("[Update] DotaHost up-to-date!");
                    }
                }
                catch
                {
                    Helpers.log("[Update] Updating failed.");
                }
            });
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
        private static void startUpdater(string version)
        {
            Helpers.log("[Update] Starting...");
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Global.TEMP;
            proc.FileName = "DotaHostManagerUpdater.exe";
            //proc2.Verb = "runas";
            proc.Arguments = "\"" + Global.BASE_PATH + "\" " + version;
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
            string[] fileList = Directory.GetFiles(dotaPath + @"dota\addons_dotahost");
            for (int i = 0; i < fileList.Length; ++i)
            {
                try
                {
                    string[] arr = fileList[i].Split('\\');
                    string addonID = arr[arr.Length - 1].Split('.')[0];
                    Helpers.log(addonID);
                    string downloadPath = string.Format(Global.DOWNLOAD_PATH_ADDON_INFO, addonID);
                    dlManager.downloadSync(downloadPath, Global.TEMP + addonID);
                    string[] info = File.ReadAllLines(Global.TEMP + addonID);
                    Helpers.deleteSafe(Global.TEMP + addonID);
                    if (info.Length != 2)
                    {
                        Helpers.log("ERROR: Infopacket for " + addonID + " is corrupted! Got " + info.Length + " lines instead of 2.");
                        c.Send(Helpers.packArguments("addon", ADDON_STATUS_ERROR.ToString(), addonID));
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
                            c.Send(Helpers.packArguments("addon", ADDON_STATUS_READY.ToString(), addonID));
                            continue;
                        }
                        else
                        {
                            c.Send(Helpers.packArguments("addon", ADDON_STATUS_UPDATE.ToString(), addonID));
                        }
                    }
                    else
                    {
                        c.Send(Helpers.packArguments("addon", ADDON_STATUS_MISSING.ToString(), addonID));
                        continue;
                    }
                }
                catch
                {

                }
            }
        }

        // Attempts to find the dota path, returns false if not found
        private static bool checkDotaPath()
        {
            bool failed = false;

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
            if (failed)
            {
                Helpers.log("Could not find dota path.");
                return false;
            }
            else
            {
                Helpers.log("Found dota path: " + dotaPath);
                return true;
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
            wsServer.addHook("setDotaPath", (c, x) => { updateDotaPath(x[1]); });
            wsServer.addHook("exit", (c, x) => { requestClose = true; });
            wsServer.addHook(WebSocketServer.CONNECTED, (c) => { wsServer.send(Helpers.packArguments("dotaPath", dotaPath)); });
            wsServer.addHook("autorun", (c, x) => { registerProtocol(); });
            wsServer.addHook("update", (c, x) =>
            {
                c.Send("startInstall");
                AddonDownloader.updateAddon(x[1], (addonID, success) =>
                {
                    // Tell the server what happened
                    if (success)
                    {
                        // Installation was successful, send formatted string to most recent connection
                        wsServer.send(Helpers.packArguments("installationComplete", "addon", addonID));
                    }
                    else
                    {
                        wsServer.send(Helpers.packArguments("installationFailed", "addon", addonID));
                    }
                }, (addonID, e) =>
                {
                    // If a socket connection has previously been opened, send the progress percentage in a formatted string
                    wsServer.send(Helpers.packArguments("addon", addonID, "percent", e.ProgressPercentage.ToString()));
                });
            });
            wsServer.addHook("getAddonStatus", (c, x) =>
            {
                checkAddons(c);
            });
            wsServer.addHook(WebSocketServer.RECEIVE, (c) => { appKeepAlive(); });
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

            // Stores the full executable path of this application, file name included
            string applicationPath = Assembly.GetEntryAssembly().Location;

            // Opens the key "DotaHost" and stores it in key
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("DotaHost");

            // If the protocol is not registered yet, we register it
            if (key == null)
            {
                try
                {
                    // Creates the subkey in the registry
                    key = Registry.ClassesRoot.CreateSubKey("DotaHost");

                    // Register the URI protocol in registry
                    key.SetValue(string.Empty, "URL:DotaHost Protocol");
                    key.SetValue("URL Protocol", string.Empty);

                    // Set URI to launch the application with one given argument
                    key = key.CreateSubKey(@"shell\open\command");
                    key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                    Helpers.log("Registry added.");
                }
                catch
                {
                    if (MessageBox.Show("Would you like DotaHostManager to launch itself automatically when you visit the DotaHost.net website? (Requires Administrator Privillages)", "Autorun", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Helpers.log("Failed to add registry. Launching as admin...");
                        const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.

                        // Start a new instance of this executable as administrator
                        ProcessStartInfo info = new ProcessStartInfo("DotaHostManager.exe");
                        info.UseShellExecute = true;
                        info.Verb = "runas";
                        try
                        {
                            Process.Start(info);
                        }
                        catch (Win32Exception ex)
                        {
                            if (ex.NativeErrorCode == ERROR_CANCELLED)
                            {
                                Helpers.log("Admin request denied.");
                            }
                        }
                    }
                    else
                    {
                        // Save user preference
                        Properties.Settings.Default.autorun = false;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            // Check if the current executing path does not match the one in the registry
            else if (key.OpenSubKey(@"shell\open\command").GetValue(string.Empty).ToString().ToLower() != ("\"" + applicationPath + "\" " + "%1").ToLower())
            {
                // Open the subkey
                key = key.OpenSubKey(@"shell\open\command", true);

                // Change the path to the current executing path
                key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                Helpers.log("Registry updated.");
            }
            else
            {
                Helpers.log("No registry action taken.");
            }

            // We're done with the registry, close the key
            if (key != null)
            {
                key.Close();
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
                    var info = new ProcessStartInfo("cmd.exe", "/C ping 1.1.1.1 -n 1 -w 3000 > Nul & Del \"" + Global.BASE_PATH + "DotaHostManager.exe" + "\"");
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(info).Dispose();
                    File.Delete(Global.BASE_PATH + "log.txt");
                }
                Timers.setTimeout(1, Timers.SECONDS, () => { Environment.Exit(0); });
            }
        }

    }

}
